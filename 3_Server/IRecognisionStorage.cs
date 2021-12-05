using System;
using System.Threading.Tasks;

namespace Lab
{
    internal interface IRecognisionStorage
    {
        void Add(RecognisionData obj);
        void Remove(RecognisionData obj);
        Task AddAsync(RecognisionData obj);
        Task RemoveAsync(RecognisionData obj);
        bool Contains(RecognisionData obj);
        void Clear();
        RecognisionData[] LoadAll();
        RecognisionData[] LoadAllAsync(Action<RecognisionData, double> callback);
        CategoryInfo[] LoadCategories();
        RecognisionData[] LoadCategory(string category);
    }
}
