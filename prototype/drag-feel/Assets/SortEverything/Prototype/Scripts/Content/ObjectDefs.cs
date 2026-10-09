namespace SortEverything.Prototype
{
    // Plain C# (no UnityEngine) so the object data can be unit-tested and rendered outside Unity.

    /// <summary>Bin colours. Values are the palette indices used by bins and the legacy shape mode.</summary>
    public enum SortColor { None = -1, Red = 0, Blue = 1, Yellow = 2, Green = 3, Purple = 4 }

    public enum ObjectCategory { Food, Office, Home, Toys, Electronics, Nature, Animals, Fantasy }

    public enum SizeClass { Small, Medium, Large }

    public enum MaterialKind { Organic, Plastic, Metal, Paper, Fabric, Glass, Wood, Rubber, Ceramic, Stone, Electronic, Magic }

    public enum Room { Kitchen, Office, Bathroom, Bedroom, LivingRoom, Playroom, Garden, Outdoors, Fantasy }

    /// <summary>Core = the first 30; Extended = more everyday objects; Later = animals / fantasy content.</summary>
    public enum ContentTier { Core, Extended, Later }

    /// <summary>What the round generator spawns. Shapes is the original P1 content.</summary>
    public enum ContentMode { Shapes, RealObjects, Mixed }

    /// <summary>Which tiers real-object rounds may draw from.</summary>
    public enum ObjectPool { Core, CoreAndExtended, Everything }

    /// <summary>
    /// One sortable thing. One definition is reused by every level; levels only reference it by id.
    ///   - id: also the visual asset reference (the drawing in ObjectDrawings and the cached sprite in ObjectArt).
    ///   - sortColor: the primary colour, the only colour bin rules match on. None = no bin colour.
    ///   - secondaryColors: descriptive only, never used for matching.
    ///   - PrimaryCategory / Categories: the sorting categories (CategoryLibrary ids). A category bin matches any of
    ///     them; the primary is only a default for display and tooling. Set from ObjectLibrary's category table.
    ///   - category (ObjectCategory): the coarse legacy group used by content spreading and the default material
    ///     family (ObjectMaterials); it is not a sorting category.
    /// </summary>
    public sealed class ObjectDef
    {
        public readonly string id;
        public readonly string displayName;
        public readonly ObjectCategory category;
        public readonly SortColor sortColor;   // None = no clear bin colour; never used in colour rounds
        public readonly int mass;              // mass class 1 (feather) .. 5 (very heavy), drives drag weight lag
        public readonly SizeClass size;
        public readonly MaterialKind material;
        public readonly Room room;
        public readonly ContentTier tier;
        public readonly string[] tags;
        public readonly string[] special;      // reserved for later mechanics (e.g. "fragile"); empty for now

        static readonly string[] NoCategories = new string[0];
        static readonly SortColor[] NoColors = new SortColor[0];

        /// <summary>Main sorting category (CategoryLibrary id), or null when none is assigned.</summary>
        public string PrimaryCategory { get; private set; }
        /// <summary>Further sorting categories (CategoryLibrary ids).</summary>
        public string[] SecondaryCategories { get; private set; }
        /// <summary>Primary followed by secondary categories; what category bins match against.</summary>
        public string[] Categories { get; private set; }
        /// <summary>Descriptive extra colours. Never used for matching.</summary>
        public SortColor[] SecondaryColors { get; private set; }

        public ObjectDef(string id, string displayName, ObjectCategory category, SortColor sortColor, int mass,
            SizeClass size, MaterialKind material, Room room, ContentTier tier, string[] tags, string[] special = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.sortColor = sortColor;
            this.mass = mass < 1 ? 1 : mass > 5 ? 5 : mass;
            this.size = size;
            this.material = material;
            this.room = room;
            this.tier = tier;
            this.tags = tags ?? new string[0];
            this.special = special ?? new string[0];
            SecondaryCategories = NoCategories;
            Categories = NoCategories;
            SecondaryColors = NoColors;
        }

        /// <summary>Assigns the sorting categories (called once by ObjectLibrary while building the library).</summary>
        internal void SetCategories(string primary, string[] secondary, SortColor[] secondaryColors)
        {
            PrimaryCategory = primary;
            SecondaryCategories = secondary ?? NoCategories;
            var all = new string[SecondaryCategories.Length + 1];
            all[0] = primary;
            for (int i = 0; i < SecondaryCategories.Length; i++) all[i + 1] = SecondaryCategories[i];
            Categories = all;
            SecondaryColors = secondaryColors ?? NoColors;
        }

        /// <summary>True when the category is the primary or any secondary category (no implicit hierarchy).</summary>
        public bool HasCategory(string categoryId)
        {
            var c = Categories;
            for (int i = 0; i < c.Length; i++) if (string.Equals(c[i], categoryId, System.StringComparison.Ordinal)) return true;
            return false;
        }

        public bool ColorSortable { get { return sortColor != SortColor.None; } }

        /// <summary>Multiplier on the 56 dp base object size. Matches the 1.0–1.3 range of shape mode.</summary>
        public float SizeScale
        {
            get
            {
                switch (size)
                {
                    case SizeClass.Small: return 1.0f;
                    case SizeClass.Medium: return 1.15f;
                    default: return 1.3f;
                }
            }
        }

        public bool InPool(ObjectPool pool)
        {
            switch (pool)
            {
                case ObjectPool.Core: return tier == ContentTier.Core;
                case ObjectPool.CoreAndExtended: return tier != ContentTier.Later;
                default: return true;
            }
        }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < tags.Length; i++) if (tags[i] == tag) return true;
            return false;
        }

        public override string ToString() { return id; }
    }
}
