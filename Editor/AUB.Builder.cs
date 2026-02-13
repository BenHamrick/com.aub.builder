using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AUB
{
    /// <summary>
    /// Main AUB build entry point.
    /// Called via: Unity -batchmode -quit -executeMethod AUB.Builder.Build
    ///
    /// Reads configuration from environment variables set by the AUB runner,
    /// executes the build with proper platform switching and define injection,
    /// and writes a structured result JSON for the runner to consume.
    /// </summary>
    public static class Builder
    {
        /// <summary>
        /// Main build entry point. Called by the runner via -executeMethod.
        /// Exits with code 0 on success, 1 on failure.
        /// </summary>
        public static void Build()
        {
            var startTime = DateTime.UtcNow;
            Debug.Log("[AUB] ═══════════════════════════════════════════");
            Debug.Log("[AUB] AUB Builder starting...");

            // 1. Read config from environment
            var config = BuildConfig.FromEnvironment();
            if (!config.IsValid)
            {
                Debug.LogError($"[AUB] Configuration error: {config.Error}");
                WriteFailureAndExit(config, "Configuration error: " + config.Error, startTime);
                return;
            }
            Debug.Log($"[AUB] Config: {config}");

            // 2. Resolve Unity build target
            if (!PlatformHelper.TryParseBuildTarget(config.BuildTarget, out var buildTarget))
            {
                var msg = $"Unknown build target: '{config.BuildTarget}'";
                Debug.LogError($"[AUB] {msg}");
                WriteFailureAndExit(config, msg, startTime);
                return;
            }

            // 3. Version stamp
            if (!string.IsNullOrEmpty(config.BuildId) || !string.IsNullOrEmpty(config.CommitHash))
            {
                VersionStamper.Stamp(config, config.BuildTarget);
            }

            // 4. Switch build target if needed
            PlatformHelper.SwitchBuildTarget(buildTarget);
            var targetGroup = PlatformHelper.GetTargetGroup(buildTarget);

            // 5. Inject scripting defines
            DefineManager.InjectDefines(targetGroup, config.Defines);

            try
            {
                // 6. Build the player
                var outputDir = config.OutputDir;
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var outputName = PlatformHelper.GetDefaultOutputName(buildTarget);
                var outputPath = Path.Combine(outputDir, outputName);

                // Collect scenes
                var scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                    .Select(s => s.path)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    var msg = "No scenes found in Build Settings. Add at least one scene.";
                    Debug.LogError($"[AUB] {msg}");
                    WriteFailureAndExit(config, msg, startTime);
                    return;
                }

                Debug.Log($"[AUB] Building {scenes.Length} scene(s) for {buildTarget} -> {outputPath}");

                var buildOptions = BuildOptions.None;
#if UNITY_2021_2_OR_NEWER
                if (config.ServerBuild)
                {
                    EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
                    Debug.Log("[AUB] Server build subtarget enabled.");
                }
#endif

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = buildTarget,
                    targetGroup = targetGroup,
                    options = buildOptions,
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                var duration = (float)(DateTime.UtcNow - startTime).TotalSeconds;

                // 7. Process result
                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[AUB] Build succeeded in {duration:F1}s");
                    Debug.Log($"[AUB] Output: {outputPath}");
                    Debug.Log($"[AUB] Size: {report.summary.totalSize:N0} bytes");
                    Debug.Log($"[AUB] Warnings: {report.summary.totalWarnings}, Errors: {report.summary.totalErrors}");

                    // Calculate actual output size on disk
                    var diskSize = AUB.BuildResult.CalculateDirSize(outputPath);

                    var result = AUB.BuildResult.Success(
                        config.BuildTarget,
                        outputPath,
                        diskSize > 0 ? diskSize : (long)report.summary.totalSize,
                        duration,
                        report.summary.totalWarnings,
                        report.summary.totalErrors,
                        scenes
                    );
                    result.WriteToDir(outputDir);

                    // 8. macOS code signing (optional)
                    if (buildTarget == UnityEditor.BuildTarget.StandaloneOSX && !string.IsNullOrEmpty(config.CodesignIdentity))
                    {
                        CodeSigning.Sign(outputPath, config.CodesignIdentity);

                        if (!string.IsNullOrEmpty(config.NotarizeProfile))
                        {
                            CodeSigning.Notarize(outputPath, config.NotarizeProfile);
                        }
                    }

                    Debug.Log("[AUB] ═══════════════════════════════════════════");
                    Debug.Log("[AUB] BUILD SUCCEEDED");
                    // Exit code 0 (default for successful -quit)
                }
                else
                {
                    var errorMsg = $"Build failed with result: {report.summary.result}";
                    Debug.LogError($"[AUB] {errorMsg}");

                    // Log build errors
                    foreach (var step in report.steps)
                    {
                        foreach (var msg in step.messages)
                        {
                            if (msg.type == LogType.Error)
                                Debug.LogError($"[AUB] Build error: {msg.content}");
                        }
                    }

                    var failResult = AUB.BuildResult.Failure(config.BuildTarget, errorMsg, duration);
                    failResult.warnings = report.summary.totalWarnings;
                    failResult.errors = report.summary.totalErrors;
                    failResult.scenes = scenes;
                    failResult.WriteToDir(outputDir);

                    Debug.Log("[AUB] ═══════════════════════════════════════════");
                    Debug.Log("[AUB] BUILD FAILED");
                    EditorApplication.Exit(1);
                }
            }
            finally
            {
                // Always restore defines, even on failure
                DefineManager.RestoreDefines();
            }
        }

        /// <summary>
        /// Clean build cache entry point.
        /// Called via: Unity -batchmode -quit -executeMethod AUB.Builder.CleanBuildCache
        /// </summary>
        public static void CleanBuildCache()
        {
            Debug.Log("[AUB] Cleaning build cache...");
            BeeCleanup.Clean();
        }

        private static void WriteFailureAndExit(BuildConfig config, string error, DateTime startTime)
        {
            var duration = (float)(DateTime.UtcNow - startTime).TotalSeconds;
            var result = AUB.BuildResult.Failure(config?.BuildTarget ?? "unknown", error, duration);

            var outputDir = config?.OutputDir;
            if (!string.IsNullOrEmpty(outputDir))
            {
                result.WriteToDir(outputDir);
            }

            EditorApplication.Exit(1);
        }
    }
}
