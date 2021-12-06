using Lab.Contract;
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
        RecognisionData? Load(int id);
        void Clear();
        int[] LoadIds();
        CategoryInfo[] LoadCategories();
        RecognisionData[] LoadCategory(string category);
    }
}
