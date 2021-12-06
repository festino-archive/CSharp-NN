using Recognision;
using Recognision.DataStructures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace Lab
{
    interface IRecogniser
    {
        Task<Lab.Contract.RecognisionResult> RecogniseAsync(string name, byte[] pixels, int width, int height);
        Task<Lab.Contract.RecognisionResult> RecogniseAsync(string name, Bitmap bitmap);
    }

    class RecogniserWrapper : IRecogniser
    {
        private readonly static string modelPath = "..\\..\\..\\..\\YOLOv4 Model\\yolov4.onnx";
        private ImageRecogniser recogniser = new ImageRecogniser(modelPath);

        int processingFiles = 0;

        public async Task<Lab.Contract.RecognisionResult> RecogniseAsync(string name, byte[] pixels, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            return await RecogniseAsync(name, bitmap);
        }

        public async Task<Lab.Contract.RecognisionResult> RecogniseAsync(string name, Bitmap bitmap)
        {
            lock (this)
            {
                processingFiles++;
            }

            BufferBlock<RecognisionResult> bufferBlock = new BufferBlock<RecognisionResult>(new ExecutionDataflowBlockOptions
            {
                CancellationToken = recogniser.Token
            });

            await recogniser.RecogniseAsync(name, bitmap, bufferBlock);

            RecognisionResult res;
            try
            {
                res = bufferBlock.Receive(recogniser.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            List<DetectedObject> objects = res.Objects;
            Lab.Contract.RecognisionData[] imageResult = new Contract.RecognisionData[objects.Count];

            /*byte[] pixels = ToByteArray(bitmap, ImageFormat.Bmp);
            int bytesPerPixel = width * ((bitmap.Format.BitsPerPixel + 7) / 8);*/
            for (int i = 0; i < imageResult.Length; i++)
            {
                DetectedObject obj = objects[i];
                byte[] croppedImage = GetCroppedPixels(bitmap, new Rectangle(obj.X1, obj.Y1, obj.X2 - obj.X1, obj.Y2 - obj.Y1));
                imageResult[i] = new Lab.Contract.RecognisionData()
                {
                    Name = res.Filename,
                    Category = obj.Label,
                    ObjectImage = croppedImage,
                    X1 = obj.X1,
                    Y1 = obj.Y1,
                    X2 = obj.X2,
                    Y2 = obj.Y2
                };
            }

            lock (this)
            {
                processingFiles--;
            }

            return new Contract.RecognisionResult(imageResult);
        }

        public void StopRecognision()
        {
            if (recogniser != null)
            {
                recogniser.Stop();
                recogniser.Dispose();
                recogniser = null;
                GC.Collect(); // force collecting long-term garbage
            }
        }

        /*private byte[] GetCroppedPixels(byte[] orig, int bytesPerPixel, int x1, int y1, int x2, int y2)
        {
            int width = x2 - x1;
            int height = y2 - y1;
            int stride = bytesPerPixel * width;
            byte[] res = new byte[stride * height];
            for (int y = y1; y < y2; y++)
                for (int x = x1; x < x2; x++)
                    res[(x - x1) * stride + (y - y1) * height] = orig[x * stride + y * height];
            return res;
        }*/

        private byte[] GetCroppedPixels(Bitmap bitmap, Rectangle cropArea)
        {
            Bitmap cropped = CropImage(bitmap, cropArea);
            return ToByteArray(bitmap, ImageFormat.Bmp);
        }

        // https://stackoverflow.com/a/7350732
        private static byte[] ToByteArray(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }
        // https://stackoverflow.com/a/734938
        private static Bitmap CropImage(Bitmap bitmap, Rectangle cropArea)
        {
            return bitmap.Clone(cropArea, bitmap.PixelFormat);
        }
    }
}
