using System;
using System.Threading.Tasks;

namespace Lab
{
    internal interface IRecognisionService
    {
        //Task AddAsync(ImageObject obj);
        //Task RemoveAsync(ImageObject obj);
        //bool Contains(ImageObject obj);
        void Clear();
        Task LoadAllAsync(Action<ImageObject, double> callback);
        CategoryInfo[] LoadCategories();
        ImageObject[] LoadCategory(string category);
    }
}
