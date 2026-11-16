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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _objects = null; _byId = null; _containers = null; _areas = null; _levelFiles = null; _dataLevels = null; _scenes = null; }

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
                int next = KitchenCatalogue.BuildLevelsCached().Count + 1;
                foreach (var (name, file) in LevelFiles)
                {
                    try
                    {
                        var built = DataLevelBuilder.Build(file, Objects, next);
                        foreach (var l in built)
                        {
                            Strings.Register(l.PurposeKey, l.PurposeEnglish);
                            _dataLevels.Add(l);
                        }
                        next += built.Count;
                    }
                    catch (System.Exception e) { Debug.LogError($"Sort Everything: level file {name} skipped: {e.Message}"); }
                }
                return _dataLevels;
            }
        }
    }
}
