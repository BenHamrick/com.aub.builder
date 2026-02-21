using System;
using System.Diagnostics;
using System.Threading;
using UnityEditor;

using Debug = UnityEngine.Debug;

namespace AUB
{
    /// <summary>
    /// Blocks until Unity has finished all compilation and asset importing.
    /// Used to prevent "script class layout is incompatible" errors that occur
    /// when BuildPipeline.BuildPlayer is invoked before the editor has settled
    /// after a platform switch or define change.
    /// </summary>
    public static class UnitySettleGate
    {
        /// <summary>
        /// Polls until Unity is no longer compiling or updating assets.
        /// Safe to call in batchmode (uses Thread.Sleep, no coroutines).
        /// </summary>
        /// <param name="context">Description of why we're waiting (for logs)</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait before throwing</param>
        public static void WaitForUnityToSettle(string context, int timeoutSeconds = 600)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var sw = Stopwatch.StartNew();
            var lastLogTime = -5.0; // force immediate first log if busy

            while (true)
            {
                bool compiling = EditorApplication.isCompiling;
                bool updating = EditorApplication.isUpdating;

                if (!compiling && !updating)
                    break;

                if (sw.Elapsed.TotalSeconds - lastLogTime >= 5.0)
                {
                    Debug.Log($"[AUB] Waiting for Unity to settle ({context}): " +
                              $"isCompiling={compiling}, " +
                              $"isUpdating={updating} [{sw.Elapsed.TotalSeconds:F1}s elapsed]");
                    lastLogTime = sw.Elapsed.TotalSeconds;
                }

                if (sw.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    throw new TimeoutException(
                        $"[AUB] Unity did not settle within {timeoutSeconds}s ({context}). " +
                        $"isCompiling={compiling}, isUpdating={updating}");
                }

                Thread.Sleep(200);
            }

            double waited = sw.Elapsed.TotalSeconds;
            if (waited > 0.5)
            {
                Debug.Log($"[AUB] Unity settled ({context}) after {waited:F1}s");
            }
        }
    }
}
