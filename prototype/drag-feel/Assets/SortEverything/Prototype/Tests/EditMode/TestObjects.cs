using SortEverything.Prototype;

namespace SortEverything.Tests
{
    /// <summary>Builds throwaway ObjectDefs with chosen colour and categories for rule tests.</summary>
    static class TestObjects
    {
        public static ObjectDef Make(string id, SortColor color, string primary, params string[] secondary)
        {
            var d = new ObjectDef("test_" + id, id, ObjectCategory.Home, color, 1, SizeClass.Small, MaterialKind.Plastic,
                Room.LivingRoom, ContentTier.Extended, null);
            d.SetCategories(primary, secondary, null);
            return d;
        }

        public static ObjectDef Lib(string id)
        {
            var d = ObjectLibrary.Get(id);
            if (d == null) throw new System.ArgumentException("no library object " + id);
            return d;
        }
    }
}
