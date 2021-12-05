using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace Lab
{
    class RecogniserWrapper
    {
        private readonly static string modelPath = "..\\..\\..\\..\\YOLOv4 Model\\yolov4.onnx";
        private ImageRecogniser recogniser;

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
                RecognisionData[] imageResult = new RecognisionData[objects.Count];

                var uri = new Uri(Path.Combine(imageDir, res.Filename));
                byte[] croppedImage = null; // TODO
                for (int i = 0; i < imageResult.Length; i++)
                {
                    DetectedObject obj = objects[i];
                    labels[i] = obj.Label;
                    imageResult[i] = new RecognisionData() {
                        Name = res.Filename, Category = labels[i], ObjectImage = croppedImage, X1 = obj.X1, Y1 = obj.Y1, X2 = obj.X2, Y2 = obj.Y2
                    };
                }
                // send obj

                count++;
                if (count == recogniser.ImageCount)
                    break;
            }

            recogniser.Dispose();
            recogniser = null;
            GC.Collect(); // force collecting long-term garbage
        }

        public void StopRecognision()
        {
            if (recogniser != null)
                recogniser.Stop();
        }
    }
}
