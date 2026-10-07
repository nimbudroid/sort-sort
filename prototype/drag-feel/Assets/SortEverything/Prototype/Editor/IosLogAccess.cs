#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SortEverything.Prototype.EditorTools
{
    /// <summary>
    /// Exposes the app's Documents folder (Application.persistentDataPath, where Telemetry writes its logs)
    /// in Finder / the Files app, so observers can copy test logs off the device.
    /// </summary>
    static class IosLogAccess
    {
        [PostProcessBuild]
        static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("UIFileSharingEnabled", true);
            plist.root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
