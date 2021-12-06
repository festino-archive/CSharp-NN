namespace Lab.Contract
{
    public class SingleImage
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ImageBase64 { get; set; }

        public SingleImage()
        {
        }

        public SingleImage(string name, byte[] pixels, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
            ImageBase64 = ToBase64(pixels);
        }

        public static string ToBase64(byte[] pixels)
        {
            return Convert.ToBase64String(pixels);
        }

        public static byte[] FromBase64(string encoded)
        {
            return Convert.FromBase64String(encoded);
        }
    }
}
