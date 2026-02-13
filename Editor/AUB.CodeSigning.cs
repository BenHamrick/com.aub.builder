using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AUB
{
    /// <summary>
    /// macOS code signing and notarization for .app bundles.
    /// Only runs when AUB_CODESIGN_IDENTITY is set and the target is macOS.
    /// </summary>
    public static class CodeSigning
    {
        /// <summary>
        /// Sign a macOS .app bundle using codesign.
        /// </summary>
        /// <param name="appPath">Path to the .app bundle</param>
        /// <param name="identity">Code signing identity (e.g. "Developer ID Application: ...")</param>
        /// <returns>True if signing succeeded</returns>
        public static bool Sign(string appPath, string identity)
        {
#if UNITY_EDITOR_OSX
            Debug.Log($"[AUB] Signing {appPath} with identity: {identity}");

            var result = RunProcess("codesign",
                $"--force --deep --sign \"{identity}\" --options runtime \"{appPath}\"");

            if (result != 0)
            {
                Debug.LogError($"[AUB] Code signing failed with exit code {result}");
                return false;
            }

            Debug.Log("[AUB] Code signing succeeded.");
            return true;
#else
            Debug.LogWarning("[AUB] Code signing is only available on macOS.");
            return false;
#endif
        }

        /// <summary>
        /// Submit a signed .app for notarization using notarytool.
        /// Requires a keychain profile to be set up beforehand:
        /// xcrun notarytool store-credentials "PROFILE_NAME" --apple-id ... --team-id ... --password ...
        /// </summary>
        /// <param name="appPath">Path to the signed .app bundle</param>
        /// <param name="profile">Keychain profile name</param>
        /// <returns>True if notarization succeeded</returns>
        public static bool Notarize(string appPath, string profile)
        {
#if UNITY_EDITOR_OSX
            Debug.Log($"[AUB] Submitting {appPath} for notarization (profile: {profile})");

            // First, create a zip for submission
            var zipPath = appPath + ".zip";
            var zipResult = RunProcess("ditto",
                $"-c -k --keepParent \"{appPath}\" \"{zipPath}\"");

            if (zipResult != 0)
            {
                Debug.LogError($"[AUB] Failed to create zip for notarization (exit code {zipResult})");
                return false;
            }

            // Submit for notarization
            var notarizeResult = RunProcess("xcrun",
                $"notarytool submit \"{zipPath}\" --keychain-profile \"{profile}\" --wait");

            // Clean up zip
            try { System.IO.File.Delete(zipPath); } catch { }

            if (notarizeResult != 0)
            {
                Debug.LogError($"[AUB] Notarization failed with exit code {notarizeResult}");
                return false;
            }

            // Staple the notarization ticket
            var stapleResult = RunProcess("xcrun",
                $"stapler staple \"{appPath}\"");

            if (stapleResult != 0)
            {
                Debug.LogWarning($"[AUB] Stapling failed (exit code {stapleResult}), but notarization may have succeeded.");
            }
            else
            {
                Debug.Log("[AUB] Notarization and stapling succeeded.");
            }

            return true;
#else
            Debug.LogWarning("[AUB] Notarization is only available on macOS.");
            return false;
#endif
        }

        private static int RunProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(psi))
            {
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                    Debug.Log($"[AUB] {fileName}: {stdout.TrimEnd()}");
                if (!string.IsNullOrEmpty(stderr) && process.ExitCode != 0)
                    Debug.LogError($"[AUB] {fileName} stderr: {stderr.TrimEnd()}");

                return process.ExitCode;
            }
        }
    }
}
