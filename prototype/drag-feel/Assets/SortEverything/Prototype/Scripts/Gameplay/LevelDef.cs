using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>One bin inside a level: its acceptance rule, the label it shows and its body colour.</summary>
    public sealed class BinDef
    {
        public readonly BinRule rule;
        public readonly string label;       // shown on the bin; defaults to the rule label ("RED FRUIT")
        public readonly string colorHex;    // bin body colour

        /// <summary>Category bins get colours outside the five rule colours so they never read as colour rules.</summary>
        public static readonly string[] CategoryBinColors = { "F28C28", "22B8B0", "FF6FAE", "B9875A" };

        public BinDef(SortColor requiredColor, string requiredCategory, string colorHex = null, string label = null)
        {
            rule = new BinRule(requiredColor, requiredCategory);
            this.label = label ?? RuleEvaluator.Label(rule);
            this.colorHex = colorHex ?? RuleEvaluator.ColorHex(requiredColor) ?? CategoryBinColors[0];
        }

        public static BinDef Color(SortColor c) { return new BinDef(c, null); }
        public static BinDef Category(string category, string colorHex) { return new BinDef(SortColor.None, category, colorHex); }
        public static BinDef Both(SortColor c, string category) { return new BinDef(c, category); }

        public override string ToString() { return label; }
    }

    /// <summary>
    /// One authored level. Object and bin counts come only from here (never from the level number).
    /// Objects are an explicit list of library ids; the same ObjectDef is reused by every level that lists it.
    /// </summary>
    public sealed class LevelDef
    {
        public const float DefaultWrongDropPenalty = 1f;

        public readonly string id;              // stable id, used for best times
        public readonly int designLevel;        // where it sits in the intended long-term progression
        public readonly float timerSeconds;
        public readonly BinDef[] bins;
        public readonly string[] objectIds;
        public readonly float wrongDropPenalty; // seconds removed per wrong drop; 0 disables
        public readonly bool comboEnabled;      // cosmetic combo pop-up (off by default)

        public LevelDef(string id, int designLevel, float timerSeconds, BinDef[] bins, string[] objectIds,
            float wrongDropPenalty = DefaultWrongDropPenalty, bool comboEnabled = false)
        {
            this.id = id;
            this.designLevel = designLevel;
            this.timerSeconds = timerSeconds;
            this.bins = bins ?? new BinDef[0];
            this.objectIds = objectIds ?? new string[0];
            this.wrongDropPenalty = wrongDropPenalty;
            this.comboEnabled = comboEnabled;
        }

        /// <summary>How the level reads, derived from its bins (for the rule banner only).</summary>
        public SortingMode Mode
        {
            get
            {
                RuleShape first = RuleShape.Invalid;
                for (int i = 0; i < bins.Length; i++)
                {
                    var s = bins[i].rule.Shape;
                    if (i == 0) first = s;
                    else if (s != first) return SortingMode.Mixed;
                }
                switch (first)
                {
                    case RuleShape.Color: return SortingMode.Color;
                    case RuleShape.Category: return SortingMode.Category;
                    case RuleShape.ColorAndCategory: return SortingMode.ColorAndCategory;
                    default: return SortingMode.Mixed;
                }
            }
        }

        /// <summary>Rule banner headline: "SORT BY COLOR", "SORT BY CATEGORY", "SORT BY CATEGORY + COLOR", "SORT BY:".</summary>
        public string BannerTitle
        {
            get
            {
                switch (Mode)
                {
                    case SortingMode.Color: return "SORT BY COLOR";
                    case SortingMode.Category: return "SORT BY CATEGORY";
                    case SortingMode.ColorAndCategory: return "SORT BY CATEGORY + COLOR";
                    default: return "SORT BY:";
                }
            }
        }

        /// <summary>Banner detail line: the bin labels in order ("FRUIT · BLUE · TOYS").</summary>
        public string BannerDetail
        {
            get
            {
                var parts = new List<string>();
                for (int i = 0; i < bins.Length; i++) parts.Add(bins[i].label);
                return string.Join("  •  ", parts.ToArray());
            }
        }

        public override string ToString() { return id; }
    }
}
