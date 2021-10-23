using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Lab
{
    internal class ImageObject
    {
        public string Filename { get; private set; }
        public CroppedBitmap CroppedImage { get; private set; }
        public int X1 { get; private set; }
        public int Y1 { get; private set; }
        public int X2 { get; private set; }
        public int Y2 { get; private set; }

        public ImageObject(string filename, BitmapImage freezedImage, int x1, int y1, int x2, int y2)
        {
            Filename = filename;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Int32Rect rect = new Int32Rect(X1, Y1, X2 - X1, Y2 - Y1);
            CroppedImage = new CroppedBitmap(freezedImage, rect);
            CroppedImage.Freeze();
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
