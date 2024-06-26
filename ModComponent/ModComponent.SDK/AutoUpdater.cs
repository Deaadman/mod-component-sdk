#if UNITY_EDITOR
using ModComponent.Editor.SDK;
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
            await InitializeUpdateCheck();
        }

        private static void AddToUpdateQueue(UpdateInfo updateInfo)
        {
            updateQueue.Enqueue(updateInfo);
            ShowNextUpdateWindow();
        }

        private static async Task<bool> FetchLatestReleaseInfoAsync(string currentVersion)
        {
            bool checkForPreviewUpdates = EditorPrefs.GetBool("CheckForPreviewUpdates", false);

            try
            {
                using var client = new WebClient();
                string url = checkForPreviewUpdates
                    ? "https://api.github.com/repos/Deaadman/ModComponentSDK/releases"
                    : "https://api.github.com/repos/Deaadman/ModComponentSDK/releases/latest";

                client.Headers.Add("User-Agent", "Unity web request");
                string json = await client.DownloadStringTaskAsync(new Uri(url));

                if (checkForPreviewUpdates)
                {
                    var jsonArray = JArray.Parse(json);
                    var latestRelease = jsonArray.FirstOrDefault();
                    if (latestRelease == null) return false;
                    return ProcessReleaseInfo(latestRelease, currentVersion);
                }
                else
                {
                    var jsonObject = JObject.Parse(json);
                    return ProcessReleaseInfo(jsonObject, currentVersion);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool ProcessReleaseInfo(JToken releaseInfo, string currentVersion)
        {
            string latestVersion = releaseInfo["tag_name"].ToString().TrimStart('v');
            currentVersion = currentVersion.TrimStart('v');

            if (latestVersion != currentVersion)
            {
                LatestVersionChanges = releaseInfo["body"].ToString();
                LatestModComponentVersion = latestVersion;
                return true;
            }
            Debug.Log($"ModComponent SDK is up-to-date!");
            return false;
        }

        internal static async Task<bool> InitializeUpdateCheck()
        {
            bool updateAvailable = await FetchLatestReleaseInfoAsync(ModComponentVersion);
            if (updateAvailable)
            {
                UpdateInfo updateInfo = new(ModComponentVersion, LatestModComponentVersion, LatestVersionChanges, "ModComponent SDK");
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