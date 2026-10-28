using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor
{
    /// <summary>
    /// Bundles must be built with the exact Unity version the game uses. Any other editor version is
    /// reported once per session, and builds refuse to run.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityVersionGuard
    {
        private const string ReportedKey = "ModComponentSDK.UnityVersionReported";

        static UnityVersionGuard()
        {
            if (IsSupported || SessionState.GetBool(ReportedKey, false)) return;
            SessionState.SetBool(ReportedKey, true);
            Debug.LogError(Message);
        }

        public static bool IsSupported => IsSupportedVersion(Application.unityVersion);

        public static bool IsSupportedVersion(string unityVersion) =>
            unityVersion == SdkInfo.RequiredUnityVersion;

        public static string Message =>
            $"{SdkInfo.DisplayName} needs Unity {SdkInfo.RequiredUnityVersion}, the version The Long Dark uses. " +
            $"This editor is {Application.unityVersion}. Install {SdkInfo.RequiredUnityVersion} from Unity Hub " +
            "and reopen the project; building mods is disabled until then.";
    }
}
