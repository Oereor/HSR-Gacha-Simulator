using System.IO;
using System.Windows.Media.Imaging;

namespace HSR_Gacha_Simulator
{
    public static class IconLoader
    {
        private static readonly Dictionary<string, BitmapImage> Cache = new();
        private static readonly string IconsDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Icons");

        public static BitmapImage? LoadOrNull(string fileName)
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
