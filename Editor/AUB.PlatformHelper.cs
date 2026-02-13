using System;
using System.Collections.Generic;
using UnityEditor;

namespace AUB
{
    /// <summary>
    /// Maps AUB build target strings to Unity BuildTarget enums and handles
    /// platform switching, build target groups, and subtargets.
    /// </summary>
    public static class PlatformHelper
    {
        private static readonly Dictionary<string, BuildTarget> TargetMap = new Dictionary<string, BuildTarget>(StringComparer.OrdinalIgnoreCase)
        {
            { "Win64", BuildTarget.StandaloneWindows64 },
            { "Win", BuildTarget.StandaloneWindows },
            { "Linux64", BuildTarget.StandaloneLinux64 },
            { "OSX", BuildTarget.StandaloneOSX },
            { "WebGL", BuildTarget.WebGL },
            { "Android", BuildTarget.Android },
            { "iOS", BuildTarget.iOS },
#if UNITY_2021_2_OR_NEWER
            { "Switch", BuildTarget.Switch },
#endif
            // Aliases for compatibility
            { "StandaloneWindows64", BuildTarget.StandaloneWindows64 },
            { "StandaloneLinux64", BuildTarget.StandaloneLinux64 },
            { "StandaloneOSX", BuildTarget.StandaloneOSX },
            { "windows", BuildTarget.StandaloneWindows64 },
            { "linux", BuildTarget.StandaloneLinux64 },
            { "macos", BuildTarget.StandaloneOSX },
            { "webgl", BuildTarget.WebGL },
            { "android", BuildTarget.Android },
            { "ios", BuildTarget.iOS },
        };

        /// <summary>
        /// Parse a AUB target string into a Unity BuildTarget enum.
        /// </summary>
        public static bool TryParseBuildTarget(string targetString, out BuildTarget target)
        {
            return TargetMap.TryGetValue(targetString, out target);
        }

        /// <summary>
        /// Get the BuildTargetGroup for a given BuildTarget.
        /// </summary>
        public static BuildTargetGroup GetTargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.WebGL:
                    return BuildTargetGroup.WebGL;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
#if UNITY_2021_2_OR_NEWER
                case BuildTarget.Switch:
                    return BuildTargetGroup.Switch;
#endif
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        /// <summary>
        /// Get the default output file/folder name for a target.
        /// </summary>
        public static string GetDefaultOutputName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "game.exe";
                case BuildTarget.StandaloneLinux64:
                    return "game.x86_64";
                case BuildTarget.StandaloneOSX:
                    return "game.app";
                case BuildTarget.WebGL:
                    return "webgl";
                case BuildTarget.Android:
                    return "game.apk";
                case BuildTarget.iOS:
                    return "ios-build";
                default:
                    return "build";
            }
        }

        /// <summary>
        /// Switch the active build target if it differs from the current one.
        /// Returns true if a switch was needed (and performed).
        /// </summary>
        public static bool SwitchBuildTarget(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                return false;

            var group = GetTargetGroup(target);
            UnityEngine.Debug.Log($"[AUB] Switching build target: {EditorUserBuildSettings.activeBuildTarget} -> {target} (group: {group})");
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            return true;
        }

        /// <summary>
        /// Get the StandaloneBuildSubtarget for server builds (Unity 2021.2+).
        /// </summary>
        public static int GetServerSubtarget()
        {
#if UNITY_2021_2_OR_NEWER
            return (int)StandaloneBuildSubtarget.Server;
#else
            return 0; // Not supported in older Unity
#endif
        }
    }
}
