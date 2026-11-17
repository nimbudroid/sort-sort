using System.Runtime.InteropServices;

namespace SortEverything.Runtime
{
    /// <summary>
    /// On WebGL, flushes the persistent-data folder to the browser's IndexedDB so saves survive a reload.
    /// No-op on every other platform and in the Editor.
    /// </summary>
    public static class BrowserStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void SortEverything_SyncFileSystem();
        public static void Flush() => SortEverything_SyncFileSystem();
#else
        public static void Flush() { }
#endif
    }
}
