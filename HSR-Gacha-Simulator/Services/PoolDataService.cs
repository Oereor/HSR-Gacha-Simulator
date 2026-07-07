using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HSR_Gacha_Simulator.Models;

namespace HSR_Gacha_Simulator.Services
{
    /// <summary>Instance service that deserializes JSON pool-config files into ItemData lists.</summary>
    public class PoolDataService : IPoolDataService
    {
        /// <summary>Load all items from a single pool-config JSON file.</summary>
        public List<ItemData> LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var dtos = JsonSerializer.Deserialize<List<ItemDataDto>>(json, options);

            if (dtos == null)
                return new List<ItemData>();

            var items = new List<ItemData>(dtos.Count);
            foreach (var dto in dtos)
            {
                items.Add(ToItemData(dto));
            }
            return items;
        }

        /// <summary>
        /// Builds a lookup dictionary from gold + purple master pool files.
        /// Call this ONCE in MainViewModel, then pass the result to LoadEventPoolConfigs.
        /// </summary>
        public Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>
            BuildMasterLookup(string allGoldPath, string ordinaryPurplePath)
        {
            var allGold   = LoadFromFile(allGoldPath);
            var allPurple = LoadFromFile(ordinaryPurplePath);
            var merged    = allGold.Concat(allPurple).ToList();

            var lookup = new Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>();

            foreach (var item in merged)
            {
                var key = (item.Type, item.Rarity);
                if (!lookup.ContainsKey(key))
                    lookup[key] = new Dictionary<string, ItemData>();

                // If there's a duplicate name within the same (type, rarity) group,
                // later entries overwrite earlier ones (shouldn't happen, but be safe).
                lookup[key][item.Name] = item;
            }
            return lookup;
        }

        /// <summary>
        /// Loads the consolidated event pool config file and returns a list of
        /// parsed banner descriptors (only entries with <c>enabled: true</c>).
        /// Each simplified item (type, rarity, name) is enriched with full data
        /// (path, element-type) from the <paramref name="masterLookup"/>.
        /// </summary>
        public List<EventPoolConfigEntry> LoadEventPoolConfigs(
            string filePath,
            Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>> masterLookup)
        {
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dtos = JsonSerializer.Deserialize<List<EventPoolEntryDto>>(json, options);

            if (dtos == null)
                return new List<EventPoolConfigEntry>();

            var entries = new List<EventPoolConfigEntry>();
            foreach (var dto in dtos)
            {
                if (!dto.Enabled)
                    continue;

                var items = new List<ItemData>(dto.Items.Count);
                foreach (var itemDto in dto.Items)
                {
                    // Parse type and rarity from the simplified item
                    var type   = ParseEnum<ItemType>(itemDto.Type);
                    var rarity = ParseEnum<ItemRarity>(itemDto.Rarity);
                    var name   = itemDto.Name ?? "";

                    // Look up the full data from the master pools
                    if (masterLookup.TryGetValue((type, rarity), out var nameDict)
                        && nameDict.TryGetValue(name, out var fullItem))
                    {
                        // Use the master pool's ItemData — it has path + element
                        items.Add(fullItem);
                    }
                    else
                    {
                        // Fallback: item not found in master pool — log warning,
                        // create a partial ItemData (path/element = Unknown)
                        // so the banner still loads without crashing.
                        LogWarning($"Item '{name}' ({type}, {rarity}) from event config not found in master pools.");
                        items.Add(new ItemData
                        {
                            Type        = type,
                            Rarity      = rarity,
                            Name        = name,
                            Path        = PathType.Unknown,
                            ElementType = ElementType.Unknown
                        });
                    }
                }

                entries.Add(new EventPoolConfigEntry
                {
                    BannerKey   = dto.BannerKey,
                    BannerTitle = dto.BannerTitle,
                    Items       = items
                });
            }
            return entries;
        }

        private static ItemData ToItemData(ItemDataDto dto)
        {
            return new ItemData
            {
                Type        = ParseEnum<ItemType>(dto.Type),
                Rarity      = ParseEnum<ItemRarity>(dto.Rarity),
                Name        = dto.Name ?? "",
                Path        = ParseEnum<PathType>(dto.Path),
                ElementType = string.IsNullOrWhiteSpace(dto.ElementType)
                                ? ElementType.Unknown
                                : ParseEnum<ElementType>(dto.ElementType)
            };
        }

        private static T ParseEnum<T>(string? value) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
                return result;

            return default;
        }

        private static void LogWarning(string message)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HSR-Gacha-Simulator");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(
                    Path.Combine(logDir, "error.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [PoolDataService] WARNING: {message}{Environment.NewLine}");
            }
            catch { /* best-effort */ }
        }

        // ── DTO matching the on‑disk JSON schema ────────────────
        private class ItemDataDto
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("rarity")]
            public string? Rarity { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("path")]
            public string? Path { get; set; }

            [JsonPropertyName("element-type")]
            public string? ElementType { get; set; }
        }

        /// <summary>DTO for a single banner entry in EventPoolConfigs.json.</summary>
        private class EventPoolEntryDto
        {
            [JsonPropertyName("banner-key")]
            public string BannerKey { get; set; } = "";

            [JsonPropertyName("banner-title")]
            public string BannerTitle { get; set; } = "";

            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; }

            [JsonPropertyName("items")]
            public List<ItemDataDto> Items { get; set; } = new();
        }
    }
}
