using Lab.Contract;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public Task AddAsync(RecognisionData obj) // TODO workaround System.Reflection.TargetInvocationException
        {
            return Task.Run(() => Add(obj));
        }

        public Task RemoveAsync(RecognisionData obj)
        {
            return Task.Run(() => Remove(obj));
        }

        public async Task LoadAllAsync(Action<RecognisionData, double> callback)
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

        public void Add(RecognisionData data)
        {
            db.Recognised.Add(data);
            db.SaveChanges();
        }

        public RecognisionData? Load(int id)
        {
            return db.Recognised.Where(d => d.Id == id).First();
        }

        public void Remove(RecognisionData obj)
        {
            int? id = GetDuplicateId(obj);
            if (id != null)
            {
                db.Recognised.Remove(db.Recognised.Where(d => d.Id == id.Value).First());
                db.SaveChanges();
            }
        }

        public bool Contains(RecognisionData obj)
        {
            return GetDuplicateId(obj) != null;
        }

        public int[] LoadIds()
        {
            int[] filteredIds = db.Recognised
                         .Select(x => x.Id)
                         .ToArray();
            return filteredIds;
        }

        public RecognisionData[] LoadAll()
        {
            return LoadByIds(LoadIds());
        }

        public RecognisionData[] LoadCategory(string category)
        {
            int[] filteredIds = db.Recognised
                         .Where(d => d.Category == category)
                         .Select(x => x.Id)
                         .ToArray();
            return LoadByIds(filteredIds);
        }

        public CategoryInfo[] LoadCategories()
        {
            var categoryNames = db.Recognised
                         .Select(d => d.Category)
                         .Distinct()
                         .ToList();
            CategoryInfo[] res = new CategoryInfo[categoryNames.Count];
            for (int i = 0; i < categoryNames.Count; i++)
            {
                string name = categoryNames[i];
                int count = db.Recognised
                         .Where(d => d.Category == name)
                         .Count();
                res[i] = new CategoryInfo(name, count);
            }
            return res;
        }

        private RecognisionData[] LoadByIds(int[] ids)
        {
            RecognisionData[] objects = new RecognisionData[ids.Length];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Load(ids[i]);
            return objects;
        }

        private int? GetDuplicateId(RecognisionData obj)
        {
            byte[] pixels = obj.ObjectImage;
            int duplicateId = db.Recognised
                         .Where(d => d.X1 == obj.X1 && d.Y1 == obj.Y1 && d.X2 == obj.X2 && d.Y2 == obj.Y2)
                         .Where(d => d.ObjectImage == pixels)
                         .Select(d => d.Id)
                         .FirstOrDefault();
            if (duplicateId == default(int))
                return null;
            return duplicateId;
        }
    }
}
