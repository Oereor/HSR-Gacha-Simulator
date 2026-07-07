using System.IO;
using System.Windows.Media.Imaging;

namespace HSR_Gacha_Simulator.Services
{
    public class IconService : IIconService
    {
        private readonly Dictionary<string, BitmapImage> Cache = new();
        private readonly string IconsDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Icons");

        public BitmapImage? LoadOrNull(string fileName)
        {
            string fullPath = Path.Combine(IconsDir, fileName);
            if (!File.Exists(fullPath)) return null;

            if (Cache.TryGetValue(fullPath, out var cached))
                return cached;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(fullPath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            Cache[fullPath] = bmp;
            return bmp;
        }
    }
}
