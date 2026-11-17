using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using UnityEngine;

namespace SortEverything.Runtime
{
    /// <summary>
    /// Loads authored content data from Resources/SortEverything/Content: objects.json (object metadata),
    /// containers.json (container archetypes), areas.json (surfaces, walls, mess) and Levels/*.json
    /// (data levels). Cached; reset on every Play because domain reload may be disabled.
    /// </summary>
    public static class ContentDb
    {
        const string Root = "SortEverything/Content/";
        static ObjectsFile _objects;
        static Dictionary<string, ObjectData> _byId;
        static ContainersFile _containers;
        static AreasFile _areas;
        static List<(string name, LevelsFile file)> _levelFiles;
        static List<LevelDef> _dataLevels;
        static ScenesFile _scenes;
        static CategoriesFile _categories;
        static bool _stringsRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _objects = null; _byId = null; _containers = null; _areas = null; _levelFiles = null; _dataLevels = null; _scenes = null; _categories = null; _stringsRegistered = false; }

        static T Load<T>(string path) where T : new()
        {
            var ta = Resources.Load<TextAsset>(Root + path);
            if (ta == null) return new T();
            try { return JsonUtility.FromJson<T>(ta.text) ?? new T(); }
            catch (System.Exception e) { Debug.LogError($"Sort Everything: could not parse {path}.json: {e.Message}"); return new T(); }
        }

        public static ObjectsFile ObjectsFile => _objects ?? (_objects = Load<ObjectsFile>("objects"));

        public static IReadOnlyDictionary<string, ObjectData> Objects
        {
            get
            {
                if (_byId != null) return _byId;
                _byId = new Dictionary<string, ObjectData>();
                foreach (var o in ObjectsFile.objects) if (!string.IsNullOrEmpty(o.id)) _byId[o.id] = o;
                return _byId;
            }
        }

        /// <summary>Metadata for an object; a neutral default when an ID has no entry (the validator flags that).</summary>
        public static ObjectData Object(string id) =>
            Objects.TryGetValue(id, out var o) ? o : new ObjectData { id = id, relativeSize = 1f, material = "plastic" };

        public static ContainersFile Containers => _containers ?? (_containers = Load<ContainersFile>("containers"));
        public static AreasFile Areas => _areas ?? (_areas = Load<AreasFile>("areas"));

        public static AreaData Area(string areaId) =>
            Areas.areas.FirstOrDefault(a => a.id == areaId) ?? Areas.areas.FirstOrDefault() ?? new AreaData { id = areaId };

        public static ScenesFile Scenes => _scenes ?? (_scenes = Load<ScenesFile>("scenes"));
        public static CategoriesFile Categories => _categories ?? (_categories = Load<CategoriesFile>("categories"));

        /// <summary>Every category the game knows: the Kitchen's four plus the rooms' data categories.</summary>
        public static IEnumerable<string> AllCategoryIds => CategoryIds.Ordered.Concat(Categories.categories.Select(c => c.id)).Distinct();

        public static CategoryData Category(string id) => Categories.categories.FirstOrDefault(c => c.id == id);

        /// <summary>Rooms in House order (the Kitchen first).</summary>
        public static IEnumerable<AreaData> Rooms => Areas.areas.OrderBy(a => a.order).ThenBy(a => a.id, System.StringComparer.Ordinal);

        public static IEnumerable<KeyValuePair<string, string>> AreaUnlocks =>
            Areas.areas.Where(a => !string.IsNullOrEmpty(a.unlockAfterLevel)).Select(a => new KeyValuePair<string, string>(a.id, a.unlockAfterLevel));

        /// <summary>Chapter and milestone rules of every data room (the Kitchen keeps its GDD anchors).</summary>
        public static IEnumerable<RoomRules> RoomRules => Areas.areas.Where(a => a.id != KitchenCatalogue.AreaId).Select(a => new RoomRules
        {
            AreaId = a.id,
            Chapters = a.chapters.Where(c => !c.planned).Select(c => (c.from, c.to)).ToList(),
            HasPlannedChapters = a.chapters.Any(c => c.planned),
            Milestones = a.milestones.Select(m => (m.id, m.prerequisites ?? System.Array.Empty<string>())).ToList(),
        });

        /// <summary>The installed House catalogue (Kitchen + data rooms, unlocks and room rules).</summary>
        public static Catalogue House(string profileId) => Catalogue.CreateHouse(profileId, DataLevels, AreaUnlocks, RoomRules);

        /// <summary>Display names from data (categories, objects, rooms) join the string table once.</summary>
        public static void RegisterStrings()
        {
            if (_stringsRegistered) return;
            _stringsRegistered = true;
            foreach (var c in Categories.categories) Strings.Register("category." + c.id, c.name);
            foreach (var o in ObjectsFile.objects) if (!string.IsNullOrEmpty(o.name)) Strings.Register("asset." + o.id, o.name);
            foreach (var a in Areas.areas) if (!string.IsNullOrEmpty(a.name)) Strings.Register("area." + a.id, a.name);
        }

        /// <summary>The scene profile for a level, or null (generic presentation).</summary>
        public static SceneData Scene(string levelId) => Scenes.scenes.FirstOrDefault(s => s.levelId == levelId);

        /// <summary>Raw data-level files (for validation).</summary>
        public static IReadOnlyList<(string name, LevelsFile file)> LevelFiles
        {
            get
            {
                if (_levelFiles != null) return _levelFiles;
                _levelFiles = new List<(string, LevelsFile)>();
                foreach (var ta in Resources.LoadAll<TextAsset>(Root + "Levels").OrderBy(t => t.name, System.StringComparer.Ordinal))
                {
                    try { _levelFiles.Add((ta.name, JsonUtility.FromJson<LevelsFile>(ta.text) ?? new LevelsFile())); }
                    catch (System.Exception e) { Debug.LogError($"Sort Everything: could not parse level file {ta.name}: {e.Message}"); }
                }
                return _levelFiles;
            }
        }

        /// <summary>Data levels built into LevelDefs, ordinals after the authored Kitchen. Strings are registered.</summary>
        public static IReadOnlyList<LevelDef> DataLevels
        {
            get
            {
                if (_dataLevels != null) return _dataLevels;
                _dataLevels = new List<LevelDef>();
                RegisterStrings();
                int kitchen = KitchenCatalogue.BuildLevelsCached().Count;
                foreach (var (name, file) in LevelFiles)
                {
                    try
                    {
                        var built = DataLevelBuilder.Build(file, Objects,
                            area => (area == KitchenCatalogue.AreaId ? kitchen : 0) + _dataLevels.Count(l => l.AreaId == area));
                        foreach (var l in built)
                        {
                            Strings.Register(l.PurposeKey, l.PurposeEnglish);
                            _dataLevels.Add(l);
                        }
                    }
                    catch (System.Exception e) { Debug.LogError($"Sort Everything: level file {name} skipped: {e.Message}"); }
                }
                return _dataLevels;
            }
        }
    }
}
