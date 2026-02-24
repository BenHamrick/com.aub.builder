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
            // Xbox Series X|S (GameCore) — current gen
#if UNITY_6000_0_OR_NEWER
            { "xbox", BuildTarget.GameCoreXboxSeries },
            { "xboxseries", BuildTarget.GameCoreXboxSeries },
            { "GameCoreXboxSeries", BuildTarget.GameCoreXboxSeries },
#elif UNITY_2020_2_OR_NEWER
            { "xbox", BuildTarget.GameCoreScarlett },
            { "xboxseries", BuildTarget.GameCoreScarlett },
            { "GameCoreScarlett", BuildTarget.GameCoreScarlett },
#endif
            // Xbox One (GameCore) — Xbox One via modern GDK pipeline
#if UNITY_2020_2_OR_NEWER
            { "xboxone", BuildTarget.GameCoreXboxOne },
            { "GameCoreXboxOne", BuildTarget.GameCoreXboxOne },
#endif
            // Xbox One (Legacy) — old Xbox One pipeline
            { "xboxone-legacy", BuildTarget.XboxOne },
            // PlayStation
            { "ps4", BuildTarget.PS4 },
#if UNITY_2020_2_OR_NEWER
            { "ps5", BuildTarget.PS5 },
#endif
            // UWP / Windows Store
            { "uwp", BuildTarget.WSAPlayer },
            { "wsa", BuildTarget.WSAPlayer },
            { "WSAPlayer", BuildTarget.WSAPlayer },
            // tvOS
            { "tvos", BuildTarget.tvOS },
            // VisionOS (Apple Vision Pro)
#if UNITY_6000_0_OR_NEWER
            { "visionos", BuildTarget.VisionOS },
#endif
            // Embedded Linux
#if UNITY_2021_2_OR_NEWER
            { "embeddedlinux", BuildTarget.EmbeddedLinux },
#endif
            // QNX
#if UNITY_6000_0_OR_NEWER
            { "qnx", BuildTarget.QNX },
#endif
            // Aliases for compatibility
            { "StandaloneWindows64", BuildTarget.StandaloneWindows64 },
            { "StandaloneLinux64", BuildTarget.StandaloneLinux64 },
            { "StandaloneOSX", BuildTarget.StandaloneOSX },
            { "windows", BuildTarget.StandaloneWindows64 },
            { "linux", BuildTarget.StandaloneLinux64 },
            { "macos", BuildTarget.StandaloneOSX },
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
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.GameCoreXboxSeries:
                    return BuildTargetGroup.GameCoreXboxSeries;
#elif UNITY_2020_2_OR_NEWER
                case BuildTarget.GameCoreScarlett:
                    return BuildTargetGroup.GameCoreScarlett;
#endif
#if UNITY_2020_2_OR_NEWER
                case BuildTarget.GameCoreXboxOne:
                    return BuildTargetGroup.GameCoreXboxOne;
#endif
                case BuildTarget.XboxOne:
                    return BuildTargetGroup.XboxOne;
                case BuildTarget.PS4:
                    return BuildTargetGroup.PS4;
#if UNITY_2020_2_OR_NEWER
                case BuildTarget.PS5:
                    return BuildTargetGroup.PS5;
#endif
                case BuildTarget.WSAPlayer:
                    return BuildTargetGroup.WSA;
                case BuildTarget.tvOS:
                    return BuildTargetGroup.tvOS;
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.VisionOS:
                    return BuildTargetGroup.VisionOS;
#endif
#if UNITY_2021_2_OR_NEWER
                case BuildTarget.EmbeddedLinux:
                    return BuildTargetGroup.EmbeddedLinux;
#endif
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.QNX:
                    return BuildTargetGroup.QNX;
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
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.GameCoreXboxSeries:
#elif UNITY_2020_2_OR_NEWER
                case BuildTarget.GameCoreScarlett:
#endif
#if UNITY_2020_2_OR_NEWER
                    return "xboxseries-build";
                case BuildTarget.GameCoreXboxOne:
                    return "xboxone-build";
#endif
                case BuildTarget.XboxOne:
                    return "xboxone-legacy-build";
                case BuildTarget.PS4:
                    return "ps4-build";
#if UNITY_2020_2_OR_NEWER
                case BuildTarget.PS5:
                    return "ps5-build";
#endif
                case BuildTarget.WSAPlayer:
                    return "uwp-build";
                case BuildTarget.tvOS:
                    return "tvos-build";
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.VisionOS:
                    return "visionos-build";
#endif
#if UNITY_2021_2_OR_NEWER
                case BuildTarget.EmbeddedLinux:
                    return "embeddedlinux-build";
#endif
#if UNITY_6000_0_OR_NEWER
                case BuildTarget.QNX:
                    return "qnx-build";
#endif
                default:
                    return "build";
            }
        }

        /// <summary>
        /// Switch the active build target if it differs from the current one.
        /// Returns true if a switch was needed (and performed).
        /// </summary>
        [System.Obsolete("SwitchActiveBuildTarget is unsupported in batchmode. Pass -buildTarget on the Unity command line instead.")]
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
