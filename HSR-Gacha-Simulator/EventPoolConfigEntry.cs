namespace HSR_Gacha_Simulator
{
    /// <summary>
    /// Parsed representation of one enabled event banner from EventPoolConfigs.json.
    /// </summary>
    public class EventPoolConfigEntry
    {
        /// <summary>Stable key, e.g. "cyrene_avatar". Used for localization lookup.</summary>
        public string BannerKey { get; set; } = "";

        /// <summary>English display title, e.g. "Cyrene (Avatar)".</summary>
        public string BannerTitle { get; set; } = "";

        /// <summary>All items in this banner's pool (gold + purple, avatar + light cone).</summary>
        public List<ItemData> Items { get; set; } = new();
    }
}
