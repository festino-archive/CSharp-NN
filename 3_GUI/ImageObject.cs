using Lab.Contract;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab
{
    internal class ImageObject
    {
        public string Filename { get; private set; }
        public string Category { get; private set; }
        public CroppedBitmap CroppedImage { get; private set; }

        public ImageObject(string filename, string category, BitmapSource freezedImage, int x1, int y1, int x2, int y2)
        {
            Filename = filename;
            Category = category;
            Int32Rect rect = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
            CroppedImage = new CroppedBitmap(freezedImage, rect);
            CroppedImage.Freeze();
        }

        public ImageObject(RecognisionData data)
        {
            int width = data.X2 - data.X1;
            int height = data.Y2 - data.Y1;
            WriteableBitmap img = FromBytes(data.ObjectImage, width, height);
            img.Freeze();
            Filename = data.Name;
            Category = data.Category;
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            CroppedImage = new CroppedBitmap(img, rect);
            CroppedImage.Freeze();
        }

        private static WriteableBitmap FromBytes(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return bitmap;
        }
    }

    internal class ClassificationCategory
    {
        public string Name { get; private set; }
        public ObservableCollection<ImageObject> FoundObjects;
        public int Count { get => FoundObjects.Count; }

        public ClassificationCategory(string name)
        {
            Name = name;
            FoundObjects = new ObservableCollection<ImageObject>();
        }
    }
}
