namespace Lab
{
    public class CategoryInfo
    {
        public string Name { get; set; }
        public int Count { get; set; }
    
        public CategoryInfo(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
}
