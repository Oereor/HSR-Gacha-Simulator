namespace HSR_Gacha_Simulator
{
    public enum ItemType
    {
        Unknown = 0,
        Avatar,
        LightCone
    }

    public enum ItemRarity
    {
        Unknown = 0,
        Blue, 
        Purple, 
        Gold
    }

    public enum PathType
    {
        Unknown = 0,
        Destruction, 
        TheHunt,
        Erudition,
        Harmony,
        Nihility,
        Preservation,
        Abundance,
        Remembrance,
        Elation
    }

    public enum ElementType
    {
        Unknown = 0,
        Physical,
        Fire,
        Ice,
        Lightning,
        Wind,
        Quantum,
        Imaginary
    }

    public class ItemData
    {
        public ItemType Type { get; set; }
        public ItemRarity Rarity { get; set; }
        public string Name { get; set; } = "";
        public PathType Path { get; set; }
        public ElementType ElementType { get; set; }  // Only applicable for Avatars, can be set to Unknown for Light Cones
    }
}
