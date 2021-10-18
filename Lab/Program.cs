using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lab
{
    // 2b => TPL Flow + progress bar
    class Program
    {
        readonly static string ModelPath = "..\\..\\..\\YOLOv4 Model\\yolov4.onnx";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // input - full path
            // better use CommandLine.Parser
            string path, modelPath = "";
            int threadNum = 0;
            if (args.Length > 0)
            {
                path = args[0];
                if (path == "help")
                {
                    PrintHelp();
                    return;
                }

                if (args.Length > 1)
                {
                    int.TryParse(args[1], out threadNum);
                    if (threadNum < 0)
                        threadNum = 0;
                }
                if (args.Length > 2)
                    modelPath = args[2];
            }
            else
            {
                PrintHelp();
                Console.WriteLine($"Please enter image directory path: ");
                path = Console.ReadLine();
            }
            if (modelPath == "")
                modelPath = ModelPath;
            Console.WriteLine($"Using path \"{path}\"...");

            ImageRecogniser recogniser = new ImageRecogniser(path, modelPath, Environment.ProcessorCount);

            CancellationTokenSource inputToken = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                if (Console.ReadKey().KeyChar == 'c')
                    recogniser.Stop();
                Console.WriteLine("\nPress any key to exit");
            }, inputToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            List<RecognisionResult> fullResult = new List<RecognisionResult>();
            Dictionary<string, int> currentResults = new Dictionary<string, int>();
            ConsoleProgressBar progress = new ConsoleProgressBar();
            BufferBlock<RecognisionResult> bufferBlock = new BufferBlock<RecognisionResult>(new ExecutionDataflowBlockOptions
            {
                CancellationToken = recogniser.Token
            });

            recogniser.RecogniseAsync(bufferBlock);

            progress.Write(0.0, "");
            while (true)
            {
                RecognisionResult res;
                try
                {
                    res = bufferBlock.Receive(recogniser.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                fullResult.Add(res);

                List<DetectedObject> objects = res.Objects;
                foreach (var obj in objects)
                {
                    if (currentResults.ContainsKey(obj.Label))
                        currentResults[obj.Label]++;
                    else
                        currentResults[obj.Label] = 1;
                }
                // updating - progress bar + list of classes: class name + count
                string info = "";
                foreach (var keyval in currentResults)
                    info = info + keyval.Key + ": " + keyval.Value + "\n";
                progress.Write(fullResult.Count / (double)recogniser.ImageCount, info);
                if (fullResult.Count == recogniser.ImageCount)
                    break;
            }

            inputToken.Cancel();
            currentResults.Clear();

            // output - list of files: file name + its objects: class name + bounding box
            Console.WriteLine("\nResult\n");
            foreach (RecognisionResult res in fullResult)
            {
                Console.WriteLine($"{res.Filename}");
                foreach (DetectedObject obj in res.Objects)
                {
                    int x1 = obj.X1;
                    int y1 = obj.Y1;
                    int x2 = obj.X2;
                    int y2 = obj.Y2;
                    Console.WriteLine($"\t{obj.Label}: ({x1}, {y1}), ({x2}, {y2})");
                }
            }
            Console.ReadLine();
        }
        static private void PrintHelp()
        {
            Console.WriteLine($"Use args: \n\t(0) image dir, \n\t(1) thread number(0 to use max), \n\t(2) model path");
        }
    }
}
