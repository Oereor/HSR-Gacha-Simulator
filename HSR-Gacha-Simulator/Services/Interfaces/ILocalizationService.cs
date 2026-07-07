using System.ComponentModel;

namespace HSR_Gacha_Simulator.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; set; }
    IReadOnlyList<string> AvailableLanguages { get; }

    string this[string key] { get; }
    string Get(string key);
    string Get(string key, params object[] args);
    string GetItemName(string englishName);
}
