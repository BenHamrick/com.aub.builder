using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AUB
{
    /// <summary>
    /// Writes build version info to Assets/Resources/AUBBuildInfo.json
    /// so the game can read it at runtime via Resources.Load.
    /// </summary>
    public static class VersionStamper
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string BuildInfoFile = "Assets/Resources/AUBBuildInfo.json";

        /// <summary>
        /// Stamp build info into the project before building.
        /// Creates Assets/Resources/ if it doesn't exist.
        /// </summary>
        public static void Stamp(BuildConfig config, string buildTarget)
        {
            // Ensure Resources directory exists
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
                AssetDatabase.Refresh();
            }

            var info = new BuildInfo
            {
                buildId = config.BuildId,
                commitHash = config.CommitHash,
                branch = config.Branch,
                buildTarget = buildTarget,
                timestamp = DateTime.UtcNow.ToString("O"),
                aubVersion = "0.1.0"
            };

            var json = JsonUtility.ToJson(info, true);
            File.WriteAllText(BuildInfoFile, json);
            AssetDatabase.ImportAsset(BuildInfoFile);

            Debug.Log($"[AUB] Version stamped: commit={config.CommitHash}, build={config.BuildId}, target={buildTarget}");
        }

        /// <summary>
        /// Clean up the build info file after building (optional).
        /// </summary>
        public static void Cleanup()
        {
            if (File.Exists(BuildInfoFile))
            {
                File.Delete(BuildInfoFile);
                var metaFile = BuildInfoFile + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);
                AssetDatabase.Refresh();
            }
        }

        [Serializable]
        private class BuildInfo
        {
            public string buildId;
            public string commitHash;
            public string branch;
            public string buildTarget;
            public string timestamp;
            public string aubVersion;
        }
    }
}
