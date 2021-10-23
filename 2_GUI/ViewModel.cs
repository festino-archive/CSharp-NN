using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media.Imaging;

namespace Lab
{
    class ViewModel
    {
        readonly static string modelPath = "..\\..\\..\\..\\YOLOv4 Model\\yolov4.onnx";
        ImageRecogniser recogniser;

        public event Action RecognisionFinished;
        public event Action ResultUpdated;
        public Dictionary<string, List<ImageObject>> Result;

        public void Recognise(string imageDir)
        {
            recogniser = new ImageRecogniser(imageDir, modelPath, Environment.ProcessorCount);
            if (recogniser.ImageCount == 0)
            {
                recogniser = null;
                RecognisionFinished?.Invoke();
                return;
            }

            Result = new Dictionary<string, List<ImageObject>>();
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
                var uri = new Uri(Path.Combine(imageDir, res.Filename));
                BitmapImage image = new BitmapImage(uri);
                image.Freeze();
                foreach (DetectedObject obj in objects)
                {
                    if (!Result.ContainsKey(obj.Label))
                        Result[obj.Label] = new List<ImageObject>();
                    ImageObject resObj = new ImageObject(res.Filename, image, obj.X1, obj.Y1, obj.X2, obj.Y2);
                    Result[obj.Label].Add(resObj);
                }
                ResultUpdated?.Invoke();

                count++;
                if (count == recogniser.ImageCount)
                    break;
            }

            recogniser = null;
            RecognisionFinished?.Invoke();
        }

        public void StopRecognision()
        {
            if (recogniser != null)
                recogniser.Stop();
        }
    }
}
