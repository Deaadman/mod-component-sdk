using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Integrations;
using ModComponent.SDK.Editor.Migration;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.ItemRarities.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using V1Definition = ModComponent.SDK.Components.ModDefinition;
using V1Rarities = ModComponent.SDK.Components.ModItemRarities;

namespace ModComponent.SDK.ItemRarities.Editor.Tests
{
    public class ItemRaritiesTests
    {
        private const string Folder = "Assets/ItemRaritiesTests";

        private Mod _mod;

        [SetUp]
        public void SetUp() => _mod = ScriptableObject.CreateInstance<Mod>();

        [TearDown]
        public void TearDown()
        {
            // Saved assets go with the folder; only in-memory ones are destroyed.
            foreach (var item in _mod.Items.Where(i => i != null && !EditorUtility.IsPersistent(i))) Object.DestroyImmediate(item);
            if (!EditorUtility.IsPersistent(_mod)) Object.DestroyImmediate(_mod);
            AssetDatabase.DeleteAsset(Folder);
        }

        private GearItem AddItem(string gearName, Rarity? rarity = null)
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = gearName;
            if (rarity != null) item.GetOrAddIntegrationData<ItemRarityData>().Rarity = rarity.Value;
            _mod.Items.Add(item);
            return item;
        }

        [Test]
        public void Registry_FindsTheIntegrationItsDataAndExporter()
        {
            var info = IntegrationRegistry.Get(typeof(ItemRaritiesIntegration));

            Assert.That(info.DisplayName, Is.EqualTo("Item Rarities"));
            Assert.That(info.Exporter, Is.InstanceOf<ItemRaritiesExporter>());
            Assert.That(info.ItemData.Single().Type, Is.EqualTo(typeof(ItemRarityData)));
            Assert.That(info.ItemData.Single().Label, Is.EqualTo("Has a rarity"));
        }

        [Test]
        public void Export_GroupsItemsByRarity()
        {
            AddItem("GEAR_A", Rarity.Epic);
            AddItem("GEAR_B");
            AddItem("GEAR_C", Rarity.Common);
            var integration = _mod.EnableIntegration<ItemRaritiesIntegration>();

            var files = IntegrationRegistry.Get(integration).Exporter.Export(_mod, integration).ToList();

            Assert.That(files.Single().Key, Is.EqualTo("rarities.ir"));
            Assert.That(files.Single().Value, Is.EqualTo("[Common]\nGEAR_C\n\n[Epic]\nGEAR_A\n\n"));
        }

        [Test]
        public void Export_WritesNothingWithoutRarities()
        {
            AddItem("GEAR_A");
            var integration = _mod.EnableIntegration<ItemRaritiesIntegration>();

            Assert.That(new ItemRaritiesExporter().Export(_mod, integration), Is.Empty);
        }

        [Test]
        public void Packager_AcceptsRaritiesAtTheRootOnly()
        {
            _mod.EnableIntegration<ItemRaritiesIntegration>();
            var extensions = IntegrationRegistry.RootExtensions(_mod);

            Assert.That(ModPackager.ToEntryName("rarities.ir", out _, extensions), Is.EqualTo("rarities.ir"));
            Assert.That(ModPackager.ToEntryName("extras/rarities.ir", out _, extensions), Is.Null);
            Assert.That(ModPackager.ToEntryName("rarities.ir", out _), Is.Null, "Only with the integration on");
        }

        [Test]
        public void Rule_WarnsWhenNoItemHasARarity()
        {
            var item = AddItem("GEAR_A");
            _mod.EnableIntegration<ItemRaritiesIntegration>();

            var issue = new ItemRaritiesRule().Check(new ValidationContext(_mod, null)).Single();
            Assert.That(issue.Severity, Is.EqualTo(Severity.Warning));

            issue.Fix();
            Assert.That(item.GetIntegrationData<ItemRarityData>().Rarity, Is.EqualTo(Rarity.Common));
            Assert.That(new ItemRaritiesRule().Check(new ValidationContext(_mod, null)), Is.Empty);
        }

        [Test]
        public void Rule_NotesItemsWithoutARarity()
        {
            AddItem("GEAR_A", Rarity.Rare);
            var without = AddItem("GEAR_B");
            _mod.EnableIntegration<ItemRaritiesIntegration>();

            var issue = new ItemRaritiesRule().Check(new ValidationContext(_mod, null)).Single();
            Assert.That(issue.Severity, Is.EqualTo(Severity.Info));
            Assert.That(issue.Target, Is.SameAs(without));
        }

        [Test]
        public void Rule_IsQuietWhenTheIntegrationIsOff()
        {
            AddItem("GEAR_A");
            Assert.That(new ItemRaritiesRule().Check(new ValidationContext(_mod, null)), Is.Empty);
        }

        [Test]
        public void ImportStep_BringsAcross1xRarities()
        {
            var prefab = new GameObject("GEAR_OldStew");
            var other = new GameObject("GEAR_NotInTheMod");
            var item = AddItem("GEAR_OldStew");
            item.Prefab = prefab;
            var rarities = ScriptableObject.CreateInstance<V1Rarities>();
            rarities.Rare = new[] { prefab };
            rarities.Epic = new[] { other };
            var definition = V1Definition.CreateModDefinition("Old Kitchen", "Someone");
            definition.modItemRarities = rarities;
            var report = new ImportReport();
            try
            {
                new ItemRaritiesImportStep().Import(definition, _mod, report);

                Assert.That(_mod.HasIntegration<ItemRaritiesIntegration>(), Is.True);
                Assert.That(item.GetIntegrationData<ItemRarityData>().Rarity, Is.EqualTo(Rarity.Rare));
                Assert.That(report.Notes.Single(), Does.Contain("GEAR_NotInTheMod"));
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(other);
                Object.DestroyImmediate(rarities);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Build_WritesRaritiesIntoTheArchive()
        {
            AssetDatabase.CreateFolder("Assets", "ItemRaritiesTests");
            var root = new GameObject("GEAR_TestCan");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/GEAR_TestCan.prefab");
            Object.DestroyImmediate(root);

            var item = AddItem("GEAR_TestCan", Rarity.Legendary);
            item.Prefab = prefab;
            item.AddModule(new Modules.FoodComponent { Calories = 300, NormalModel = "Mesh" });
            AssetDatabase.CreateAsset(item, $"{Folder}/GEAR_TestCan.asset");
            _mod.DisplayName = "Rarity Test";
            _mod.Id = "RarityTest";
            _mod.EnableIntegration<ItemRaritiesIntegration>();
            AssetDatabase.CreateAsset(_mod, $"{Folder}/RarityTest.asset");
            AssetDatabase.SaveAssets();
            var output = Path.Combine(Path.GetTempPath(), "mcsdk-rarities-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var report = ModBuildPipeline.Build(_mod, new ModBuildOptions { OutputDirectory = output, SkipBundles = true });

                Assert.That(report.Succeeded, Is.True, string.Join("\n", report.Messages));
                Assert.That(report.Entries, Does.Contain("rarities.ir"));
                using var stream = File.OpenRead(report.ArchivePath);
                using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
                using var reader = new StreamReader(zip.GetEntry("rarities.ir")!.Open());
                Assert.That(reader.ReadToEnd(), Is.EqualTo("[Legendary]\nGEAR_TestCan\n\n"));
            }
            finally
            {
                if (Directory.Exists(output)) Directory.Delete(output, true);
            }
        }

        [Test]
        public void MovedTypes_LoadFromModsSavedWithTheSdksOwn()
        {
            // A mod saved before 2.0 names the types in the SDK's own namespace and assembly.
            AssetDatabase.CreateFolder("Assets", "ItemRaritiesTests");
            var path = Folder + "/OldMod.asset";
            var mod = ScriptableObject.CreateInstance<Mod>();
            mod.EnableIntegration<ItemRaritiesIntegration>();
            AssetDatabase.CreateAsset(mod, path);
            AssetDatabase.SaveAssets();

            var file = Path.GetFullPath(path);
            var yaml = File.ReadAllText(file);
            const string Current = "ns: ModComponent.SDK.ItemRarities, asm: ModComponent.SDK.ItemRarities}";
            Assert.That(yaml, Does.Contain(Current));
            File.WriteAllText(file, yaml.Replace(Current, "ns: ModComponent.SDK.Integrations, asm: ModComponent.SDK.Runtime}"));
            Resources.UnloadAsset(mod);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var loaded = AssetDatabase.LoadAssetAtPath<Mod>(path);
            Assert.That(loaded.HasIntegration<ItemRaritiesIntegration>(), Is.True);
            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(loaded), Is.False);
        }
    }
}
