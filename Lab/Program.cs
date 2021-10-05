using System;
using System.Text;
using System.Threading.Tasks;

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

            Task.Factory.StartNew(() =>
            {
                if (Console.ReadKey().KeyChar == 'c')
                    recogniser.Stop();
                Console.WriteLine("\nPress any key to exit");
            });

            var results = await recogniser.Start();
            // output - list of files: file name + its objects: class name + bounding box
            Console.WriteLine("\nResult\n");
            foreach (RecognisionResult res in results)
            {
                Console.WriteLine($"{res.Filename}");
                foreach (DetectedObject obj in res.Objects)
                {
                    int x1 = obj.Box.X;
                    int y1 = obj.Box.Y;
                    int x2 = obj.Box.X + obj.Box.Width;
                    int y2 = obj.Box.Y + obj.Box.Height;
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
