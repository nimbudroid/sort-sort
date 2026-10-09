namespace SortEverything.Prototype
{
    // Plain C# (no UnityEngine): rules, boards, attempts, validation and progression are testable in EditMode.

    /// <summary>
    /// The one rule-evaluation path. A target accepts an object when the object has any of the target's accepted
    /// categories (primary or secondary, no implied hierarchy) and, if the target names a colour, the object's
    /// primary colour matches. Colours and categories come only from authored object metadata.
    /// </summary>
    public static class RuleEvaluator
    {
        public static bool Accepts(TargetDef target, ObjectDef def)
        {
            if (target == null || def == null) return false;
            var cats = target.accepts;
            bool hasCats = cats != null && cats.Length > 0;
            if (!hasCats && target.color == SortColor.None) return false; // a target must test something
            if (target.color != SortColor.None && def.sortColor != target.color) return false;
            if (!hasCats) return true;
            for (int i = 0; i < cats.Length; i++) if (def.HasCategory(cats[i])) return true;
            return false;
        }

        /// <summary>Default upper-case label for a rule: "FRUIT", "BLUE", "RED FRUIT", "FRUIT + VEGETABLES".</summary>
        public static string Label(string[] categories, SortColor color)
        {
            string c = color != SortColor.None ? ColorName(color) : null;
            string k = null;
            if (categories != null && categories.Length > 0)
            {
                var parts = new string[categories.Length];
                for (int i = 0; i < categories.Length; i++)
                {
                    var def = CategoryLibrary.Get(categories[i]);
                    parts[i] = def != null ? def.label : categories[i].Replace('_', ' ');
                }
                k = string.Join(" + ", parts);
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

        public static SortColor ParseColor(string s)
        {
            if (string.IsNullOrEmpty(s)) return SortColor.None;
            switch (s.ToUpperInvariant())
            {
                case "RED": return SortColor.Red;
                case "BLUE": return SortColor.Blue;
                case "YELLOW": return SortColor.Yellow;
                case "GREEN": return SortColor.Green;
                case "PURPLE": return SortColor.Purple;
                default: return SortColor.None;
            }
        }
    }

    /// <summary>Why a placement was refused (shown to the player as the reason).</summary>
    public enum PlaceResult { Accepted, WrongTarget, Full, Ignored }
}
