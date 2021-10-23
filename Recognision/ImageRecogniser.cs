using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
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
using YoloPredictionEngine = Microsoft.ML.PredictionEngine<YOLOv4MLNet.DataStructures.YoloV4BitmapData, YOLOv4MLNet.DataStructures.YoloV4Prediction>;

namespace Lab
{
    public class ImageRecogniser : IDisposable
    {
        private bool disposed = false;

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
        private ConcurrentStack<YoloPredictionEngine> PredictionEngines;

        readonly string ModelPath;
        string FullModelPath;
        string[] supportedExtensions = { ".jpg", ".png" };
        readonly string ImagePath;
        string FullImagePath;

        readonly CancellationTokenSource TokenSource;
        public CancellationToken Token { get => TokenSource.Token; }

        string[] Filenames;
        int imageCount = -1;
        public int ImageCount
        {
            get
            {
                TryLoadFiles();
                return imageCount;
            }
        }
        int processedCount = 0;

        public ImageRecogniser(string fullPath, string modelPath, int threadNum = 2)
        {
            ImagePath = fullPath;
            ModelPath = modelPath;
            ThreadNum = threadNum;
            PredictionEngines = new ConcurrentStack<YoloPredictionEngine>();
            TokenSource = new CancellationTokenSource();
        }

        public async Task RecogniseAsync(ITargetBlock<RecognisionResult> outputBlock)
        {
            FullModelPath = Path.GetFullPath(/*Directory.GetCurrentDirectory() + */ModelPath);
            //Console.WriteLine("Using model: " + FullModelPath);
            await InitPredictionEngines();

            TryLoadFiles();

            // output - list of files: file name + its objects: class name + bounding box
            ParallelOptions options = new ParallelOptions();
            options.CancellationToken = Token;

            var processImageBlock = new TransformBlock<string, RecognisionResult>(imagePath =>
            {
                YoloPredictionEngine engine;
                PredictionEngines.TryPop(out engine);
                var results = Predict(imagePath, engine);
                PredictionEngines.Push(engine);
                List<DetectedObject> objects = new List<DetectedObject>();
                foreach (var res in results)
                    objects.Add(new DetectedObject(res));
                return new RecognisionResult(Path.GetFileName(imagePath), objects);
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = Token,
                MaxDegreeOfParallelism = ThreadNum
            });

            var counterBlock = new TransformBlock<RecognisionResult, RecognisionResult>(recognisionResult =>
            {
                processedCount++;
                if (processedCount == ImageCount)
                    processImageBlock.Complete();
                return recognisionResult;
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = Token,
                MaxDegreeOfParallelism = 1
            });

            processImageBlock.LinkTo(counterBlock);
            counterBlock.LinkTo(outputBlock);
            Parallel.For(0, ImageCount, options, fileNum => processImageBlock.Post(Filenames[fileNum]));

            await Task.WhenAll(processImageBlock.Completion);
        }

        public void Stop()
        {
            TokenSource.Cancel();
        }

        private void TryLoadFiles()
        {
            if (imageCount >= 0)
                return;

            FullImagePath = ImagePath.Trim();
            if (FullImagePath.Length == 0 || FullImagePath.Contains(new string(Path.GetInvalidFileNameChars())))
            {
                Filenames = null;
                imageCount = 0;
                return;
            }

            FullImagePath = Path.GetFullPath(/*Directory.GetCurrentDirectory() + */ImagePath);
            Filenames = Directory.EnumerateFiles(FullImagePath, "*.*")
                .Where(file => supportedExtensions.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            //Console.WriteLine("Files found: " + Filenames.Length);
            imageCount = Filenames.Length;
        }

        private IReadOnlyList<YoloV4Result> Predict(string fullPath, YoloPredictionEngine engine)
        {
            using (var bitmap = new Bitmap(Image.FromFile(fullPath)))
            {
                var predict = engine.Predict(new YoloV4BitmapData() { Image = bitmap });
                return predict.GetResults(classesNames, 0.3f, 0.7f);
            }
        }

        private async Task InitPredictionEngines()
        {
            MLContext mlContext = new MLContext();
            var model = InitModel(mlContext);

            var ab = new ActionBlock<int>(async index =>
            {
                var engine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                PredictionEngines.Push(engine);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = ThreadNum
            });

            Parallel.For(0, ThreadNum, i => ab.Post(i));
            ab.Complete();

            await ab.Completion;
        }


        private TransformerChain<OnnxTransformer> InitModel(MLContext mlContext)
        {
            // model is available here:
            // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4

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
            return pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));
        }

        public void Dispose()
        {
            if (!disposed)
            {
                PredictionEngines.Clear();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
