using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HSR_Gacha_Simulator.Services
{
    /// <summary>
    /// Singleton localisation service. Loads <c>LanguageConfigs/TextMap.json</c> and
    /// exposes translated strings via an indexer that XAML bindings can consume.
    /// Changing <see cref="CurrentLanguage"/> raises <c>PropertyChanged</c> for
    /// <c>"Item[]"</c> so all <c>{markup:Loc}</c> bindings re-evaluate live.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        // ── Static accessor (for MarkupExtension + XAML x:Static) ──

        /// <summary>
        /// Set by the DI-managed constructor so LocExtension and XAML
        /// {x:Static} bindings can reach the singleton instance.
        /// </summary>
        public static ILocalizationService Current { get; internal set; } = default!;

        // ── Private fields ─────────────────────────────────────────

        private string _currentLanguage = "en";
        private string _defaultLanguage = "en";
        private Dictionary<string, Dictionary<string, string>> _entries = new();
        private List<string> _availableLanguages = new() { "en" };
        private bool _loaded;

        // ── Settings path ──────────────────────────────────────────

        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "HSR-Gacha-Simulator");

        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        // ── Constructor ────────────────────────────────────────────

        public LocalizationService()
        {
            Current = this;

            // Restore saved language preference before first load
            string? saved = LoadSavedLanguage();
            if (!string.IsNullOrEmpty(saved))
                _currentLanguage = saved;
        }

        // ── Public properties ──────────────────────────────────────

        /// <summary>
        /// Current ISO 639-1 language code (e.g. "en", "zh").
        /// Setting this triggers all live XAML bindings to refresh.
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (_currentLanguage == value) return;

                _currentLanguage = value;
                SaveLanguage(value);
                OnPropertyChanged("Item[]");   // magic name → all indexer bindings refresh
                OnPropertyChanged(nameof(CurrentLanguage));
            }
        }

        /// <summary>
        /// Read-only list of available language codes, sourced from <c>meta.languages</c>.
        /// </summary>
        public IReadOnlyList<string> AvailableLanguages
        {
            get
            {
                EnsureLoaded();
                return _availableLanguages.AsReadOnly();
            }
        }

        // ── Indexer (for XAML {markup:Loc key}) ────────────────────

        /// <summary>
        /// Returns the translated string for <paramref name="key"/> in the
        /// <see cref="CurrentLanguage"/>, falling back through the default language
        /// to the key itself.
        /// </summary>
        public string this[string key] => Get(key);

        // ── Public methods ─────────────────────────────────────────

        /// <summary>
        /// Returns the translated string for <paramref name="key"/>.
        /// Fallback chain: current language → default language → raw key.
        /// Never throws.
        /// </summary>
        public string Get(string key)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(key))
                return key;

            if (_entries.TryGetValue(key, out var translations))
            {
                // Try current language first
                if (translations.TryGetValue(_currentLanguage, out var value))
                    return value;

                // Fall back to default language
                if (_currentLanguage != _defaultLanguage &&
                    translations.TryGetValue(_defaultLanguage, out var defaultVal))
                    return defaultVal;
            }

            // Last resort: return the key itself
            return key;
        }

        /// <summary>
        /// Returns a formatted translated string. Uses
        /// <see cref="string.Format(string, object[])"/> with the
        /// translation as the format string.
        /// </summary>
        public string Get(string key, params object[] args)
        {
            string format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Returns the display name for an item, trying <c>avatar.<paramref name="englishName"/></c>
        /// then <c>lightcone.<paramref name="englishName"/></c>.
        /// Falls back to <paramref name="englishName"/> itself if neither key exists.
        /// </summary>
        public string GetItemName(string englishName)
        {
            if (string.IsNullOrEmpty(englishName))
                return englishName;

            // Try avatar first, then lightcone
            string avatarKey = $"avatar.{englishName}";
            string avatarValue = Get(avatarKey);
            if (avatarValue != avatarKey)
                return avatarValue;

            string lcKey = $"lightcone.{englishName}";
            string lcValue = Get(lcKey);
            if (lcValue != lcKey)
                return lcValue;

            // No translation found — return the English name as-is
            return englishName;
        }

        // ── Loading ────────────────────────────────────────────────

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(baseDir, "LanguageConfigs", "TextMap.json");

                if (!File.Exists(path))
                {
                    LogError($"TextMap.json not found at {path}");
                    return;
                }

                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                using var doc = JsonDocument.Parse(json);

                // Parse meta
                if (doc.RootElement.TryGetProperty("meta", out var meta))
                {
                    if (meta.TryGetProperty("defaultLanguage", out var defLang))
                        _defaultLanguage = defLang.GetString() ?? "en";

                    if (meta.TryGetProperty("languages", out var langs) && langs.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var l in langs.EnumerateArray())
                        {
                            string? code = l.GetString();
                            if (!string.IsNullOrEmpty(code))
                                list.Add(code);
                        }
                        _availableLanguages = list;
                    }
                }

                // Parse entries
                if (doc.RootElement.TryGetProperty("entries", out var entries))
                {
                    _entries = new Dictionary<string, Dictionary<string, string>>();
                    foreach (var entry in entries.EnumerateObject())
                    {
                        var translations = new Dictionary<string, string>();
                        foreach (var langProp in entry.Value.EnumerateObject())
                        {
                            translations[langProp.Name] = langProp.Value.GetString() ?? "";
                        }
                        _entries[entry.Name] = translations;
                    }
                }

                // Ensure current language is in the available list
                if (!_availableLanguages.Contains(_currentLanguage))
                    _currentLanguage = _defaultLanguage;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load TextMap.json: {ex.Message}");
                // Leave _entries empty — fallback returns keys
            }
        }

        // ── Settings persistence ───────────────────────────────────

        private static string? LoadSavedLanguage()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return null;

                string json = File.ReadAllText(SettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var lang))
                    return lang.GetString();
            }
            catch
            {
                // Corrupted settings file — ignore
            }
            return null;
        }

        private static void SaveLanguage(string language)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(
                    new { language },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Best-effort persistence — silently ignore failures
            }
        }

        private static void LogError(string message)
        {
            try
            {
                string logDir = SettingsDir;
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "error.log");
                File.AppendAllText(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [LocalizationService] {message}{Environment.NewLine}");
            }
            catch
            {
                // Cannot even log — nothing more we can do
            }
        }

        // ── INotifyPropertyChanged ─────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
