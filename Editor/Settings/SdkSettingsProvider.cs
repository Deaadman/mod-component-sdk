using System.Collections.Generic;
using System.IO;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Editor.UI;
using ModComponent.SDK.Editor.UI.Windows;
using ModComponent.SDK.Editor.Updates;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.Settings
{
    /// <summary>
    /// Preferences › ModComponent SDK. Everything here is per user (the game folder is a machine path), so it is
    /// stored in EditorPrefs rather than in the project.
    /// </summary>
    internal static class SdkSettingsProvider
    {
        private const string AuthorKey = "ModComponentSDK.DefaultAuthor";

        [SettingsProvider]
        public static SettingsProvider Create() => new("Preferences/ModComponent SDK", SettingsScope.User)
        {
            label = SdkInfo.DisplayName,
            keywords = new HashSet<string> { "ModComponent", "The Long Dark", "mod", "update", "game folder" },
            activateHandler = (_, root) => Build(root.WithStyles())
        };

        private static void Build(VisualElement root)
        {
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.Add(new Label(SdkInfo.DisplayName) { style = { fontSize = 19, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2, marginBottom = 8 } });

            var game = SdkUI.Section("The Long Dark", out var c);
            var folder = new TextField("Game folder") { value = BuildPreferences.GameFolder, isDelayed = true, tooltip = "The folder that contains TheLongDark.exe." };
            var folderHelp = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            void CheckFolder()
            {
                var ok = string.IsNullOrEmpty(folder.value) || File.Exists(Path.Combine(folder.value, "TheLongDark.exe"));
                folderHelp.text = "That folder doesn't contain TheLongDark.exe.";
                folderHelp.style.display = ok ? DisplayStyle.None : DisplayStyle.Flex;
            }

            folder.RegisterValueChangedCallback(e =>
            {
                BuildPreferences.GameFolder = e.newValue;
                CheckFolder();
            });
            c.Add(folder);
            c.Add(SdkUI.Row(new Button(() =>
            {
                var picked = EditorUtility.OpenFolderPanel("The Long Dark folder", folder.value, string.Empty);
                if (!string.IsNullOrEmpty(picked)) folder.value = picked;
            }) { text = "Choose…" }));
            c.Add(folderHelp);
            CheckFolder();
            root.Add(game);

            var authoring = SdkUI.Section("New mods", out c);
            var author = new TextField("Default author") { value = EditorPrefs.GetString(AuthorKey, string.Empty) };
            author.RegisterValueChangedCallback(e => EditorPrefs.SetString(AuthorKey, e.newValue));
            c.Add(author);
            root.Add(authoring);

            var updates = SdkUI.Section("Updates", out c);
            var check = new Toggle("Check on startup") { value = EditorPrefs.GetBool(UpdateService.CheckOnStartupKey, true) };
            check.RegisterValueChangedCallback(e => EditorPrefs.SetBool(UpdateService.CheckOnStartupKey, e.newValue));
            var nightlies = new Toggle("Include nightlies") { value = UpdateService.IncludePrereleases, tooltip = "Offer 2.x.y-nightly.N releases as updates." };
            nightlies.RegisterValueChangedCallback(e => UpdateService.IncludePrereleases = e.newValue);
            c.Add(check);
            c.Add(nightlies);
            c.Add(SdkUI.Row(new Button(UpdateService.Check) { text = "Check now" }));
            root.Add(updates);

            var status = SdkUI.Section("Project status", out c);
            c.Add(StatusLine(UnityVersionGuard.IsSupported, $"Unity {Application.unityVersion}",
                UnityVersionGuard.IsSupported ? "matches The Long Dark" : $"needs {SdkInfo.RequiredUnityVersion}"));
            c.Add(StatusLine(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64, "Build target",
                EditorUserBuildSettings.activeBuildTarget.ToString()));
            c.Add(StatusLine(ProjectSetup.IsJsonCatalogEnabled, "JSON catalogs", ProjectSetup.IsJsonCatalogEnabled ? "on" : "off"));
            if (!ProjectSetup.IsJsonCatalogEnabled) c.Add(SdkUI.Row(new Button(ProjectSetup.EnableJsonCatalog) { text = "Turn on JSON catalogs" }));
            try
            {
                var catalog = GameDataCatalog.Default;
                c.Add(new Label($"Game data catalog: lists from TLD {catalog.GameVersion} ({catalog.Gear.Count} items, " +
                                $"{catalog.Sounds.Count} sounds, {catalog.Scenes.Count} scenes, {catalog.LootTables.Count} loot tables). " +
                                $"The SDK targets TLD {SdkInfo.TargetGameVersion} and ModComponent {SdkInfo.TargetModComponentVersion}.")
                    { style = { whiteSpace = WhiteSpace.Normal, marginTop = 4 } });
            }
            catch (System.Exception e)
            {
                c.Add(new HelpBox(e.Message, HelpBoxMessageType.Error));
            }

            c.Add(SdkUI.Row(new Button(ModHubWindow.Open) { text = "Open Mod Hub" }, new Button(MigrationWindow.Open) { text = "Import 1.x mods…" }));
            root.Add(status);
        }

        private static VisualElement StatusLine(bool ok, string name, string detail)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new Image { image = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.erroricon.sml").image, style = { width = 16, height = 16, marginRight = 4 } });
            row.Add(new Label($"{name}: {detail}"));
            return row;
        }
    }
}
