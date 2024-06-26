#if UNITY_EDITOR
using ModComponent.SDK;
using System;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor.SDK
{
    internal class EditorAutoUpdater : EditorWindow
    {
        private static string _currentVersion;
        private static string _latestVersion;
        private static string _latestVersionChanges;
        private static string _packageName;
        private static Action _onClose;
        private Vector2 _scrollPosition;

        internal static void Init(string currentVer, string latestVer, string changes, string packageName, Action onClose)
        {
            _currentVersion = currentVer;
            _latestVersion = latestVer;
            _latestVersionChanges = MarkdownToRichText(changes);
            _packageName = packageName;
            _onClose = onClose;
            var window = GetWindow<EditorAutoUpdater>("Auto Updater");
            window.minSize = new Vector2(720, 650);
            window.Show();
        }

        private void OnGUI()
        {
            var richTextStyle = new GUIStyle(GUI.skin.textArea) { richText = true };

            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DisplayHeader();
            DisplayVersionInfo();
            DisplayChangelog(richTextStyle);
            DisplayActionButtons();
            GUILayout.EndVertical();
        }

        private void DisplayHeader()
        {
            GUILayout.Label($"{_packageName} Auto Updater", ModComponentEditorStyles.CenteredLabelBold);
            GUILayout.Label($"A newer version is available for {_packageName}.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(10);
        }

        private void DisplayVersionInfo()
        {
            GUILayout.Label($"Current Version: {_currentVersion}", ModComponentEditorStyles.CenteredLabel);
            GUILayout.Space(5);
            GUILayout.Label($"Latest Version: {_latestVersion}", ModComponentEditorStyles.CenteredLabelBold);
            GUILayout.Space(10);
        }

        private void DisplayChangelog(GUIStyle richTextStyle)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            GUILayout.Label("What's New:", EditorStyles.boldLabel);
            GUILayout.TextArea(_latestVersionChanges, richTextStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private void DisplayActionButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Remind Me Later", GUILayout.Width(120)))
            {
                Close();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Update Now", GUILayout.Width(120)))
            {
                AutoUpdater.UpdatePackage("com.deadman.modcomponent.sdk", _latestVersion);
                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static string MarkdownToRichText(string markdown)
        {
            var lines = markdown.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].StartsWith("##") ? $"<size=15>{lines[i][2..].Trim()}</size>" : lines[i];
            }

            markdown = string.Join("\n", lines);
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*(.*?)\*\*", "<b>$1</b>");
            return markdown.Replace("`", "");
        }

        private void OnDestroy()
        {
            _onClose?.Invoke();
        }
    }
}
#endif