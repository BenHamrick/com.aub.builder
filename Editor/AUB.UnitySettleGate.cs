using System;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;

using Debug = UnityEngine.Debug;

namespace AUB
{
    /// <summary>
    /// Blocks until Unity has finished all compilation and asset importing.
    /// Used to prevent "script class layout is incompatible" errors that occur
    /// when BuildPipeline.BuildPlayer is invoked before the editor has settled
    /// after a define change or asset refresh.
    ///
    /// Key design decisions:
    /// - Periodically calls AssetDatabase.Refresh to pump the editor pipeline,
    ///   preventing deadlocks where domain reload needs the main thread.
    /// - Checks EditorUtility.scriptCompilationFailed for early error detection.
    /// - Calls UnlockReloadAssemblies upfront to clear stale locks.
    /// - Uses progressive interventions at time thresholds.
    /// </summary>
    public static class UnitySettleGate
    {
        /// <summary>
        /// Waits until Unity is no longer compiling or updating assets.
        /// Uses periodic AssetDatabase.Refresh calls to pump the editor pipeline
        /// and avoid main-thread deadlocks that can occur with pure Thread.Sleep.
        /// </summary>
        /// <param name="context">Description of why we're waiting (for logs)</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait before throwing</param>
        public static void WaitForUnityToSettle(string context, int timeoutSeconds = 600) // 10 minutes
        {
            // Preventive: clear any stale assembly reload locks that could block domain reload
            EditorApplication.UnlockReloadAssemblies();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Fast exit: if Unity is already idle after the synchronous refresh, no need to poll
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                return;

            Debug.Log($"[AUB] Settle gate: waiting for Unity to finish ({context})...");

            var sw = Stopwatch.StartNew();
            var lastLogTime = -5.0; // force immediate first log
            int lastRefreshAt = 0;  // track which 10s interval we last refreshed at
            bool triedUnlock = false;

            while (true)
            {
                bool compiling = EditorApplication.isCompiling;
                bool updating = EditorApplication.isUpdating;

                if (!compiling && !updating)
                    break;

                double elapsed = sw.Elapsed.TotalSeconds;

                // Early detection: if compilation has errors, fail fast instead of waiting
                // for the full timeout. scriptCompilationFailed is true when the last
                // compile had errors, which can cause isCompiling to stay true indefinitely.
                if (compiling && EditorUtility.scriptCompilationFailed)
                {
                    throw new Exception(
                        $"[AUB] Script compilation errors detected during settle gate ({context}). " +
                        "Check the log output above for 'error CS' messages. " +
                        "Fix the compilation errors and retry the build.");
                }

                // Every 10 seconds, call Refresh to pump the editor pipeline.
                // This is critical: Thread.Sleep blocks the main thread, which can
                // prevent domain reload from completing after script compilation.
                // Periodic Refresh calls give Unity a chance to process pending work.
                int refreshInterval = (int)(elapsed / 10.0);
                if (refreshInterval > lastRefreshAt)
                {
                    lastRefreshAt = refreshInterval;
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                // At 60s: try unlocking assemblies again in case a plugin locked them
                if (!triedUnlock && elapsed > 60)
                {
                    triedUnlock = true;
                    Debug.LogWarning(
                        $"[AUB] Settle gate: 60s elapsed and still waiting ({context}). " +
                        "Attempting UnlockReloadAssemblies and forced refresh...");
                    EditorApplication.UnlockReloadAssemblies();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                // Log every 5 seconds
                if (elapsed - lastLogTime >= 5.0)
                {
                    Debug.Log($"[AUB] Waiting for Unity to settle ({context}): " +
                              $"isCompiling={compiling}, " +
                              $"isUpdating={updating} [{elapsed:F1}s elapsed]");
                    lastLogTime = elapsed;
                }

                if (elapsed > timeoutSeconds)
                {
                    throw new TimeoutException(
                        $"[AUB] Settle gate timed out after {timeoutSeconds}s ({context}). " +
                        $"isCompiling={compiling}, isUpdating={updating}. " +
                        "This usually means Unity's compilation pipeline is stuck. " +
                        "Try cleaning the Library folder (AUB.Builder.CleanBuildCache) or check for " +
                        "plugins that call LockReloadAssemblies without a matching unlock.");
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
