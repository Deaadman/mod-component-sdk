#if UNITY_EDITOR
using ModComponent.Editor.SDK;
using ModComponent.SDK;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor
{
    internal static class EditorHub
    {
        internal static void DrawMainContent()
        {
            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawHeader();
            DrawGettingStartedSection();
            DrawCommunitySection();
            DrawVersionInfo();
            GUILayout.EndVertical();
        }

        private static void DrawHeader()
        {
            GUILayout.Label($"Welcome to {ModComponentSDK.SDK_NAME}!", ModComponentEditorStyles.CenteredLabelBold);
            GUILayout.Label($"Version {ModComponentSDK.SDK_VERSION}", EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawVersionInfo()
        {
            GUILayout.Space(10);
            GUILayout.Label("Supported Versions:", ModComponentEditorStyles.CenteredLabel);
            GUILayout.Label($"ModComponent - v{ModComponentSDK.MODCOMPONENT_VERSION}", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"The Long Dark - v{ModComponentSDK.TLD_VERSION}", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"Modders' Gear Toolbox - v{ModComponentSDK.MODDERSTOOLBOX_VERSION}", EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawGettingStartedSection()
        {
            GUILayout.Space(5);
            GUILayout.Label("To get started, explore the following links:", ModComponentEditorStyles.CenteredLabel);
            DrawButtonWithLink("Beginner's Guide", "https://github.com/Deaadman/ModComponentSDK/wiki/Beginner's-Guide");
            DrawButtonWithLink("Documentation", "https://github.com/Deaadman/ModComponentSDK/wiki");
            GUILayout.Space(10);
        }

        private static void DrawCommunitySection()
        {
            DrawSectionWithLinks("For the latest updates and to connect with the modding community, \ncheck out these links:", new Dictionary<string, string>
            {
                { "Changelog", "https://github.com/Deaadman/ModComponentSDK/releases" },
                { "Discord Server", "https://discord.gg/2mnXAZfGXQ" }
            });
        }

        private static void DrawButtonWithLink(string label, string url)
        {
            if (GUILayout.Button(label))
            {
                Application.OpenURL(url);
            }
        }

        private static void DrawSectionWithLinks(string sectionTitle, Dictionary<string, string> links)
        {
            GUILayout.Label(sectionTitle, EditorStyles.centeredGreyMiniLabel);
            foreach (var link in links)
            {
                DrawButtonWithLink(link.Key, link.Value);
            }
        }
    }

    internal class EditorHubWindow : EditorWindow
    {
        [MenuItem("ModComponent SDK/About", false, 110)]
        internal static void Init()
        {
            var window = GetWindow<EditorHubWindow>("About Page");
            window.minSize = new Vector2(400, 265);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            EditorHub.DrawMainContent();
            GUILayout.EndVertical();
        }
    }
}
#endif