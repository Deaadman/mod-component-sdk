#if UNITY_EDITOR
using ModComponent.Editor.SDK;
using ModComponent.SDK.Components;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK
{
    internal struct UpdateInfo
    {
        public string CurrentVersion;
        public string LatestVersion;
        public string LatestVersionChanges;
        public string PackageName;

        public UpdateInfo(string currentVersion, string latestVersion, string changes, string packageName)
        {
            CurrentVersion = currentVersion;
            LatestVersion = latestVersion;
            LatestVersionChanges = changes;
            PackageName = packageName;
        }
    }

    [InitializeOnLoad]
    internal class AutoUpdater
    {
        private static readonly string ModComponentVersion = ModComponentSDK.SDK_VERSION;
        private static string LatestModComponentVersion;
        private static string LatestExampleModVersion;
        private static string LatestVersionChanges;
        private static readonly Queue<UpdateInfo> updateQueue = new();
        private static bool isUpdateWindowOpen = false;

        static AutoUpdater()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool("AutomaticallyCheckForUpdates", true))
                {
                    CheckForUpdatesOnStartup();
                }
            };
        }

        private static async void CheckForUpdatesOnStartup()
        {
            await InitializeUpdateCheck("modcomponent");
        }

        private static void AddToUpdateQueue(UpdateInfo updateInfo)
        {
            updateQueue.Enqueue(updateInfo);
            ShowNextUpdateWindow();
        }

        private static async Task DownloadAndImportUnityPackage(string downloadUrl, string localPath)
        {
            using var client = new WebClient();
            TaskCompletionSource<bool> tcs = new();

            client.DownloadFileCompleted += (sender, e) =>
            {
                if (e.Error == null && !e.Cancelled)
                {
                    AssetDatabase.ImportPackage(localPath, true);
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(e.Error ?? new Exception("Download was cancelled."));
                }
            };

            client.DownloadFileAsync(new Uri(downloadUrl), localPath);
            await tcs.Task;
        }

        private static async Task<bool> FetchLatestReleaseInfoAsync(string repoPath, string currentVersion, string packageName)
        {
            bool checkForPreviewUpdates = EditorPrefs.GetBool("CheckForPreviewUpdates", false);

            try
            {
                using var client = new WebClient();
                string url = checkForPreviewUpdates
                    ? $"https://api.github.com/repos/{repoPath}/releases"
                    : $"https://api.github.com/repos/{repoPath}/releases/latest";

                client.Headers.Add("User-Agent", "Unity web request");
                string json = await client.DownloadStringTaskAsync(new Uri(url));

                // If checking for preview updates, select the most recent release or pre-release
                if (checkForPreviewUpdates)
                {
                    var jsonArray = JArray.Parse(json);
                    var latestRelease = jsonArray.FirstOrDefault();
                    if (latestRelease == null) return false;
                    ProcessReleaseInfo(latestRelease, currentVersion, packageName);
                }
                else
                {
                    var jsonObject = JObject.Parse(json);
                    ProcessReleaseInfo(jsonObject, currentVersion, packageName);
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private static bool ProcessReleaseInfo(JToken releaseInfo, string currentVersion, string packageName)
        {
            string latestVersion = releaseInfo["tag_name"].ToString().TrimStart('v');
            currentVersion = currentVersion.TrimStart('v');

            if (latestVersion != currentVersion)
            {
                LatestVersionChanges = releaseInfo["body"].ToString();
                if (packageName == "ModComponent SDK")
                {
                    LatestModComponentVersion = latestVersion;
                }
                else
                {
                    LatestExampleModVersion = latestVersion;
                }
                return true;
            }
            return false;
        }

        private static string GetInstalledExampleModVersion()
        {
            var exampleModAsset = AssetDatabase.LoadAssetAtPath<ModDefinition>("Assets/_ModComponent/ExampleMod/ExampleMod.asset");
            return exampleModAsset != null ? exampleModAsset.Version : null;
        }

        internal static async Task<bool> InitializeUpdateCheck(string updateType)
        {
            string currentVersion = updateType == "modcomponent" ? ModComponentVersion : GetInstalledExampleModVersion();
            string repoPath = updateType == "modcomponent" ? "Deaadman/ModComponentSDK" : "Deaadman/ExampleModSDK";
            string packageName = updateType == "modcomponent" ? "ModComponent SDK" : "Example Mod";

            bool updateAvailable = await FetchLatestReleaseInfoAsync(repoPath, currentVersion, packageName);
            if (updateAvailable)
            {
                string latestVersion = updateType == "modcomponent" ? LatestModComponentVersion : LatestExampleModVersion;
                UpdateInfo updateInfo = new(currentVersion, latestVersion, LatestVersionChanges, packageName);
                AddToUpdateQueue(updateInfo);
            }

            return updateAvailable;
        }

        private static void OpenUpdateWindow(UpdateInfo updateInfo)
        {
            isUpdateWindowOpen = true;
            EditorAutoUpdater.Init(updateInfo.CurrentVersion, updateInfo.LatestVersion, updateInfo.LatestVersionChanges, updateInfo.PackageName,
                () =>
                {
                    isUpdateWindowOpen = false;
                    ShowNextUpdateWindow();
                });
        }

        internal static async Task<bool> PromptAndInstallExampleModIfNeeded()
        {
            string exampleModPath = "Assets/_ModComponent/ExampleMod";
            if (Directory.Exists(exampleModPath))
            {
                return true;
            }

            bool installExampleMod = EditorUtility.DisplayDialog(
                "Example Mod Package Installer",
                "The Example Mod is not installed. Would you like to install it now?",
                "Install", "Skip");

            if (installExampleMod)
            {
                string tempFilePath = Path.Combine(Path.GetTempPath(), "ExampleMod.unitypackage");
                string downloadUrl = "https://github.com/Deadman/ExampleModSDK/releases/latest/download/ExampleMod.unitypackage";
                await DownloadAndImportUnityPackage(downloadUrl, tempFilePath);
            }
            return false;
        }


        private static void ShowNextUpdateWindow()
        {
            if (!isUpdateWindowOpen && updateQueue.Count > 0)
            {
                EditorApplication.update += TryOpenNextUpdateWindow;
            }
        }

        private static void TryOpenNextUpdateWindow()
        {
            if (!isUpdateWindowOpen && updateQueue.Count > 0)
            {
                var updateInfo = updateQueue.Dequeue();
                OpenUpdateWindow(updateInfo);
                EditorApplication.update -= TryOpenNextUpdateWindow;
            }
        }

        internal static void UpdateExampleMod(string latestVersion)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "ExampleMod.unitypackage");
            _ = DownloadAndImportUnityPackage("https://github.com/Deadman/ExampleModSDK/releases/latest/download/ExampleMod.unitypackage", tempFilePath);
        }

        internal static void UpdatePackage(string packageName, string latestVersion)
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("manifest.json not found");
                return;
            }

            string manifestContent = File.ReadAllText(manifestPath);
            var manifestJson = JObject.Parse(manifestContent);
            JToken packageToken = manifestJson["dependencies"][packageName];

            if (packageToken == null)
            {
                Debug.LogError($"{packageName} not found in manifest.json");
                return;
            }

            manifestJson["dependencies"][packageName] = latestVersion;
            File.WriteAllText(manifestPath, manifestJson.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"{packageName} updated successfully.");
        }
    }
}
#endif