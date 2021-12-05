using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab
{
    class RemoteRecognisionService : IRecognisionService
    {
        private RecognisionStorageContext db;
        internal readonly RecogniserWrapper recogniser = new RecogniserWrapper();

        public PersistentRecognisionStorage()
        {
            db = new RecognisionStorageContext(true);

        }

        public void Clear()
        {
            db.Dispose();
            db = new RecognisionStorageContext(false);
        }

        public Task AddAsync(ImageObject obj)
        {
            return Task.Run(() => Add(obj));
        }

        public Task RemoveAsync(ImageObject obj)
        {
            return Task.Run(() => Remove(obj));
        }

        public async Task LoadAllAsync(Action<ImageObject, double> callback)
        {
            await Task.Run(() =>
            {
                int[] ids = db.Recognised
                             .Select(x => x.Id)
                             .ToArray();
                for (int i = 0; i < ids.Length; i++)
                {
                    double percent = (i + 1) / (double)ids.Length;
                    callback(Load(ids[i]), percent);
                }
            });
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
            db.SaveChanges();
        }

        private ImageObject Load(int id)
        {
            RecognisionData data = db.Recognised.Where(d => d.Id == id).First();
            int width = data.X2 - data.X1;
            int height = data.Y2 - data.Y1;
            WriteableBitmap bitmap = FromBytes(data.ObjectImage, width, height);
            return new ImageObject(data.Name, data.Category, bitmap, 0, 0, width, height);
        }

        public void Remove(ImageObject obj)
        {
            int? id = GetDuplicateId(obj);
            if (id != null)
            {
                db.Recognised.Remove(db.Recognised.Where(d => d.Id == id.Value).First());
                db.SaveChanges();
            }
        }

        public bool Contains(ImageObject obj)
        {
            return GetDuplicateId(obj) != null;
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

        public CategoryInfo[] LoadCategories()
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

        private int? GetDuplicateId(ImageObject obj)
        {
            byte[] pixels = ToBytes(obj.CroppedImage);
            int duplicateId = db.Recognised
                         .Where(d => d.X1 == obj.X1 && d.Y1 == obj.Y1 && d.X2 == obj.X2 && d.Y2 == obj.Y2)
                         .Where(d => d.ObjectImage == pixels)
                         .Select(d => d.Id)
                         .FirstOrDefault();
            if (duplicateId == default(int)) // check if enumeration goes from 0
                return null;
            return duplicateId;
        }

        private WriteableBitmap FromBytes(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return bitmap;
        }

        private byte[] ToBytes(CroppedBitmap cropped)
        {
            BitmapSource source = cropped.Source;
            int width = cropped.SourceRect.Width;
            int height = cropped.SourceRect.Height;
            int stride = width * ((source.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(cropped.SourceRect, pixels, stride, 0);
            return pixels;
        }
    }
}
