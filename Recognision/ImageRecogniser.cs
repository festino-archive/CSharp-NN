using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using Recognision.DataStructures;
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

namespace Recognision
{
    public class ImageRecogniser : IDisposable
    {
        private bool disposed = false;
        private bool processing = false;

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
        readonly string ModelPath;
        readonly string FullModelPath;

        private BlockingCollection<IRecognisionTask> RecognisionTasks;
        private BlockingCollection<YoloPredictionEngine> PredictionEngines;
        readonly CancellationTokenSource TokenSource;
        public CancellationToken Token { get => TokenSource.Token; }

        public ImageRecogniser(string modelPath, int threadNum = 0)
        {
            if (threadNum <= 0)
            {
                threadNum = Environment.ProcessorCount;
            }
            ModelPath = modelPath;
            FullModelPath = Path.GetFullPath(/*Directory.GetCurrentDirectory() + */ModelPath);
            if (!File.Exists(FullModelPath))
                throw new ArgumentException($"Model file doesn't exists at \"{FullModelPath}\"");
            //Console.WriteLine("Using model: " + FullModelPath);

            ThreadNum = threadNum;
            RecognisionTasks = new BlockingCollection<IRecognisionTask>();
            PredictionEngines = new BlockingCollection<YoloPredictionEngine>();
            TokenSource = new CancellationTokenSource();
            InitPredictionEngines();
        }

        public async Task RecogniseAsync(string[]? filenames, ITargetBlock<RecognisionResult> outputBlock)
        {
            RecognisionTasks.Add(new FilesRecognisionTask(filenames, outputBlock));
            TryRecogniseNext();
        }

        public async Task RecogniseAsync(string name, Bitmap bitmap, ITargetBlock<RecognisionResult> outputBlock)
        {
            RecognisionTasks.Add(new BitmapRecognisionTask(name, bitmap, outputBlock));
            TryRecogniseNext();
        }

        public void TryRecogniseNext()
        {
            IRecognisionTask? task = null;
            lock (RecognisionTasks)
            {
                if (!processing)
                {
                    RecognisionTasks.TryTake(out task);
                    if (task != null)
                        processing = true;
                }
            }
            if (task != null)
            {
                if (task is FilesRecognisionTask)
                {
                    FilesRecognisionTask filesTask = task as FilesRecognisionTask;
                    RecogniseFilesAsync(filesTask.Filenames, task.OutputBlock);
                }
                else if (task is BitmapRecognisionTask)
                {
                    BitmapRecognisionTask bitmapTask = task as BitmapRecognisionTask;
                    RecogniseBitmapAsync(bitmapTask.Name, bitmapTask.Bitmap, task.OutputBlock);
                }
            }
        }

        private async Task RecogniseFilesAsync(string[]? filenames, ITargetBlock<RecognisionResult> outputBlock)
        {
            // output - list of files: file name + its objects: class name + bounding box
            ParallelOptions options = new ParallelOptions();
            options.CancellationToken = Token;

            var processImageBlock = new TransformBlock<string, RecognisionResult>(imagePath =>
            {
                YoloPredictionEngine engine = PredictionEngines.Take();
                var results = Predict(imagePath, engine);
                PredictionEngines.Add(engine);
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

            int imageCount = filenames == null ? 0 : filenames.Length;
            int processedCount = 0;

            var counterBlock = new TransformBlock<RecognisionResult, RecognisionResult>(recognisionResult =>
            {
                processedCount++;
                if (processedCount == imageCount)
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
            Parallel.For(0, imageCount, options, fileNum => processImageBlock.Post(filenames[fileNum]));

            await processImageBlock.Completion;

            processing = false;
            TryRecogniseNext();
        }

        private async Task RecogniseBitmapAsync(string name, Bitmap bitmap, ITargetBlock<RecognisionResult> outputBlock)
        {
            YoloPredictionEngine engine = PredictionEngines.Take(Token);
            if (Token.IsCancellationRequested)
            {
                return;
            }
            var results = Predict(bitmap, engine);
            PredictionEngines.Add(engine);
            List<DetectedObject> objects = new List<DetectedObject>();
            foreach (var res in results)
                objects.Add(new DetectedObject(res));

            RecognisionResult result = new RecognisionResult(name, objects);
            outputBlock.Post(result);

            processing = false;
            TryRecogniseNext();
        }

        public void Stop()
        {
            TokenSource.Cancel();
        }

        private IReadOnlyList<YoloV4Result> Predict(string fullPath, YoloPredictionEngine engine)
        {
            using (var bitmap = new Bitmap(Image.FromFile(fullPath)))
            {
                return Predict(bitmap, engine);
            }
        }

        private IReadOnlyList<YoloV4Result> Predict(Bitmap bitmap, YoloPredictionEngine engine)
        {
            var predict = engine.Predict(new YoloV4BitmapData() { Image = bitmap });
            return predict.GetResults(classesNames, 0.3f, 0.7f);
        }

        private async Task InitPredictionEngines()
        {
            MLContext mlContext = new MLContext();
            var model = InitModel(mlContext);

            var ab = new ActionBlock<int>(index =>
            {
                var engine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                PredictionEngines.Add(engine);
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
                // wait for all Prediction engines
                for (int i = 0; i < ThreadNum; i++)
                {
                    var _ = PredictionEngines.Take();
                }
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
