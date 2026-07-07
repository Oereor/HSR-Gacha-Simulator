namespace HSR_Gacha_Simulator.Models
{
    public enum GachaType
    {
        Ordinary,
        EventAvatar,
        EventLightCone
    }

    public class GachaSystem
    {
        public List<ItemData> History { get; private set; } = new List<ItemData>();

        public GachaType Type { get; set; }

        /// <summary>Raised after History changes (pull, 10-pull, or reset).</summary>
        public event Action? HistoryChanged;

        private readonly ItemData EmptyItem = new ItemData { Type = ItemType.Unknown, Rarity = ItemRarity.Unknown, Name = "EmptyItem", Path = PathType.Unknown, ElementType = ElementType.Unknown };

        private readonly Random random = new Random();

        private GachaSystem()
        {
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>Create a GachaSystem for the given banner type.</summary>
        public static GachaSystem Create(GachaType type)
        {
            return new GachaSystem { Type = type };
        }

        /// <summary>Atomically populate all internal item pools.</summary>
        public void LoadPools(
            List<ItemData> goldAvatars,
            List<ItemData> goldLightCones,
            List<ItemData> celestialGoldAvatars,
            List<ItemData> eventGoldItems,
            List<ItemData> purpleAvatars,
            List<ItemData> purpleLightCones,
            List<ItemData> eventPurpleItems,
            List<ItemData> blueItems)
        {
            goldAvatarPool = goldAvatars;
            goldLightConePool = goldLightCones;
            celestialGoldAvatarPool = celestialGoldAvatars;
            eventGoldItemPool = eventGoldItems;
            purpleAvatarPool = purpleAvatars;
            purpleLightConePool = purpleLightCones;
            eventPurpleItemPool = eventPurpleItems;
            blueItemPool = blueItems;
        }

        /// <summary>Execute one gacha pull and return the result.</summary>
        public ItemData Pull()
        {
            ItemData item = DoGacha(Type);
            History.Add(item);
            HistoryChanged?.Invoke();
            return item;
        }

        /// <summary>
        /// Execute 10 pulls.  Enforces the HSR rule that a 10‑pull
        /// always contains at least one Purple (4★) or better item.
        /// </summary>
        public ItemData[] Pull10()
        {
            var results = new ItemData[10];
            bool hasPurpleOrBetter = false;

            for (int i = 0; i < 10; i++)
            {
                results[i] = DoGacha(Type);
                if (results[i].Rarity == ItemRarity.Purple || results[i].Rarity == ItemRarity.Gold)
                    hasPurpleOrBetter = true;
            }

            // If the 10 pulls were all blue, forcibly upgrade the last one.
            if (!hasPurpleOrBetter)
            {
                // Undo the blue‑branch purple‑pity increment (the gold‑pity
                // increment is correct — purple is still not gold).
                if (Type == GachaType.Ordinary)
                    ordinaryNonPurpleGachaCount = 0;
                else
                    eventNonPurpleGachaCount = 0;

                ItemData forcedPurple = GetPurpleItem(Type, missedPurpleEventItem);
                missedPurpleEventItem = (eventPurpleItemPool.Count > 0) && (!eventPurpleItemPool.Contains(forcedPurple));
                results[9] = forcedPurple;
            }

            History.AddRange(results);
            HistoryChanged?.Invoke();
            return results;
        }

        /// <summary>
        /// Reset all mutable state (history, guarantee flags, pity counters)
        /// without touching the item pools or banner type.
        /// </summary>
        public void Reset()
        {
            History.Clear();
            missedGoldEventItem = false;
            missedPurpleEventItem = false;
            eventNonGoldGachaAvatarCount = 0;
            eventNonGoldGachaLightConeCount = 0;
            eventNonPurpleGachaCount = 0;
            ordinaryNonGoldGachaCount = 0;
            ordinaryNonPurpleGachaCount = 0;
            HistoryChanged?.Invoke();
        }

        /// <summary>Number of pulls since the last 5★ item.</summary>
        public int NonGoldGachaCount => Type switch
        {
            GachaType.Ordinary       => ordinaryNonGoldGachaCount,
            GachaType.EventAvatar    => eventNonGoldGachaAvatarCount,
            GachaType.EventLightCone => eventNonGoldGachaLightConeCount,
            _ => 0
        };

        /// <summary>True when the next gold pull is guaranteed to be the event item.</summary>
        public bool IsGuaranteed => missedGoldEventItem;

        /// <summary>True when the next purple pull is guaranteed to be the event item.</summary>
        public bool IsPurpleGuaranteed => missedPurpleEventItem;

        /// <summary>Number of pulls since the last 4★ item.</summary>
        public int NonPurpleGachaCount => Type switch
        {
            GachaType.Ordinary       => ordinaryNonPurpleGachaCount,
            GachaType.EventAvatar    => eventNonPurpleGachaCount,
            GachaType.EventLightCone => eventNonPurpleGachaCount,
            _ => 0
        };

        // ── Statistics (computed from History) ────────────────────

        /// <summary>Total number of pulls performed on this banner.</summary>
        public int TotalPulls => History.Count;

        /// <summary>Number of 5★ (Gold) items pulled.</summary>
        public int GoldCount => History.Count(i => i.Rarity == ItemRarity.Gold);

        /// <summary>Number of 4★ (Purple) items pulled.</summary>
        public int PurpleCount => History.Count(i => i.Rarity == ItemRarity.Purple);

        /// <summary>Number of 3★ (Blue) items pulled.</summary>
        public int BlueCount => History.Count(i => i.Rarity == ItemRarity.Blue);

        /// <summary>
        /// Number of pulled 5★ items that are off-rate (not in the event gold pool).
        /// Chinese players call this "歪" — missing the rate-up.
        /// Returns 0 for ordinary-type banners (no event items, so there's nothing to miss).
        /// </summary>
        public int MissedGoldCount
        {
            get
            {
                int goldCount = History.Count(i => i.Rarity == ItemRarity.Gold);
                int eventCount = History.Count(i => i.Rarity == ItemRarity.Gold && eventGoldItemPool.Contains(i));
                return goldCount - eventCount;
            }
        }

        /// <summary>
        /// True if this banner has event gold items (Event Avatar / Event Light Cone).
        /// Ordinary banners return false.
        /// </summary>
        public bool HasEventItems => eventGoldItemPool.Count > 0;

        // ── Internal probability constants ──────────────────────

        private readonly int GoldAvatarRateUpThreshold = 73;  // So that the probability should reach 100% at the 90th gacha
        private readonly int GoldLightConeRateUpThreshold = 65;  // The probability should reach 100% at the 80th gacha
        private readonly int PurpleItemRateUpThreshold = 8;  // About 55% for the 9th pull, and 100% for the 10th

        private readonly int GoldItemBaseProbability = 6;  // 0.6% base chance
        private readonly int GoldAvatarRateUpStep = 60;  // 6% chance up for every gacha after the threshold
        private readonly int GoldLightConeRateUpStep = 70;  // 7% chance up for every gacha after the threshold

        private readonly int PurpleItemBaseProbability = 51;  // 5.1% base chance
        private readonly int PurpleItemRateUpStep = 500;  // 50% chance up for every gacha after the threshold

        private readonly int EventAvatarProbability = 500;  // 50% chance for event items when pulling a gold or purple avatar
        private readonly int EventLightConeProbability = 750;  // 75% chance for event items when pulling a gold or purple light cone


        private List<ItemData> goldAvatarPool = new List<ItemData>();  // The ordinary gold avatar pool

        private List<ItemData> celestialGoldAvatarPool = new List<ItemData>();  // When not trigger rate-up, randomly pick one from this pool

        private List<ItemData> goldLightConePool = new List<ItemData>();  // The ordinary gold light cone pool

        private List<ItemData> eventGoldItemPool = new List<ItemData>();

        private List<ItemData> purpleAvatarPool = new List<ItemData>();

        private List<ItemData> purpleLightConePool = new List<ItemData>();

        private List<ItemData> eventPurpleItemPool = new List<ItemData>();  // This should be a subset of either purpleAvatarPool or purpleLightConePool

        private List<ItemData> blueItemPool = new List<ItemData>();

        private bool missedGoldEventItem = false;
        private bool missedPurpleEventItem = false;

        private int eventNonGoldGachaAvatarCount = 0;
        private int eventNonGoldGachaLightConeCount = 0;
        private int eventNonPurpleGachaCount = 0;

        private int ordinaryNonGoldGachaCount = 0;
        private int ordinaryNonPurpleGachaCount = 0;

        private ItemData DoGacha(GachaType type)
        {
            if (type == GachaType.Ordinary)
            {
                if (IsGoldAvatar(ordinaryNonGoldGachaCount))
                {
                    ordinaryNonGoldGachaCount = 0;
                    ordinaryNonPurpleGachaCount = 0;
                    return GetGoldItem(type, false);
                }
                if (IsPurpleItem(ordinaryNonPurpleGachaCount))
                {
                    ordinaryNonPurpleGachaCount = 0;
                    ordinaryNonGoldGachaCount++;
                    return GetPurpleItem(type, false);
                }
                ordinaryNonGoldGachaCount++;
                ordinaryNonPurpleGachaCount++;
                return GetBlueItem();
            }
            if (type == GachaType.EventAvatar)
            {
                if (IsGoldAvatar(eventNonGoldGachaAvatarCount))
                {
                    eventNonGoldGachaAvatarCount = 0;
                    eventNonPurpleGachaCount = 0;
                    ItemData item = GetGoldItem(type, missedGoldEventItem);
                    missedGoldEventItem = !eventGoldItemPool.Contains(item);
                    return item;
                }
                if (IsPurpleItem(eventNonPurpleGachaCount))
                {
                    eventNonPurpleGachaCount = 0;
                    eventNonGoldGachaAvatarCount++;
                    ItemData item = GetPurpleItem(type, missedPurpleEventItem);
                    missedPurpleEventItem = (eventPurpleItemPool.Count > 0) && (!eventPurpleItemPool.Contains(item));
                    return item;
                }
                eventNonGoldGachaAvatarCount++;
                eventNonPurpleGachaCount++;
                return GetBlueItem();
            }
            if (type == GachaType.EventLightCone)
            {
                if (IsGoldLightCone(eventNonGoldGachaLightConeCount))
                {
                    eventNonGoldGachaLightConeCount = 0;
                    eventNonPurpleGachaCount = 0;
                    ItemData item = GetGoldItem(type, missedGoldEventItem);
                    missedGoldEventItem = !eventGoldItemPool.Contains(item);
                    return item;
                }
                if (IsPurpleItem(eventNonPurpleGachaCount))
                {
                    eventNonPurpleGachaCount = 0;
                    eventNonGoldGachaLightConeCount++;
                    ItemData item = GetPurpleItem(type, missedPurpleEventItem);
                    missedPurpleEventItem = (eventPurpleItemPool.Count > 0) && (!eventPurpleItemPool.Contains(item));
                    return item;
                }
                eventNonGoldGachaLightConeCount++;
                eventNonPurpleGachaCount++;
                return GetBlueItem();
            }
            return EmptyItem;  // Just a placeholder, should never reach here
        }

        private ItemData GetGoldItem(GachaType type, bool isRateUp)
        {
            if (type == GachaType.Ordinary)
            {
                List<ItemData> unionPool = [.. goldAvatarPool, .. goldLightConePool];
                return unionPool[random.Next(unionPool.Count)];
            }
            if (type == GachaType.EventAvatar)
            {
                List<ItemData> unionPool = [.. eventGoldItemPool, .. celestialGoldAvatarPool];
                if (IsEvent(true) || isRateUp)
                {
                    return eventGoldItemPool[random.Next(eventGoldItemPool.Count)];
                }
                return unionPool[random.Next(unionPool.Count)];
            }
            if (type == GachaType.EventLightCone)
            {
                List<ItemData> unionPool = [.. eventGoldItemPool, .. goldLightConePool];
                if (IsEvent(false) || isRateUp)
                {
                    return eventGoldItemPool[random.Next(eventGoldItemPool.Count)];
                }
                return unionPool[random.Next(unionPool.Count)];
            }
            return EmptyItem;  // Just a placeholder, should never reach here
        }

        private ItemData GetPurpleItem(GachaType type, bool isRateUp)
        {
            List<ItemData> unionPool = [.. purpleAvatarPool, .. purpleLightConePool];
            if (type == GachaType.Ordinary)
            {
                return unionPool[random.Next(unionPool.Count)];
            }
            if (type == GachaType.EventAvatar)
            {
                if (eventPurpleItemPool.Count > 0 && (IsEvent(true) || isRateUp))
                {
                    return eventPurpleItemPool[random.Next(eventPurpleItemPool.Count)];
                }
                return unionPool[random.Next(unionPool.Count)];
            }
            if (type == GachaType.EventLightCone)
            {
                if (eventPurpleItemPool.Count > 0 && (IsEvent(false) || isRateUp))
                {
                    return eventPurpleItemPool[random.Next(eventPurpleItemPool.Count)];
                }
                return unionPool[random.Next(unionPool.Count)];
            }
            return EmptyItem;  // Just a placeholder, should never reach here
        }

        private ItemData GetBlueItem()
        {
            return blueItemPool[random.Next(blueItemPool.Count)];
        }

        private int GetGoldAvatarProbability(int failureCount)
        {
            if (failureCount > GoldAvatarRateUpThreshold)
            {
                return GoldItemBaseProbability + (failureCount + 1 - GoldAvatarRateUpThreshold) * GoldAvatarRateUpStep;
            }
            return GoldItemBaseProbability;
        }

        private int GetGoldLightConeProbability(int failureCount)
        {
            if (failureCount > GoldLightConeRateUpThreshold)
            {
                return GoldItemBaseProbability + (failureCount + 1 - GoldLightConeRateUpThreshold) * GoldLightConeRateUpStep;
            }
            return GoldItemBaseProbability;
        }

        private int GetPurpleItemProbability(int failureCount)
        {
            if (failureCount > PurpleItemRateUpThreshold)
            {
                return PurpleItemBaseProbability + (failureCount + 1 - PurpleItemRateUpThreshold) * PurpleItemRateUpStep;
            }
            return PurpleItemBaseProbability;
        }

        private bool IsEvent(bool isAvatar)
        {
            int eventProbability = isAvatar ? EventAvatarProbability : EventLightConeProbability;
            return random.Next(1000) < eventProbability;
        }

        private bool IsGoldAvatar(int failureCount)
        {
            return random.Next(1000) < GetGoldAvatarProbability(failureCount);
        }

        private bool IsGoldLightCone(int failureCount)
        {
            return random.Next(1000) < GetGoldLightConeProbability(failureCount);
        }

        private bool IsPurpleItem(int failureCount)
        {
            return random.Next(1000) < GetPurpleItemProbability(failureCount);
        }
    }
}
