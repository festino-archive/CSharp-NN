using System;
using System.Windows.Media.Imaging;

namespace Lab
{
    internal class ImageObject
    {
        public string Filename { get; private set; }
        public BitmapImage Image { get; private set; }
        public int X1 { get; private set; }
        public int Y1 { get; private set; }
        public int X2 { get; private set; }
        public int Y2 { get; private set; }

        public ImageObject(string filename, BitmapImage image, int x1, int y1, int x2, int y2)
        {
            Filename = filename;
            Image = image;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }
}
