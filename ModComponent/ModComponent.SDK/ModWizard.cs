#if UNITY_EDITOR
using ModComponent.SDK.Components;
using ModComponent.Utilities;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK
{
    internal class ModWizard : ScriptableWizard
    {
        [Tooltip("The name of this Mod Definition.")]
        public string modName = "My Mod";

        [Tooltip("The author of this Mod Definition.")]
        public string modAuthor = "Author";

        [Tooltip("Will this mod be compatible with Item Rarities?")]
        public bool itemRaritiesCompatibility = false;
        
        private static readonly string modFolderName = "_ModComponent";
        private ModItemRarities modItemRarities;
        
        [MenuItem("ModComponent SDK/New Mod Definition", false, 20)]
        private static void CreateWizard()
        {
            DisplayWizard<ModWizard>("New Mod Definition", "Create");
        }

        void OnWizardCreate()
        {
            var modDefinition = ModDefinition.CreateModDefinition(modName, modAuthor);

            var mainFolderPath = Path.Combine("Assets", modFolderName);
            if (!Directory.Exists(mainFolderPath))
            {
                Directory.CreateDirectory(mainFolderPath);
            }

            var specificFolderPath = Path.Combine(mainFolderPath, modName);
            if (!Directory.Exists(specificFolderPath))
            {
                Directory.CreateDirectory(specificFolderPath);
            }

            var assetPath = Path.Combine(specificFolderPath, FileUtilities.SanitizeFileName(modName) + ".asset");
            AssetDatabase.CreateAsset(modDefinition, assetPath);

            var localizationAssetPath = Path.Combine(specificFolderPath, FileUtilities.SanitizeFileName(modName) + "Localization.asset");
            var modLocalization = CreateInstance<ModLocalization>();
            AssetDatabase.CreateAsset(modLocalization, localizationAssetPath);

            var gearSpawnsAssetPath = Path.Combine(specificFolderPath, FileUtilities.SanitizeFileName(modName) + "GearSpawns.asset");
            var modGearSpawns = CreateInstance<ModGearSpawns>();
            AssetDatabase.CreateAsset(modGearSpawns, gearSpawnsAssetPath);

            if (itemRaritiesCompatibility)
            {
                var itemRaritiesAssetPath = Path.Combine(specificFolderPath, FileUtilities.SanitizeFileName(modName) + "ItemRarities.asset");
                var modItemRarities = CreateInstance<ModItemRarities>();
                AssetDatabase.CreateAsset(modItemRarities, itemRaritiesAssetPath);
            }
            
            modDefinition.modLocalization = modLocalization;
            modDefinition.modGearSpawns = modGearSpawns;

            if (itemRaritiesCompatibility)
            {
                modDefinition.modItemRarities = modItemRarities;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void OnWizardUpdate()
        {
            helpString = "Fill out the below details to create a new Mod Definition asset.\nThis lays down the groundwork for creating a custom ModComponent.";

            errorString = "";
            if (string.IsNullOrEmpty(modName))
            {
                errorString += "Missing Name!";
            }
            if (string.IsNullOrEmpty(modAuthor))
            {
                if (!string.IsNullOrEmpty(errorString))
                    errorString += "\n";
                errorString += "Missing Author!";
            }

            isValid = string.IsNullOrEmpty(errorString);
        }
    }
}
#endif