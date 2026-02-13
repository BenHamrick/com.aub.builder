using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AUB
{
    /// <summary>
    /// Manages scripting define symbols for builds.
    /// Injects AUB-specified defines without losing existing project defines.
    /// Restores original defines after building.
    /// </summary>
    public static class DefineManager
    {
        private static string _savedDefines;
        private static BuildTargetGroup _savedGroup;
#if UNITY_2023_1_OR_NEWER
        private static UnityEditor.Build.NamedBuildTarget SavedNamedTarget => UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_savedGroup);
#endif

        /// <summary>
        /// Inject additional scripting defines for the build.
        /// Call RestoreDefines() after the build completes.
        /// </summary>
        /// <param name="group">The build target group to modify</param>
        /// <param name="defines">Semicolon-separated defines to add (e.g. "STEAMWORKS_NET;DEBUG_MODE")</param>
        public static void InjectDefines(BuildTargetGroup group, string defines)
        {
            if (string.IsNullOrEmpty(defines))
                return;

            // Save current state for restoration
            _savedGroup = group;
#if UNITY_2023_1_OR_NEWER
            PlayerSettings.GetScriptingDefineSymbols(SavedNamedTarget, out string[] savedArr);
            _savedDefines = string.Join(";", savedArr);
#else
            _savedDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

            // Merge: existing + new (dedup)
            var existing = _savedDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var additional = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var merged = new HashSet<string>(existing);
            foreach (var d in additional)
                merged.Add(d.Trim());

            var result = string.Join(";", merged);

#if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(SavedNamedTarget, merged.ToArray());
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, result);
#endif

            var added = additional.Where(d => !existing.Contains(d.Trim())).ToArray();
            if (added.Length > 0)
                Debug.Log($"[AUB] Injected scripting defines: {string.Join(", ", added)}");
        }

        /// <summary>
        /// Restore scripting defines to their pre-build state.
        /// </summary>
        public static void RestoreDefines()
        {
            if (_savedDefines == null)
                return;

#if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(SavedNamedTarget, _savedDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(_savedGroup, _savedDefines);
#endif

            Debug.Log("[AUB] Restored original scripting defines.");
            _savedDefines = null;
        }
    }
}
