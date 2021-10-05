using System;
using System.Text;
using System.Threading.Tasks;

namespace Lab
{
    class Program
    {
        // 2b => TPL Flow + progress bar

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // input - full path
            string path;
            if (args.Length > 1)
            {
                path = args[1];
                for (int i = 2; i < args.Length; i++)
                    path += " " + args[i];
            }
            else
            {
                Console.WriteLine($"Please enter image directory path: ");
                path = Console.ReadLine();
            }
            Console.WriteLine($"Using path \"{path}\"...");

            ImageRecogniser recogniser = new ImageRecogniser(path);

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
    }
}
