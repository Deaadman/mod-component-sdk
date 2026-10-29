using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;

namespace ModComponent.SDK.Editor.Build
{
    /// <summary>
    /// One-time project settings a mod build depends on. Changing them recompiles scripts, so they cannot
    /// be switched on in the middle of a build.
    /// </summary>
    public static class ProjectSetup
    {
        /// <summary>
        /// Addressables 2.x only writes a JSON catalog (the only kind ModComponent reads) when this scripting
        /// define is set for the build target; the EnableJsonCatalog setting alone still produces a .bin catalog.
        /// </summary>
        public const string JsonCatalogDefine = "ENABLE_JSON_CATALOG";

        public static bool IsJsonCatalogEnabled
        {
            get
            {
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out var symbols);
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                return symbols.Contains(JsonCatalogDefine) && (settings == null || settings.EnableJsonCatalog);
            }
        }

        /// <summary>Turns on JSON catalogs for Windows builds. Unity recompiles scripts afterwards.</summary>
        public static void EnableJsonCatalog()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.EnableJsonCatalog = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out var symbols);
            if (!symbols.Contains(JsonCatalogDefine))
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols.Append(JsonCatalogDefine).ToArray());
        }

        public const string JsonCatalogMessage =
            "ModComponent reads JSON Addressables catalogs, and this project builds binary ones. " +
            "Turn on JSON catalogs once (Unity recompiles scripts), then build again.";
    }
}
