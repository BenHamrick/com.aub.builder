using System;
using System.IO;
using UnityEngine;

namespace AUB
{
    /// <summary>
    /// Structured build result that gets written as JSON to the output directory.
    /// The runner reads this file after Unity exits to get detailed build info.
    /// </summary>
    [Serializable]
    public class BuildResultReport
    {
        public bool success;
        public string target;
        public string outputPath;
        public long totalSize;
        public float duration;
        public int warnings;
        public int errors;
        public string[] scenes;
        public string error;
        public string unityVersion;
        public string timestamp;

        private const string FileName = "aub-build-result.json";

        /// <summary>
        /// Write this result to the output directory as aub-build-result.json.
        /// </summary>
        public void WriteToDir(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var path = Path.Combine(outputDir, FileName);
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
            Debug.Log($"[AUB] Build result written to: {path}");
        }

        /// <summary>
        /// Create a success result.
        /// </summary>
        public static BuildResultReport Success(
            string target,
            string outputPath,
            long totalSize,
            float duration,
            int warnings,
            int errors,
            string[] scenes)
        {
            return new BuildResultReport
            {
                success = true,
                target = target,
                outputPath = outputPath,
                totalSize = totalSize,
                duration = duration,
                warnings = warnings,
                errors = errors,
                scenes = scenes,
                unityVersion = Application.unityVersion,
                timestamp = DateTime.UtcNow.ToString("O"),
            };
        }

        /// <summary>
        /// Create a failure result.
        /// </summary>
        public static BuildResultReport Failure(string target, string errorMessage, float duration)
        {
            return new BuildResultReport
            {
                success = false,
                target = target,
                error = errorMessage,
                duration = duration,
                scenes = Array.Empty<string>(),
                unityVersion = Application.unityVersion,
                timestamp = DateTime.UtcNow.ToString("O"),
            };
        }

        /// <summary>
        /// Calculate total size of all files in a directory recursively.
        /// </summary>
        public static long CalculateDirSize(string path)
        {
            if (File.Exists(path))
                return new FileInfo(path).Length;

            if (!Directory.Exists(path))
                return 0;

            long total = 0;
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(file).Length; }
                catch { /* skip inaccessible files */ }
            }
            return total;
        }
    }
}
