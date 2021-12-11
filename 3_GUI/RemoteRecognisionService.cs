using Lab.Contract;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab
{
    class RemoteRecognisionService : IRecognisionService
    {
        public string Host { get; set; } = "http://localhost:5000";

        public async Task<ImageObject[]?> RecogniseAsync(string filepath)
        {
            Uri uri = new Uri(filepath);
            BitmapSource source = new BitmapImage(uri);
            SingleImage recognisingImage = new SingleImage(Path.GetFileName(filepath), ToBytes(source), (int)source.Width, (int)source.Height);

            string objectJson = JsonConvert.SerializeObject(recognisingImage);
            HttpContent content = new StringContent(objectJson);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var client = new HttpClient();
            string url = Host + "/api/recognision";
            HttpResponseMessage response = await client.PutAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                RecognisionResult? parsed = JsonConvert.DeserializeObject<RecognisionResult>(await response.Content.ReadAsStringAsync());
                if (parsed == null)
                {
                    WriteError(url);
                    return null;
                }

                ImageObject[] result = new ImageObject[parsed.Recognised.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    RecognisionData data = parsed.Recognised[i];
                    result[i] = new ImageObject(data);
                }
                return result;
            }
            return null;
        }

        public async Task<bool> Clear()
        {
            string url = Host + "/api/recognision";
            var client = new HttpClient();
            HttpResponseMessage response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task LoadAllAsync(Action<ImageObject, double> callback)
        {
            string url = Host + "/api/recognision/all";
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                int[]? ids = JsonConvert.DeserializeObject<int[]>(await response.Content.ReadAsStringAsync());
                if (ids == null)
                {
                    WriteError(url);
                    return;
                }

                for (int i = 0; i < ids.Length; i++)
                {
                    double percent = (i + 1) / (double)ids.Length;
                    callback(Load(ids[i]).Result, percent);
                }
            }
            return;
        }

        private async Task<ImageObject?> Load(int id)
        {
            string url = Host + "/api/recognision?id=" + id;
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                RecognisionData? data = JsonConvert.DeserializeObject<RecognisionData>(await response.Content.ReadAsStringAsync());
                if (data == null)
                {
                    WriteError(url);
                    return null;
                }
                return new ImageObject(data);
            }
            return null;
        }

        public Task<ImageObject[]?> LoadCategory(string category)
        {
            int[] filteredIds = new int[0];
            throw new NotImplementedException();
            return LoadIds(filteredIds);
        }

        public async Task<CategoryInfo[]?> LoadCategories()
        {
            string url = Host + "/api/recognision/categories" ;

            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                CategoryInfo[]? parsed = JsonConvert.DeserializeObject<CategoryInfo[]>(await response.Content.ReadAsStringAsync());
                if (parsed == null)
                {
                    WriteError(url);
                    return null;
                }
                return parsed;
            }
            return null;
        }

        private async Task<ImageObject[]?> LoadIds(int[] ids)
        {
            ImageObject[] objects = new ImageObject[ids.Length];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Load(ids[i]).Result;
            return objects;
        }

        private void WriteError(string url)
        {
            System.Console.Error.WriteLine("Unsupported protocol for URL: \"" + url + "\"");
        }

        private static WriteableBitmap FromBytes(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return bitmap;
        }

        private static byte[] ToBytes(BitmapSource source)
        {
            int stride = source.PixelWidth * ((source.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);
            return pixels;
        }
    }
}
