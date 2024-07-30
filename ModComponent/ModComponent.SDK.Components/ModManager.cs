#if UNITY_EDITOR
using ModComponent.Blueprints;
using ModComponent.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ModComponent.SDK.Components
{
    internal class ModManager
    {
        internal const string ModAssetsPath = "Assets/_ModComponent";
        private const string AssetBundlesPath = ".\\AssetBundles\\";

        internal static Dictionary<string, string> backupComponentData = new();

        private static void AddAssetsToAddressablesGroup(UnityEngine.Object[] assets, AddressableAssetGroup group)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
            }
        }

        private static void AppendRaritySection(StringBuilder sb, string rarityName, IEnumerable<GameObject> gameObjects)
        {
            if (gameObjects == null || !gameObjects.Any()) return;

            sb.AppendLine($"[{rarityName}]");
            foreach (var gameObject in gameObjects)
            {
                if (gameObject is not null)
                {
                    sb.AppendLine(gameObject.name);
                }
            }
            sb.AppendLine();
        }
        
        private static void ClearAssetBundlesDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                ClearDirectory(directoryPath);
            }
            else
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static void ClearDirectory(string directoryPath, string excludeExtension = null)
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                FileInfo fileInfo = new(file);
                if (excludeExtension == null || !fileInfo.Extension.Equals(excludeExtension, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(dir, true);
            }
        }

        internal static List<ModDefinition> GetAllMods()
        {
            List<ModDefinition> modDefinitions = new();

            string[] guids = AssetDatabase.FindAssets("t:ModDefinition", new[] { ModAssetsPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ModDefinition modDefinition = AssetDatabase.LoadAssetAtPath<ModDefinition>(assetPath);
                if (modDefinition != null)
                {
                    modDefinitions.Add(modDefinition);
                }
            }

            return modDefinitions;
        }

        private static void ClearModFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                ClearDirectory(folderPath, ".asset");
            }
            else
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        private static void CopyAssetBundlesToFolder(string bundlesFolderPath)
        {
            foreach (var file in Directory.EnumerateFiles(AssetBundlesPath))
            {
                if (!file.EndsWith(".hash"))
                {
                    string sanitizedFileName = FileUtilities.SanitizeFileName(Path.GetFileName(file), true);
                    string destFile = Path.Combine(bundlesFolderPath, sanitizedFileName);
                    File.Copy(file, destFile, true);
                }
            }
        }

        private static void CreateBuildInfoJson(ModDefinition modDefinition, string filePath)
        {
            var buildInfo = new ModDefinition.BuildInfo
            {
                Name = modDefinition.Name,
                Author = modDefinition.Author,
                Version = modDefinition.Version,
                Requires = modDefinition.RequiredMods,
                RequiresDLC = modDefinition.RequiresDLC
            };

            string json = JsonUtility.ToJson(buildInfo, true);
            File.WriteAllText(filePath, json);
        }

        private static void ExportGearSpawns(ModDefinition modDefinition, string modFolderPath)
        {
            if (modDefinition.modGearSpawns != null)
            {
                string gearSpawnsFolderPath = Path.Combine(modFolderPath, "gear-spawns");
                if (!Directory.Exists(gearSpawnsFolderPath))
                {
                    Directory.CreateDirectory(gearSpawnsFolderPath);
                }

                StringBuilder sb = new();

                foreach (var sceneSpawnEntry in modDefinition.modGearSpawns.sceneGearSpawnEntries)
                {
                    if (sceneSpawnEntry.sceneName != null)
                    {
                        sb.AppendLine($"scene={sceneSpawnEntry.sceneName.name}");
                        foreach (var itemSpawn in sceneSpawnEntry.sceneItemSpawns)
                        {
                            sb.Append($"item={itemSpawn.itemName.name} p={FormatVector3(itemSpawn.position)}");
                            if (itemSpawn.rotation != Vector3.zero)
                            {
                                sb.Append($" r={FormatVector3(itemSpawn.rotation)}");
                            }
                            sb.Append($" c={itemSpawn.spawnChance}");
                            sb.AppendLine();
                        }
                    }
                }

                sb.AppendLine();

                foreach (var lootTableSpawnEntry in modDefinition.modGearSpawns.lootTableGearSpawnEntries)
                {
                    if (lootTableSpawnEntry.lootTableName != null)
                    {
                        sb.AppendLine($"loottable={lootTableSpawnEntry.lootTableName.name}");
                        foreach (var lootTableSpawn in lootTableSpawnEntry.lootTableSpawns)
                        {
                            sb.AppendLine($"item={lootTableSpawn.itemName.name} w={lootTableSpawn.weight}");
                        }
                    }
                }

                File.WriteAllText(Path.Combine(gearSpawnsFolderPath, "gearspawns.txt"), sb.ToString());
            }
        }

        private static void ExportItemRarities(ModDefinition modDefinition, string modFolderPath)
        {
            if (modDefinition.modItemRarities is null) return;

            StringBuilder sb = new();

            AppendRaritySection(sb, "Common", modDefinition.modItemRarities.Common);
            AppendRaritySection(sb, "Uncommon", modDefinition.modItemRarities.Uncommon);
            AppendRaritySection(sb, "Rare", modDefinition.modItemRarities.Rare);
            AppendRaritySection(sb, "Epic", modDefinition.modItemRarities.Epic);
            AppendRaritySection(sb, "Legendary", modDefinition.modItemRarities.Legendary);
            AppendRaritySection(sb, "Mythic", modDefinition.modItemRarities.Mythic);

            File.WriteAllText(Path.Combine(modFolderPath, "rarities.ir"), sb.ToString());
        }
        
        private static void ExportLocalization(ModDefinition modDefinition, string modFolderPath)
        {
            if (modDefinition.modLocalization != null)
            {
                var localizationData = ExtractLocalizationData(modDefinition);
                string json = JsonConvert.SerializeObject(localizationData, Formatting.Indented);
                SaveLocalizationJson(modFolderPath, json);
            }
        }

        internal static void ExportModAsModComponent(ModDefinition modDefinition, string outputPath)
        {
            var group = AddressablesManager.CreatePackedAssetsGroup(FileUtilities.SanitizeFileName(modDefinition.Name));
            AddressablesManager.ConfigureDefaultAddressableSettings(modDefinition.Name);

            foreach (var item in modDefinition.Items)
            {
                var prefab = item as GameObject;
                if (prefab != null)
                    BackupAndRemoveComponents(prefab);
            }

            AddAssetsToAddressablesGroup(modDefinition.Items, group);
            AddAssetsToAddressablesGroup(modDefinition.Icons, group);

            ClearAssetBundlesDirectory(AssetBundlesPath);
            AddressableAssetSettings.BuildPlayerContent();

            foreach (var item in modDefinition.Items)
            {
                var prefab = item as GameObject;
                if (prefab != null)
                    RestoreComponents(prefab);
            }

            string modFolderPath = PrepareModFolder(modDefinition);
            CreateBuildInfoJson(modDefinition, Path.Combine(modFolderPath, "BuildInfo.json"));
            ExportLocalization(modDefinition, modFolderPath);
            ExportGearSpawns(modDefinition, modFolderPath);
            ExportItemRarities(modDefinition, modFolderPath);
            SerializeModComponentToJson(modDefinition, modFolderPath);

            string modComponentPath = outputPath ?? $"{modDefinition.Name}.modcomponent";
            PackageModComponent(modFolderPath, modComponentPath);
        }

        private static string FormatVector3(Vector3 vector)
        {
            return $"{vector.x},{vector.y},{vector.z}";
        }

        private static void PackageModComponent(string modFolderPath, string modComponentPath)
        {
            if (File.Exists(modComponentPath))
            {
                File.Delete(modComponentPath);
            }

            using var zip = ZipFile.Open(modComponentPath, ZipArchiveMode.Create);
            DirectoryInfo di = new(modFolderPath);
            foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
            {
                if (file.Extension != ".meta" && file.Extension != ".asset")
                {
                    string fullFilePath = Path.GetFullPath(file.FullName);
                    string relativePath = FileUtilities.SanitizeFileName(fullFilePath[(di.FullName.Length + 1)..], true).Replace("\\", "/");
                    zip.CreateEntryFromFile(fullFilePath, relativePath, CompressionLevel.Optimal);
                }
            }
        }

        private static string PrepareModFolder(ModDefinition modDefinition)
        {
            string modFolderPath = Path.Combine(ModAssetsPath, modDefinition.Name);
            ClearModFolder(modFolderPath);

            string bundlesFolderPath = Path.Combine(modFolderPath, "bundle");
            Directory.CreateDirectory(bundlesFolderPath);
            CopyAssetBundlesToFolder(bundlesFolderPath);

            return modFolderPath;
        }

        private static void SaveLocalizationJson(string modFolderPath, string json)
        {
            string localizationFolderPath = Path.Combine(modFolderPath, "localizations");
            Directory.CreateDirectory(localizationFolderPath);
            File.WriteAllText(Path.Combine(localizationFolderPath, "Localization.json"), json);
        }

        private static void SerializeBlueprints(ModDefinition modDefinition, string modFolderPath)
        {
            string blueprintsFolderPath = Path.Combine(modFolderPath, "blueprints");
            string recipesFolderPath = Path.Combine(modFolderPath, "recipes");
            Directory.CreateDirectory(blueprintsFolderPath);
            Directory.CreateDirectory(recipesFolderPath);

            foreach (var prefab in modDefinition.Items)
            {
                foreach (var blueprint in prefab.GetComponents<ModBlueprint>())
                {
                    var serializedBlueprint = DataSerializer.SerializeBlueprint(blueprint);
                    string json = JsonConvert.SerializeObject(serializedBlueprint, Formatting.Indented);
                    string jsonFileName = prefab.name.StartsWith("GEAR_") ? prefab.name[5..] : prefab.name;

                    string folderPath = blueprint is ModRecipe ? recipesFolderPath : blueprintsFolderPath;
                    string jsonFilePath = Path.Combine(folderPath, jsonFileName + ".json");

                    File.WriteAllText(jsonFilePath, json);
                }
            }
        }


        private static void SerializeModComponentToJson(ModDefinition modDefinition, string modFolderPath)
        {
            string autoMappedFolderPath = Path.Combine(modFolderPath, "auto-mapped");
            Directory.CreateDirectory(autoMappedFolderPath);

            foreach (var prefab in modDefinition.Items)
            {
                SerializePrefab(prefab, autoMappedFolderPath);
            }

            SerializeBlueprints(modDefinition, modFolderPath);
        }

        private static void SerializePrefab(GameObject prefab, string folderPath)
        {
            var componentData = new Dictionary<string, object>();

            foreach (var component in prefab.GetComponents<MonoBehaviour>())
            {
                if (component is ModBlueprint) continue;

                var serializedComponent = DataSerializer.SerializeComponent(component);
                if (serializedComponent != null)
                {
                    string componentTypeName = component.GetType().Name;
                    componentData[componentTypeName] = serializedComponent;
                }
            }

            string json = JsonConvert.SerializeObject(componentData, Formatting.Indented);
            string jsonFileName = prefab.name.StartsWith("GEAR_") ? prefab.name[5..] : prefab.name;
            string jsonFilePath = Path.Combine(folderPath, jsonFileName + ".json");
            File.WriteAllText(jsonFilePath, json);
        }

        private static Dictionary<string, Dictionary<string, string>> ExtractLocalizationData(ModDefinition modDefinition)
        {
            var localizationData = new Dictionary<string, Dictionary<string, string>>();

            foreach (var entry in modDefinition.modLocalization.localizationEntries)
            {
                var translations = new Dictionary<string, string>
                {
                    { "English", entry.languages.English },
                    { "German", entry.languages.German },
                    { "Russian", entry.languages.Russian },
                    { "French (France)", entry.languages.French },
                    { "Japanese", entry.languages.Japanese },
                    { "Korean", entry.languages.Korean },
                    { "Simplified Chinese", entry.languages.SimplifiedChinese },
                    { "Swedish", entry.languages.Swedish },
                    { "Traditional Chinese", entry.languages.TraditionalChinese },
                    { "Turkish", entry.languages.Turkish },
                    { "Norwegian", entry.languages.Norwegian },
                    { "Spanish (Spain)", entry.languages.Spanish },
                    { "Portuguese (Portugal)", entry.languages.PortuguesePortugal },
                    { "Portuguese (Brazil)", entry.languages.PortugueseBrazil },
                    { "Dutch", entry.languages.Dutch },
                    { "Finnish", entry.languages.Finnish },
                    { "Italian", entry.languages.Italian },
                    { "Polish", entry.languages.Polish },
                    { "Ukrainian", entry.languages.Ukrainian }
                };

                localizationData.Add(entry.localizationKey, translations);
            }

            return localizationData;
        }

        private static void BackupAndRemoveComponents(GameObject prefab)
        {
            var components = prefab.GetComponents<MonoBehaviour>();
            var componentData = new Dictionary<string, object>();

            foreach (var component in components)
            {
                var jsonData = JsonUtility.ToJson(component);
                componentData[component.GetType().FullName] = jsonData;
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            string backupJson = JsonConvert.SerializeObject(componentData, Formatting.Indented);
            backupComponentData[prefab.name] = backupJson;
        }

        private static void RestoreComponents(GameObject prefab)
        {
            if (!backupComponentData.TryGetValue(prefab.name, out var jsonData))
                return;

            var componentData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            foreach (var data in componentData)
            {
                var type = Type.GetType(data.Key);
                if (type != null)
                {
                    var component = prefab.AddComponent(type);
                    JsonUtility.FromJsonOverwrite(data.Value, component);
                }
            }
        }
    }
}
#endif