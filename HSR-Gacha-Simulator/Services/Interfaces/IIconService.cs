using System.Windows.Media.Imaging;

namespace HSR_Gacha_Simulator.Services;

public interface IIconService
{
    /// <summary>
    /// Returns a cached BitmapImage for the given file name inside the Icons/ directory,
    /// or null if the file does not exist.
    /// </summary>
    BitmapImage? LoadOrNull(string fileName);
}
