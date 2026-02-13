using System;
using UnityEngine;

namespace AUB
{
    /// <summary>
    /// Reads build configuration from environment variables set by the AUB runner.
    /// </summary>
    public class BuildConfig
    {
        /// <summary>Build target string (e.g. "Win64", "Linux64", "WebGL", "OSX")</summary>
        public string BuildTarget { get; private set; }

        /// <summary>Output directory for the build</summary>
        public string OutputDir { get; private set; }

        /// <summary>Whether this is a dedicated server build (subtarget)</summary>
        public bool ServerBuild { get; private set; }

        /// <summary>Semicolon-separated scripting defines to inject</summary>
        public string Defines { get; private set; }

        /// <summary>AUB build ID for version stamping</summary>
        public string BuildId { get; private set; }

        /// <summary>Git commit hash for version stamping</summary>
        public string CommitHash { get; private set; }

        /// <summary>Git branch name</summary>
        public string Branch { get; private set; }

        /// <summary>Optional path to a Unity Build Profile asset</summary>
        public string BuildProfile { get; private set; }

        /// <summary>macOS code signing identity (optional)</summary>
        public string CodesignIdentity { get; private set; }

        /// <summary>macOS notarization keychain profile (optional)</summary>
        public string NotarizeProfile { get; private set; }

        /// <summary>Whether the config was loaded successfully</summary>
        public bool IsValid { get; private set; }

        /// <summary>Validation error message if not valid</summary>
        public string Error { get; private set; }

        private BuildConfig() { }

        /// <summary>
        /// Read build config from environment variables.
        /// Required: AUB_BUILD_TARGET, AUB_OUTPUT_DIR
        /// Optional: AUB_SERVER_BUILD, AUB_DEFINES, AUB_BUILD_ID, AUB_COMMIT_HASH,
        ///           AUB_BRANCH, AUB_BUILD_PROFILE, AUB_CODESIGN_IDENTITY, AUB_NOTARIZE_PROFILE
        /// </summary>
        public static BuildConfig FromEnvironment()
        {
            var config = new BuildConfig();

            config.BuildTarget = GetEnv("AUB_BUILD_TARGET");
            config.OutputDir = GetEnv("AUB_OUTPUT_DIR");
            config.ServerBuild = GetEnv("AUB_SERVER_BUILD") == "true";
            config.Defines = GetEnv("AUB_DEFINES") ?? "";
            config.BuildId = GetEnv("AUB_BUILD_ID") ?? "";
            config.CommitHash = GetEnv("AUB_COMMIT_HASH") ?? "";
            config.Branch = GetEnv("AUB_BRANCH") ?? "";
            config.BuildProfile = GetEnv("AUB_BUILD_PROFILE") ?? "";
            config.CodesignIdentity = GetEnv("AUB_CODESIGN_IDENTITY") ?? "";
            config.NotarizeProfile = GetEnv("AUB_NOTARIZE_PROFILE") ?? "";

            // Validate required fields
            if (string.IsNullOrEmpty(config.BuildTarget))
            {
                config.IsValid = false;
                config.Error = "AUB_BUILD_TARGET environment variable is required but not set.";
                return config;
            }

            if (string.IsNullOrEmpty(config.OutputDir))
            {
                config.IsValid = false;
                config.Error = "AUB_OUTPUT_DIR environment variable is required but not set.";
                return config;
            }

            config.IsValid = true;
            return config;
        }

        private static string GetEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        public override string ToString()
        {
            return $"BuildConfig(target={BuildTarget}, output={OutputDir}, server={ServerBuild}, " +
                   $"defines={Defines}, buildId={BuildId}, commit={CommitHash})";
        }
    }
}
