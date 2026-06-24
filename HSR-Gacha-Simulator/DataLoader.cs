using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HSR_Gacha_Simulator
{
    /// <summary>Static loader that deserializes JSON pool-config files into ItemData lists.</summary>
    public static class DataLoader
    {
        /// <summary>Load all items from a single pool-config JSON file.</summary>
        public static List<ItemData> LoadFromFile(string filePath)
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
        /// Loads the consolidated event pool config file and returns a list of
        /// parsed banner descriptors (only entries with <c>enabled: true</c>).
        /// </summary>
        public static List<EventPoolConfigEntry> LoadEventPoolConfigs(string filePath)
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
                    items.Add(ToItemData(itemDto));
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
