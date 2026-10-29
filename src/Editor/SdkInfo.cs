using UnityEditor.PackageManager;

namespace ModComponent.SDK.Editor
{
    /// <summary>Versions this build of the SDK targets.</summary>
    public static class SdkInfo
    {
        public const string PackageName = "com.deadman.modcomponent.sdk";
        public const string DisplayName = "ModComponent SDK";
        public const string RepositoryUrl = "https://github.com/Deaadman/mod-component-sdk";

        /// <summary>The only editor version supported: the Unity version The Long Dark is built with.</summary>
        public const string RequiredUnityVersion = "6000.0.60f1";

        public const string TargetGameVersion = "2.58";
        public const string TargetModComponentVersion = "7.0.0";

        private static PackageInfo _package;

        public static PackageInfo Package => _package ??= PackageInfo.FindForAssembly(typeof(SdkInfo).Assembly);

        /// <summary>Installed package version, e.g. 2.0.0 or 2.0.0-nightly.20261029.42.</summary>
        public static string Version => Package?.version ?? "0.0.0";

        /// <summary>Absolute path of the package folder on disk.</summary>
        public static string PackagePath => Package?.resolvedPath;
    }
}
