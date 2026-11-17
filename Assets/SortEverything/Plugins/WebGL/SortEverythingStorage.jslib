// Unity WebGL keeps Application.persistentDataPath in an in-memory file system backed by IndexedDB.
// Writes only reach IndexedDB (and survive a page reload) after FS.syncfs, which this exposes to C#.
mergeInto(LibraryManager.library, {
  SortEverything_SyncFileSystem: function () {
    FS.syncfs(false, function (err) {
      if (err) console.warn("Sort Everything: saving to browser storage failed", err);
    });
  }
});
