#if UNITY_EDITOR
using ModComponent.Utilities;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace ModComponent.SDK
{
    internal class AddressablesManager
    {
        internal static AddressableAssetGroup CreatePackedAssetsGroup(string groupName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings != null ? AddressableAssetSettingsDefaultObject.Settings : AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                                                           AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                                                           true,
                                                           true);

            var existingGroup = settings.FindGroup(groupName);
            if (existingGroup != null)
            {
                return existingGroup;
            }

            var group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema));

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema != null)
            {
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            }

            return group;
        }

        internal static void ConfigureDefaultAddressableSettings(string modName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings != null ? AddressableAssetSettingsDefaultObject.Settings : AddressableAssetSettingsDefaultObject.Settings =
                           AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                                                           AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                                                           true,
                                                           true);

            settings.OverridePlayerVersion = FileUtilities.SanitizeFileName(modName);

            // settings.ShaderBundleNaming = ShaderBundleNaming.Custom;
            // settings.ShaderBundleCustomNaming = FileUtilities.SanitizeFileName(modName);

            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        }

        [InitializeOnLoadMethod]
        private static void SetCustomLocalBuildPath()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return;
            }

            var profileSettings = settings.profileSettings;
            var profileId = settings.activeProfileId;

            var pathVar = profileSettings.GetValueByName(profileId, AddressableAssetSettings.kLocalBuildPath);
            if (pathVar == null)
            {
                profileSettings.CreateValue(AddressableAssetSettings.kLocalBuildPath, @".\AssetBundles\");
                profileSettings.CreateValue(AddressableAssetSettings.kLocalLoadPath, @".\AssetBundles\");
            }
            else
            {
                profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalBuildPath, @".\AssetBundles\");
                profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalLoadPath, @".\AssetBundles\");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif