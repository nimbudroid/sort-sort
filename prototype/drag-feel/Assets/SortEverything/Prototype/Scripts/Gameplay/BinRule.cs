namespace SortEverything.Prototype
{
    // Plain C# (no UnityEngine): rule evaluation, levels, the validator and the timer are testable in EditMode.

    /// <summary>The shape of one bin's rule. "Mixed" is not a rule shape; it describes a level (see SortingMode).</summary>
    public enum RuleShape { Invalid, Color, Category, ColorAndCategory }

    /// <summary>How a whole level reads, derived from its bins; used only for the rule banner.</summary>
    public enum SortingMode { Color, Category, ColorAndCategory, Mixed }

    /// <summary>
    /// What one bin accepts: an optional required colour and an optional required category, at least one set.
    /// Colour matches the object's primary colour only; category matches the primary or any secondary category.
    /// </summary>
    public struct BinRule
    {
        public readonly SortColor color;     // SortColor.None = any colour
        public readonly string category;     // null = any category (CategoryLibrary id)

        public BinRule(SortColor color, string category)
        {
            this.color = color;
            this.category = category;
        }

        public static BinRule ByColor(SortColor color) { return new BinRule(color, null); }
        public static BinRule ByCategory(string category) { return new BinRule(SortColor.None, category); }

        public bool HasColor { get { return color != SortColor.None; } }
        public bool HasCategory { get { return category != null; } }

        public RuleShape Shape
        {
            get
            {
                if (HasColor && HasCategory) return RuleShape.ColorAndCategory;
                if (HasColor) return RuleShape.Color;
                if (HasCategory) return RuleShape.Category;
                return RuleShape.Invalid;
            }
        }

        public bool Matches(ObjectDef def) { return RuleEvaluator.Matches(this, def); }

        public override string ToString() { return RuleEvaluator.Label(this); }
    }

    public static class RuleEvaluator
    {
        /// <summary>True when the object satisfies every part of the rule. Allocation-free.</summary>
        public static bool Matches(BinRule rule, ObjectDef def)
        {
            if (def == null || rule.Shape == RuleShape.Invalid) return false;
            if (rule.HasColor && def.sortColor != rule.color) return false;
            if (rule.HasCategory && !def.HasCategory(rule.category)) return false;
            return true;
        }

        /// <summary>Upper-case bin label: "RED", "FRUIT", "RED FRUIT".</summary>
        public static string Label(BinRule rule)
        {
            string c = rule.HasColor ? ColorName(rule.color) : null;
            string k = null;
            if (rule.HasCategory)
            {
                var def = CategoryLibrary.Get(rule.category);
                k = def != null ? def.label : rule.category.Replace('_', ' ');
            }
            if (c != null && k != null) return c + " " + k;
            return c ?? k ?? "?";
        }

        public static string ColorName(SortColor c)
        {
            switch (c)
            {
                case SortColor.Red: return "RED";
                case SortColor.Blue: return "BLUE";
                case SortColor.Yellow: return "YELLOW";
                case SortColor.Green: return "GREEN";
                case SortColor.Purple: return "PURPLE";
                default: return "";
            }
        }

        /// <summary>Bin-palette hex for a rule colour (identical to the bin and object palettes).</summary>
        public static string ColorHex(SortColor c)
        {
            switch (c)
            {
                case SortColor.Red: return "F2563A";
                case SortColor.Blue: return "3D7BF2";
                case SortColor.Yellow: return "F5C327";
                case SortColor.Green: return "43C46B";
                case SortColor.Purple: return "9B5DE5";
                default: return null;
            }
        }

        public static bool IsBinColor(SortColor c) { return c >= SortColor.Red && c <= SortColor.Purple; }
    }
}
