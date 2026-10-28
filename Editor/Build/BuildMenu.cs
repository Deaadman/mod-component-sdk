using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Build
{
    /// <summary>Build entry points shared by the Project window menu, the Mod inspector and the Mod Hub.</summary>
    public static class BuildActions
    {
        /// <summary>
        /// Builds the mod to its remembered output folder, asking for one the first time, and logs the result.
        /// Offers to turn on JSON catalogs if the project still builds binary ones.
        /// </summary>
        public static BuildReport Build(Mod mod)
        {
            if (mod == null) return null;

            if (!ProjectSetup.IsJsonCatalogEnabled)
            {
                if (EditorUtility.DisplayDialog("Turn on JSON catalogs?", ProjectSetup.JsonCatalogMessage, "Turn on", "Cancel"))
                    ProjectSetup.EnableJsonCatalog();
                return null;
            }

            var output = BuildPreferences.GetOutputDirectory(mod);
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                output = EditorUtility.SaveFolderPanel($"Where should {mod.DisplayName} be built?", output, string.Empty);
                if (string.IsNullOrEmpty(output)) return null;
                BuildPreferences.SetOutputDirectory(mod, output);
            }

            var copyTo = BuildPreferences.GetCopyToGame(mod) ? BuildPreferences.GameModsFolder : null;
            var report = ModBuildPipeline.Build(mod, new ModBuildOptions { OutputDirectory = output, CopyToDirectory = copyTo });
            Log(mod, report);
            BuildPreferences.LastReport = report;
            return report;
        }

        public static void Log(Mod mod, BuildReport report)
        {
            foreach (var message in report.Messages)
            {
                switch (message.Severity)
                {
                    case BuildMessageSeverity.Error: Debug.LogError($"[{mod.Id}] {message.Text}", mod); break;
                    case BuildMessageSeverity.Warning: Debug.LogWarning($"[{mod.Id}] {message.Text}", mod); break;
                }
            }

            if (report.Succeeded)
                Debug.Log($"[{mod.Id}] Built {report.ArchivePath} in {report.Duration.TotalSeconds:0.0} s ({report.Warnings.Count()} warnings).", mod);
            else
                Debug.LogError($"[{mod.Id}] Build failed with {report.Errors.Count()} errors. See the messages above.", mod);
        }
    }

    internal static class BuildMenu
    {
        private const string MenuPath = "Assets/ModComponent SDK/Build .modcomponent";

        [MenuItem(MenuPath, false, 1000)]
        private static void BuildSelected()
        {
            var report = BuildActions.Build(Selection.activeObject as Mod);
            if (report is { Succeeded: true }) EditorUtility.RevealInFinder(report.ArchivePath);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBuild() => Selection.activeObject is Mod;
    }

    /// <summary>Per-user build settings, kept in EditorPrefs so machine paths stay out of the project.</summary>
    public static class BuildPreferences
    {
        private const string GameFolderKey = "ModComponentSDK.GameFolder";

        private static string Key(Mod mod, string name) =>
            $"ModComponentSDK.{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mod))}.{name}";

        public static string GetOutputDirectory(Mod mod) => EditorPrefs.GetString(Key(mod, "Output"), string.Empty);

        public static void SetOutputDirectory(Mod mod, string path) => EditorPrefs.SetString(Key(mod, "Output"), path ?? string.Empty);

        public static bool GetCopyToGame(Mod mod) => EditorPrefs.GetBool(Key(mod, "CopyToGame"), false) && !string.IsNullOrEmpty(GameModsFolder);

        public static void SetCopyToGame(Mod mod, bool value) => EditorPrefs.SetBool(Key(mod, "CopyToGame"), value);

        /// <summary>The Long Dark's install folder, e.g. C:/Program Files (x86)/Steam/steamapps/common/TheLongDark.</summary>
        public static string GameFolder
        {
            get => EditorPrefs.GetString(GameFolderKey, string.Empty);
            set => EditorPrefs.SetString(GameFolderKey, value ?? string.Empty);
        }

        /// <summary>The game's Mods folder, or null when the game folder isn't set or doesn't exist.</summary>
        public static string GameModsFolder =>
            !string.IsNullOrEmpty(GameFolder) && Directory.Exists(GameFolder) ? Path.Combine(GameFolder, "Mods") : null;

        /// <summary>The most recent build this session, for the Hub's footer.</summary>
        public static BuildReport LastReport { get; set; }
    }
}
