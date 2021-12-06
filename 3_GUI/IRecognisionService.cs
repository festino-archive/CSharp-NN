using Lab.Contract;
using System;
using System.Threading.Tasks;

namespace Lab
{
    internal interface IRecognisionService
    {
        string Host { get; set; }
        //Task AddAsync(ImageObject obj);
        //Task RemoveAsync(ImageObject obj);
        //bool Contains(ImageObject obj);
        Task<ImageObject[]?> RecogniseAsync(string filename);
        Task<bool> Clear();
        Task LoadAllAsync(Action<ImageObject, double> callback);
        Task<CategoryInfo[]?> LoadCategories();
        Task<ImageObject[]?> LoadCategory(string category);
    }
}
