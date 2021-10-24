using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media.Imaging;

namespace Lab
{
    class RecogniserWrapper
    {
        readonly static string modelPath = "..\\..\\..\\..\\YOLOv4 Model\\yolov4.onnx";
        ImageRecogniser recogniser;

        public event Action RecognisionFinished;
        public event Action<string[], ImageObject[]> ResultUpdated;
        public int ImageCount
        {
            get {
                if (recogniser == null)
                    return 0;
                return recogniser.ImageCount;
            }
        }

        public void Recognise(string imageDir)
        {
            recogniser = new ImageRecogniser(imageDir, modelPath, Environment.ProcessorCount);
            if (recogniser.ImageCount == 0)
            {
                recogniser = null;
                RecognisionFinished?.Invoke();
                return;
            }

            BufferBlock<RecognisionResult> bufferBlock = new BufferBlock<RecognisionResult>(new ExecutionDataflowBlockOptions
            {
                CancellationToken = recogniser.Token
            });

            recogniser.RecogniseAsync(bufferBlock);
            int count = 0;
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

                List<DetectedObject> objects = res.Objects;
                string[] labels = new string[objects.Count];
                ImageObject[] imageResult = new ImageObject[objects.Count];

                var uri = new Uri(Path.Combine(imageDir, res.Filename));
                BitmapImage image = new BitmapImage(uri);
                image.Freeze();
                for (int i = 0; i < imageResult.Length; i++)
                {
                    DetectedObject obj = objects[i];
                    labels[i] = obj.Label;
                    imageResult[i] = new ImageObject(res.Filename, image, obj.X1, obj.Y1, obj.X2, obj.Y2);
                }
                ResultUpdated?.Invoke(labels, imageResult);

                count++;
                if (count == recogniser.ImageCount)
                    break;
            }

            recogniser.Dispose();
            recogniser = null;
            GC.Collect(); // force collecting long-term garbage
            RecognisionFinished?.Invoke();
        }

        public void StopRecognision()
        {
            if (recogniser != null)
                recogniser.Stop();
        }
    }
}
