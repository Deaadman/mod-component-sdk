#if UNITY_EDITOR
using ModComponent.SDK;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor.SDK
{
    internal class EditorValidateUpdates : EditorValidateBase
    {
        private CheckStatus exampleModStatus = CheckStatus.Pending;
        private CheckStatus modComponentStatus = CheckStatus.Pending;

        [MenuItem("ModComponent SDK/Check for Updates...", false, 50)]
        internal static void ShowWindow()
        {
            var window = GetWindow<EditorValidateUpdates>("Update Checker");
            window.minSize = new Vector2(320, 300);
            window.StartUpdateCheck();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            DrawStatus("Example Mod", ref exampleModStatus);
            DrawStatus("ModComponent SDK", ref modComponentStatus);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Check for Preview Updates", GUILayout.Width(170));
            bool checkForPreviewUpdates = EditorPrefs.GetBool("CheckForPreviewUpdates", false);
            bool newCheckForPreviewUpdates = EditorGUILayout.Toggle(checkForPreviewUpdates, GUILayout.Width(20));
            GUILayout.EndHorizontal();

            if (newCheckForPreviewUpdates != checkForPreviewUpdates)
            {
                EditorPrefs.SetBool("CheckForPreviewUpdates", newCheckForPreviewUpdates);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Automatically Check for SDK Updates", GUILayout.Width(230));
            bool automaticallyCheckForUpdates = EditorPrefs.GetBool("AutomaticallyCheckForUpdates", false);
            bool newAutomaticallyCheckForUpdates = EditorGUILayout.Toggle(automaticallyCheckForUpdates, GUILayout.Width(20));
            GUILayout.EndHorizontal();

            if (newAutomaticallyCheckForUpdates != automaticallyCheckForUpdates)
            {
                EditorPrefs.SetBool("AutomaticallyCheckForUpdates", newAutomaticallyCheckForUpdates);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private async void StartUpdateCheck()
        {
            exampleModStatus = CheckStatus.Checking;

            bool isExampleModInstalled = Directory.Exists("Assets/_ModComponent/ExampleMod");
            if (!isExampleModInstalled)
            {
                exampleModStatus = CheckStatus.Failed;
            }
            else
            {
                bool exampleModUpdateAvailable = await AutoUpdater.InitializeUpdateCheck("examplemod");
                exampleModStatus = exampleModUpdateAvailable ? CheckStatus.Waiting : CheckStatus.Success;
            }

            modComponentStatus = CheckStatus.Checking;
            bool modComponentUpdateAvailable = await AutoUpdater.InitializeUpdateCheck("modcomponent");
            modComponentStatus = modComponentUpdateAvailable ? CheckStatus.Waiting : CheckStatus.Success;
        }

        protected override string GetStatusMessage(string baseLabel, CheckStatus status)
        {
            return status switch
            {
                CheckStatus.Checking => $"Checking {baseLabel} for updates...",
                CheckStatus.Success => $"{baseLabel} is up to date.",
                CheckStatus.Failed => $"{baseLabel} isn't installed.",
                CheckStatus.Pending => $"{baseLabel} update status not checked.",
                CheckStatus.Waiting => $"New update available for {baseLabel}.",
                _ => baseLabel,
            };
        }
    }
}
#endif