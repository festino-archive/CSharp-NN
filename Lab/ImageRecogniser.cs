using Microsoft.ML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using YOLOv4MLNet.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace Lab
{
    class ImageRecogniser
    {
        static readonly string[] classesNames = new string[] {
            "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light",
            "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow",
            "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
            "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle",
            "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange",
            "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed",
            "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven",
            "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
        };
        readonly int ThreadNum;
        private PredictionEngine<YoloV4BitmapData, YoloV4Prediction>[] PredictionEngines;

        readonly string ModelPath = "\\YOLOv4 Model\\yolov4.onnx";
        string FullModelPath;
        string[] supportedExtensions = { ".jpg", ".png" };
        readonly string ImagePath;
        string FullImagePath;

        readonly CancellationTokenSource TokenSource;
        readonly CancellationToken Token;
        readonly ConsoleProgressBar progress;
        readonly Dictionary<string, int> CurrentResults;

        public ImageRecogniser(string fullPath, int threadNum = 1)
        {
            ImagePath = fullPath;
            ThreadNum = threadNum;
            PredictionEngines = new PredictionEngine<YoloV4BitmapData, YoloV4Prediction>[ThreadNum];
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
            progress = new ConsoleProgressBar();
            CurrentResults = new Dictionary<string, int>();
        }

        public async Task<ConcurrentBag<RecognisionResult>> Start()
        {
            FullModelPath = Path.GetFullPath(Directory.GetCurrentDirectory() + ModelPath);
            Console.WriteLine("Using model: " + FullModelPath);
            await InitPredictionEngines();

            FullImagePath = Path.GetFullPath(Directory.GetCurrentDirectory() + ImagePath);
            string[] filenames = Directory.EnumerateFiles(FullImagePath, "*.*")
                .Where(file => supportedExtensions.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Console.WriteLine("Files found: " + filenames.Length);

            // output - list of files: file name + its objects: class name + bounding box
            ConcurrentBag<RecognisionResult> result = new ConcurrentBag<RecognisionResult>();
            ParallelOptions options = new ParallelOptions();
            options.CancellationToken = Token;

            progress.Write(0.0, "");
            var ab = new ActionBlock<int>(async fileNum => {
                string imageName = filenames[fileNum];
                var results = Predict(imageName, 0); // TODO switch engine indices
                List<DetectedObject> objects = new List<DetectedObject>();
                foreach (var res in results)
                {
                    var label = res.Label;
                    var x1 = res.BBox[0];
                    var y1 = res.BBox[1];
                    var x2 = res.BBox[2];
                    var y2 = res.BBox[3];
                    objects.Add(new DetectedObject(res));
                    if (CurrentResults.ContainsKey(label))
                        CurrentResults[label]++;
                    else
                        CurrentResults[label] = 1;
                }
                result.Add(new RecognisionResult(Path.GetFileName(imageName), objects));
                options.CancellationToken.ThrowIfCancellationRequested();
                // updating - progress bar + list of classes: class name + count
                string info = "";
                foreach (var keyval in CurrentResults)
                    info = info + keyval.Key + ": " + keyval.Value + "\n";
                progress.Write(result.Count / (double)filenames.Length, info);
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = Token,
                MaxDegreeOfParallelism = ThreadNum
            });

            Parallel.For(0, filenames.Length, options, i => ab.Post(i));
            ab.Complete();

            await ab.Completion;
            CurrentResults.Clear();

            return result;
        }

        public void Stop()
        {
            TokenSource.Cancel();
        }

        private IReadOnlyList<YoloV4Result> Predict(string fullPath, int index)
        {
            using (var bitmap = new Bitmap(Image.FromFile(fullPath)))
            {
                var predict = PredictionEngines[index].Predict(new YoloV4BitmapData() { Image = bitmap });
                return predict.GetResults(classesNames, 0.3f, 0.7f);
            }
        }

        private async Task InitPredictionEngines()
        {
            var ab = new ActionBlock<int>(async index => {
                //Console.WriteLine("Starting initialization #" + index);
                var engine = await InitPredictionEngine();
                PredictionEngines[index] = engine;
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = ThreadNum
            });

            Parallel.For(0, ThreadNum, i => ab.Post(i));
            ab.Complete();

            await ab.Completion;
        }

        private Task<PredictionEngine<YoloV4BitmapData, YoloV4Prediction>> InitPredictionEngine()
        {
            // model is available here:
            // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4

            MLContext mlContext = new MLContext();

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: FullModelPath, recursionLimit: 100));

            // Fit on empty list to obtain input data schema
            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

            // Create prediction engine
            var engine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
            return Task.FromResult(engine);
        }
    }
}
