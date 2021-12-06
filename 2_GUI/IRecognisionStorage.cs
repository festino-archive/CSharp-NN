using System;

namespace Lab
{
    internal interface IRecognisionStorage
    {
        void Add(ImageObject obj);
        bool Contains(ImageObject obj);
        void Remove(ImageObject obj);
        void Clear();
        ImageObject[] LoadAll();
        Tuple<string, int>[] LoadCategories();
        ImageObject[] LoadCategory(string category);
    }
}
