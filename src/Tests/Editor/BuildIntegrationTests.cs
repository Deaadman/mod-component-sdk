using System.IO;
using System.IO.Compression;
using System.Linq;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Modules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    /// <summary>Builds a real one-item mod through Addressables and checks the archive and the project afterwards.</summary>
    public class BuildIntegrationTests
    {
        private const string Folder = "Assets/ModComponentSDK_TestTemp";
        private string _output;
        private Mod _mod;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "ModComponentSDK_TestTemp");
            _output = Path.Combine(Path.GetTempPath(), "mcsdk-build-" + System.Guid.NewGuid().ToString("N"));

            var root = new GameObject("GEAR_TestCan");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/GEAR_TestCan.prefab");
            Object.DestroyImmediate(root);

            var texture = new Texture2D(8, 8);
            File.WriteAllBytes($"{Folder}/ico_GearItem__TestCan.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset($"{Folder}/ico_GearItem__TestCan.png");

            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = "GEAR_TestCan";
            item.Prefab = prefab;
            item.Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/ico_GearItem__TestCan.png");
            item.AddModule(new FoodComponent { Calories = 300, NormalModel = "Mesh" });
            AssetDatabase.CreateAsset(item, $"{Folder}/GEAR_TestCan.asset");

            _mod = ScriptableObject.CreateInstance<Mod>();
            _mod.DisplayName = "SDK Test";
            _mod.Id = "SdkTest";
            _mod.Items.Add(item);
            var localization = ScriptableObject.CreateInstance<LocalizationTable>();
            localization.Set("GAMEPLAY_TestCan", "English", "Test Can");
            AssetDatabase.CreateAsset(localization, $"{Folder}/Localization.asset");
            _mod.Localization = localization;
            AssetDatabase.CreateAsset(_mod, $"{Folder}/SdkTest.asset");
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            if (Directory.Exists(_output)) Directory.Delete(_output, true);
        }

        [Test]
        public void Build_ProducesLoadableArchive_AndRestoresAddressables()
        {
            if (!ProjectSetup.IsJsonCatalogEnabled) Assert.Ignore("This project builds binary Addressables catalogs; run ProjectSetup.EnableJsonCatalog once.");
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var groupsBefore = settings.groups.Select(g => g.Name).ToList();
            var includeBefore = settings.groups.Select(g => g.GetSchema<BundledAssetGroupSchema>()?.IncludeInBuild).ToList();
            var versionBefore = settings.OverridePlayerVersion;
            var remoteBefore = settings.BuildRemoteCatalog;

            var report = ModBuildPipeline.Build(_mod, new ModBuildOptions { OutputDirectory = _output });

            Assert.That(report.Succeeded, Is.True, string.Join("\n", report.Messages));
            Assert.That(report.Entries.First(), Is.EqualTo("buildinfo.json"));
            Assert.That(report.Entries, Has.Some.Match(@"^bundle/modcomponentsdk_sdktest_.*\.bundle$"));
            Assert.That(report.Entries, Does.Contain("bundle/catalog_sdktest.json"));
            Assert.That(report.Entries, Does.Contain("auto-mapped/testcan.json"));
            Assert.That(report.Entries, Does.Contain("localizations/sdktest.json"));
            Assert.That(report.Messages.Select(m => m.Text), Has.Some.Contains("CookingResult"));

            using (var stream = File.OpenRead(report.ArchivePath))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var catalogEntry = zip.Entries.Single(e => e.FullName.StartsWith("bundle/") && e.FullName.EndsWith(".json"));
                using var reader = new StreamReader(catalogEntry.Open());
                var catalog = JObject.Parse(reader.ReadToEnd());
                foreach (var key in new[] { "m_InternalIds", "m_KeyDataString", "m_BucketDataString", "m_EntryDataString", "m_ProviderIds" })
                    Assert.That(catalog[key], Is.Not.Null, $"catalog is missing {key}, which ModComponent parses");
                var internalIds = catalog["m_InternalIds"].ToObject<string[]>();
                Assert.That(internalIds, Has.Some.EndsWith(".bundle"));
                Assert.That(internalIds, Has.Some.Contains("GEAR_TestCan"));

                var itemEntry = zip.GetEntry("auto-mapped/testcan.json");
                using var itemReader = new StreamReader(itemEntry!.Open());
                var item = JObject.Parse(itemReader.ReadToEnd());
                Assert.That(item["ModFoodComponent"]["Calories"].Value<int>(), Is.EqualTo(300));
            }

            settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings.groups.Select(g => g.Name), Is.EqualTo(groupsBefore), "temporary group left behind");
            Assert.That(settings.groups.Select(g => g.GetSchema<BundledAssetGroupSchema>()?.IncludeInBuild), Is.EqualTo(includeBefore));
            Assert.That(settings.OverridePlayerVersion, Is.EqualTo(versionBefore));
            Assert.That(settings.BuildRemoteCatalog, Is.EqualTo(remoteBefore));
            Assert.That(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID($"{Folder}/GEAR_TestCan.prefab")), Is.Null,
                "prefab left as an Addressables entry");
        }

        [Test]
        public void Build_FailsOnDuplicateGearNames()
        {
            var copy = Object.Instantiate(_mod.Items[0]);
            _mod.Items.Add(copy);

            var report = ModBuildPipeline.Build(_mod, new ModBuildOptions { OutputDirectory = _output, SkipBundles = true });

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Errors.Select(e => e.Text), Has.Some.Contains("Gear names must be unique"));
            Object.DestroyImmediate(copy);
        }

        [Test]
        public void Build_FailsBeforeBundling_WhenPrefabNameDiffers()
        {
            _mod.Items[0].GearName = "GEAR_Other";

            var report = ModBuildPipeline.Build(_mod, new ModBuildOptions { OutputDirectory = _output });

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Errors.Select(e => e.Text), Has.Some.Contains("prefab is named GEAR_TestCan"));
            Assert.That(report.ArchivePath, Is.Null);
        }
    }
}
