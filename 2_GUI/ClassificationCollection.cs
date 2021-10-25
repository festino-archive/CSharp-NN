using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Lab
{
    class ClassificationCollection : IEnumerable<ClassificationCategory>, INotifyCollectionChanged
    {
        private ObservableCollection<ClassificationCategory> coll = new ObservableCollection<ClassificationCategory>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event Action ChildChanged;

        public int ObjectCount { get; private set; }
        public int Count { get => coll.Count; }

        public ClassificationCollection()
        {
            ObjectCount = 0;
            coll.CollectionChanged += OnCollectionChange;
        }

        public void Add(string category, ImageObject obj)
        {
            int index = GetIndex(category);
            ObservableCollection<ImageObject> list;
            if (index < 0)
            {
                var cc = new ClassificationCategory(category);
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

            ObjectCount++;
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
            ObjectCount = 0;
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
