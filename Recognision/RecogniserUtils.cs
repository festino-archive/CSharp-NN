using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognision
{
    public static class RecogniserUtils
    {
        readonly static string[] supportedExtensions = { ".jpg", ".png" };

        public static string[]? TryLoadFiles(string fullPath)
        {
            string fullImagePath = fullPath.Trim();
            if (fullImagePath.Length == 0 || fullImagePath.Contains(new string(Path.GetInvalidFileNameChars())))
            {
                return null;
            }

            fullImagePath = Path.GetFullPath(/*Directory.GetCurrentDirectory() + */fullImagePath);

            string[]? filenames = Directory.EnumerateFiles(fullImagePath, "*.*")
                .Where(file => supportedExtensions.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            return filenames;
        }
    }
}
