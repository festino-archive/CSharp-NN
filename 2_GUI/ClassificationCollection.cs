using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lab
{
    class ClassificationCollection : IEnumerable<ClassificationCategory>, INotifyCollectionChanged
    {
        private PersistentRecognisionStorage storage = new PersistentRecognisionStorage();
        private ObservableCollection<ClassificationCategory> coll = new ObservableCollection<ClassificationCategory>();
        private Dispatcher dispatcher;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event Action ChildChanged;

        public int ObjectCount { get; private set; }
        public int Count { get => coll.Count; }

        public ClassificationCollection(Dispatcher dispatcher)
        {
            ObjectCount = 0;
            coll.CollectionChanged += OnCollectionChange;
            this.dispatcher = dispatcher;
        }

        public async Task LoadAllAsync(Action<double> callback)
        {
            await storage.LoadAllAsync((obj, percent) => {
                    dispatcher.Invoke(() => AddToCollection(obj));
                    callback(percent);
                });
        }

        public void Add(ImageObject obj)
        {
            if (!storage.Contains(obj)) // TODO async (EntityFramework needs multiple contexts or some queue)
            {
                AddToCollection(obj);
                storage.Add(obj);
            }
        }

        private void AddToCollection(ImageObject obj)
        {
            int index = GetIndex(obj.Category);
            ObservableCollection<ImageObject> list;
            if (index < 0)
            {
                var cc = new ClassificationCategory(obj.Category);
                index = coll.Count;
                coll.Add(cc);
                list = cc.FoundObjects;
            }
            else
            {
                list = coll[index].FoundObjects;
            }
            list.Add(obj);
            //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, (object)coll[index], (object)coll[index], index));
            ChildChanged?.Invoke();
        }

        public ObservableCollection<ImageObject> Get(string category)
        {
            for (int j = 0; j < coll.Count; j++)
                if (coll[j].Name == category)
                    return coll[j].FoundObjects;
            return null;
        }

        public int GetIndex(string category)
        {
            for (int j = 0; j < coll.Count; j++)
                if (coll[j].Name == category)
                    return j;
            return -1;
        }

        private void OnCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(sender, e);
        }

        public void Clear()
        {
            for (int i = 0; i < coll.Count; i++)
                coll[i].FoundObjects.Clear();
            coll.Clear();
            storage.Clear();
            ObjectCount = 0;
        }

        public void CounterReset()
        {
            ObjectCount = 0;
        }

        public void CounterIncrement()
        {
            ObjectCount++;
        }

        public IEnumerator<ClassificationCategory> GetEnumerator()
        {
            return ((IEnumerable<ClassificationCategory>)coll).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)coll).GetEnumerator();
        }
    }
}
