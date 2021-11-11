using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab
{
    class PersistentRecognisionStorage : IRecognisionStorage
    {
        private RecognisionStorageContext db;

        public PersistentRecognisionStorage()
        {
            db = new RecognisionStorageContext(true);
        }

        public void Clear()
        {
            db.Dispose();
            db = new RecognisionStorageContext(false);
        }

        public void Add(ImageObject obj)
        {
            byte[] pixels = ToBytes(obj.CroppedImage);
            RecognisionData data = new RecognisionData() {
                Name = obj.Filename,
                Category = obj.Category,
                X1 = obj.X1,
                X2 = obj.X2,
                Y1 = obj.Y1,
                Y2 = obj.Y2,
                ObjectImage = pixels
            };
            db.Recognised.Add(data);
        }

        private ImageObject Load(int id)
        {
            RecognisionData data = db.Recognised.Where(d => d.Id == id).First();
            byte[] pixels = data.ObjectImage;
            int width = data.X2 - data.X1;
            int height = data.Y2 - data.Y1;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * ((bitmap.Format.BitsPerPixel + 7) / 8), 0);

            return new ImageObject(data.Name, data.Category, bitmap, 0, 0, width, height);
        }

        public void Remove(ImageObject obj)
        {
            int? id = GetDuplicateId(obj);
            if (id != null)
                db.Recognised.Remove(db.Recognised.Where(d => d.Id == id.Value).First());
        }

        public bool Contains(ImageObject obj)
        {
            return GetDuplicateId(obj) != null;
        }

        public int? GetDuplicateId(ImageObject obj)
        {
            byte[] pixels = ToBytes(obj.CroppedImage);
            int? duplicateId = db.Recognised
                         .Where(d => d.X1 == obj.X1 && d.Y1 == obj.Y1 && d.X2 == obj.X2 && d.Y2 == obj.Y2)
                         .Where(d => d.ObjectImage == pixels)
                         .Select(d => d.Id)
                         .First();
            return duplicateId;
        }

        public ImageObject[] LoadAll()
        {
            int[] filteredIds = db.Recognised
                         .Select(x => x.Id)
                         .ToArray();
            return LoadIds(filteredIds);
        }

        public ImageObject[] LoadCategory(string category)
        {
            int[] filteredIds = db.Recognised
                         .Where(d => d.Category == category)
                         .Select(x => x.Id)
                         .ToArray();
            return LoadIds(filteredIds);
        }

        public Tuple<string, int>[] LoadCategories()
        {
            var categoryNames = db.Recognised
                         .Select(d => d.Category)
                         .Distinct()
                         .ToList();
            Tuple<string, int>[] res = new Tuple<string, int>[categoryNames.Count];
            for (int i = 0; i < categoryNames.Count; i++)
            {
                string name = categoryNames[i];
                int count = db.Recognised
                         .Where(d => d.Category == name)
                         .Count();
                res[i] = new Tuple<string, int>(name, count);
            }
            return res;
        }

        private ImageObject[] LoadIds(int[] ids)
        {
            ImageObject[] objects = new ImageObject[ids.Length];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Load(ids[i]);
            return objects;
        }

        private byte[] ToBytes(CroppedBitmap cropped)
        {
            BitmapSource source = cropped.Source;
            int stride = source.PixelWidth * ((source.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(cropped.SourceRect, pixels, stride, 0);
            return pixels;
        }
    }
}
