using HSR_Gacha_Simulator.Models;

namespace HSR_Gacha_Simulator.Services;

public interface IPoolDataService
{
    /// <summary>Load a single pool-config JSON file from disk.</summary>
    List<ItemData> LoadFromFile(string filePath);

    /// <summary>
    /// Build a two-level lookup keyed by (ItemType, ItemRarity) → item name → full ItemData.
    /// Used as the master reference for enriching event-banner items.
    /// </summary>
    Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>>
        BuildMasterLookup(string allGoldPath, string ordinaryPurplePath);

    /// <summary>
    /// Load the consolidated EventPoolConfigs.json, filter to enabled entries,
    /// and enrich each simplified item with full data from the master lookup.
    /// </summary>
    List<EventPoolConfigEntry> LoadEventPoolConfigs(
        string filePath,
        Dictionary<(ItemType, ItemRarity), Dictionary<string, ItemData>> masterLookup);
}
