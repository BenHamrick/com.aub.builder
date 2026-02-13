using System.IO;
using UnityEngine;

namespace AUB
{
    /// <summary>
    /// Programmatic cleanup of Unity's Bee build cache and Library/BuildCache.
    /// Running this from inside Unity avoids file locking issues that occur
    /// when trying to delete these folders externally while Unity holds locks.
    ///
    /// Usage: Unity -batchmode -quit -executeMethod AUB.Builder.CleanBuildCache
    /// </summary>
    public static class BeeCleanup
    {
        /// <summary>
        /// Delete Library/Bee/ and Library/BuildCache/ directories.
        /// Safe to call from batchmode — Unity doesn't lock these during cleanup.
        /// </summary>
        public static void Clean()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;

            CleanDir(Path.Combine(projectRoot, "Library", "Bee"));
            CleanDir(Path.Combine(projectRoot, "Library", "BuildCache"));

            Debug.Log("[AUB] Build cache cleaned successfully.");
        }

        private static void CleanDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.Log($"[AUB] Cache directory does not exist, skipping: {path}");
                return;
            }

            try
            {
                Directory.Delete(path, true);
                Debug.Log($"[AUB] Deleted cache directory: {path}");
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"[AUB] Failed to delete {path}: {ex.Message}");
            }
        }
    }
}
