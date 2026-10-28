using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Build
{
    /// <summary>Right-click → Build for a Mod asset. The Mod Hub's Build window supersedes this but uses the same pipeline.</summary>
    internal static class BuildMenu
    {
        private const string MenuPath = "Assets/ModComponent SDK/Build .modcomponent";

        [MenuItem(MenuPath, false, 1000)]
        private static void BuildSelected()
        {
            var mod = Selection.activeObject as Mod;
            if (mod == null) return;

            if (!ProjectSetup.IsJsonCatalogEnabled)
            {
                if (EditorUtility.DisplayDialog("Turn on JSON catalogs?", ProjectSetup.JsonCatalogMessage, "Turn on", "Cancel"))
                    ProjectSetup.EnableJsonCatalog();
                return;
            }

            var output = BuildPreferences.GetOutputDirectory(mod);
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                output = EditorUtility.SaveFolderPanel($"Where should {mod.DisplayName} be built?", output, string.Empty);
                if (string.IsNullOrEmpty(output)) return;
                BuildPreferences.SetOutputDirectory(mod, output);
            }

            var report = ModBuildPipeline.Build(mod, new ModBuildOptions { OutputDirectory = output });
            foreach (var message in report.Messages)
            {
                switch (message.Severity)
                {
                    case BuildMessageSeverity.Error: Debug.LogError($"[{mod.Id}] {message.Text}", mod); break;
                    case BuildMessageSeverity.Warning: Debug.LogWarning($"[{mod.Id}] {message.Text}", mod); break;
                }
            }

            if (report.Succeeded)
            {
                Debug.Log($"[{mod.Id}] Built {report.ArchivePath} in {report.Duration.TotalSeconds:0.0} s " +
                          $"({report.Warnings.Count()} warnings).", mod);
                EditorUtility.RevealInFinder(report.ArchivePath);
            }
            else
            {
                Debug.LogError($"[{mod.Id}] Build failed with {report.Errors.Count()} errors. See the messages above.", mod);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBuild() => Selection.activeObject is Mod;
    }

    /// <summary>Per-user, per-mod build settings, stored in EditorPrefs so machine paths stay out of the project.</summary>
    public static class BuildPreferences
    {
        private static string Key(Mod mod, string name) =>
            $"ModComponentSDK.{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mod))}.{name}";

        public static string GetOutputDirectory(Mod mod) => EditorPrefs.GetString(Key(mod, "Output"), string.Empty);

        public static void SetOutputDirectory(Mod mod, string path) => EditorPrefs.SetString(Key(mod, "Output"), path ?? string.Empty);
    }
}
