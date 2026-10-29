using System.Linq;
using ModComponent.Behaviours;
using ModComponent.Blueprints;
using ModComponent.Components;
using ModComponent.SDK.Components;
using ModComponent.SDK.Editor.Migration;
using ModComponent.SDK.Editor.Updates;
using ModComponent.SDK.Modules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    public class SemVersionTests
    {
        [TestCase("2.0.0", "1.0.6", 1)]
        [TestCase("2.0.0-nightly.10", "2.0.0-nightly.9", 1)]
        [TestCase("2.0.0", "2.0.0-nightly.12", 1)]
        [TestCase("v2.0.1", "2.0.1", 0)]
        [TestCase("2.0.0-nightly.1", "2.0.0-preview.1", -1)]
        [TestCase("1.10.0", "1.9.9", 1)]
        public void Compares(string a, string b, int expected)
        {
            Assert.That(SemVersion.TryParse(a, out var va), Is.True);
            Assert.That(SemVersion.TryParse(b, out var vb), Is.True);
            Assert.That(System.Math.Sign(va.CompareTo(vb)), Is.EqualTo(expected));
        }

        [Test]
        public void PickUpdate_TakesNewestEligibleRelease()
        {
            const string json = "[" +
                                "{\"tag_name\":\"v2.0.0-nightly.3\",\"body\":\"n3\",\"html_url\":\"u3\"}," +
                                "{\"tag_name\":\"v2.1.0\",\"draft\":true}," +
                                "{\"tag_name\":\"v2.0.0\",\"body\":\"final\",\"html_url\":\"u\"}," +
                                "{\"tag_name\":\"v1.0.6\"},{\"tag_name\":\"not-a-version\"}]";
            SemVersion.TryParse("2.0.0-nightly.2", out var current);

            Assert.That(UpdateService.PickUpdate(json, current, includePrereleases: true).Tag, Is.EqualTo("v2.0.0"));
            SemVersion.TryParse("2.0.0", out var released);
            Assert.That(UpdateService.PickUpdate(json, released, includePrereleases: true), Is.Null);
            SemVersion.TryParse("1.0.6", out var old);
            Assert.That(UpdateService.PickUpdate("[{\"tag_name\":\"v2.0.0-nightly.3\"}]", old, includePrereleases: false), Is.Null);
        }

        [Test]
        public void PickUpdate_ReadsTheNightlysVersionFromItsTitle()
        {
            const string json = "[{\"tag_name\":\"nightly\",\"name\":\"2.0.1-nightly.20261029.42\",\"prerelease\":true}," +
                                "{\"tag_name\":\"2.0.0\",\"name\":\"2.0.0\"}]";
            SemVersion.TryParse("2.0.0", out var current);

            var update = UpdateService.PickUpdate(json, current, includePrereleases: true);
            Assert.That(update.Tag, Is.EqualTo("nightly"));
            Assert.That(update.Version.ToString(), Is.EqualTo("2.0.1-nightly.20261029.42"));
            Assert.That(UpdateService.PickUpdate(json, current, includePrereleases: false), Is.Null);
        }
    }

    public class MigrationTests
    {
        private const string Folder = "Assets/ModComponentSDK_MigrationTemp";
        private ModDefinition _definition;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "ModComponentSDK_MigrationTemp");

            DataGearAsset Gear(string name) => Save(ScriptableObject.CreateInstance<DataGearAsset>(), name);
            DataSoundAsset Sound(string name) => Save(ScriptableObject.CreateInstance<DataSoundAsset>(), name);

            var root = new GameObject("GEAR_OldStew");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform);
            var food = root.AddComponent<ModFoodComponent>();
            food.DisplayNameLocalizationId = "GAMEPLAY_OldStew";
            food.DescriptionLocalizatonId = "GAMEPLAY_OldStewDesc";
            food.WeightKG = 0.5f;
            food.Calories = 600;
            food.NormalModel = mesh;
            food.PickUpAudio = Sound("Play_SndInvCan");
            food.InitialCondition = ModComponent.Utilities.InitialCondition.Perfect;
            food.Servings = 3;
            var harvest = root.AddComponent<ModHarvestableBehaviour>();
            harvest.Minutes = 15;
            harvest.Yields = new[] { new Yields { YieldNames = Gear("GEAR_ScrapMetal"), YieldCounts = 2 } };
            harvest.RequiredToolNames = new[] { Gear("GEAR_Hacksaw") };
            var blueprint = root.AddComponent<ModBlueprint>();
            blueprint.RequiredGear = new[] { new GearRequirement { GearItem = Gear("GEAR_CannedBeans"), Count = 2 } };
            blueprint.CraftedResult = Gear("GEAR_OldStew");
            blueprint.CraftedResultCount = 1;
            blueprint.DurationMinutes = 30;
            _prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/GEAR_OldStew.prefab");
            Object.DestroyImmediate(root);

            var localization = ScriptableObject.CreateInstance<ModLocalization>();
            localization.localizationEntries.Add(new ModLocalization.LocalizationEntry
            {
                localizationKey = "GAMEPLAY_OldStew",
                languages = new ModLocalization.Languages { English = "Old Stew", French = "Vieux ragoût" }
            });
            Save(localization, "OldLocalization");

            var spawns = ScriptableObject.CreateInstance<ModGearSpawns>();
            spawns.sceneGearSpawnEntries.Add(new ModGearSpawns.SceneSpawnEntry
            {
                sceneName = Save(ScriptableObject.CreateInstance<DataSceneAsset>(), "CoastalRegion"),
                sceneItemSpawns = { new ModGearSpawns.SceneItemSpawn { itemName = Gear("GEAR_OldStew"), position = new Vector3(1, 2, 3), spawnChance = 40 } }
            });
            Save(spawns, "OldSpawns");

            var rarities = ScriptableObject.CreateInstance<ModItemRarities>();
            rarities.Rare = new[] { _prefab };
            Save(rarities, "OldRarities");

            _definition = ModDefinition.CreateModDefinition("Old Kitchen", "Someone");
            _definition.Version = "1.4.0";
            _definition.Items = new[] { _prefab };
            _definition.modLocalization = localization;
            _definition.modGearSpawns = spawns;
            _definition.modItemRarities = rarities;
            Save(_definition, "OldKitchen");
            // Changes made after CreateAsset are only written (and kept across reloads) once marked dirty.
            foreach (var asset in new Object[] { localization, spawns, rarities, _definition }) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.DeleteAsset("Assets/Mods/OldKitchen");
        }

        private static T Save<T>(T asset, string name) where T : Object
        {
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{name}.asset"));
            asset.name = name;
            return asset;
        }

        [Test]
        public void DryRun_ReportsWithoutWritingAnything()
        {
            var report = V1Importer.Import(_definition, dryRun: true);

            Assert.That(report.Items.Count, Is.EqualTo(1));
            Assert.That(report.Blueprints.Count, Is.EqualTo(1));
            Assert.That(report.LocalizationKeys, Is.EqualTo(1));
            Assert.That(report.Spawns, Is.EqualTo(1));
            Assert.That(report.Notes, Has.Some.Contains("Servings"));
            Assert.That(AssetDatabase.IsValidFolder("Assets/Mods/OldKitchen"), Is.False);
        }

        [Test]
        public void Import_ConvertsItemsAndTables()
        {
            var report = V1Importer.Import(_definition, dryRun: false);
            var mod = report.Mod;

            Assert.That(AssetDatabase.GetAssetPath(mod), Is.EqualTo("Assets/Mods/OldKitchen/OldKitchen.asset"));
            Assert.That(mod.DisplayName, Is.EqualTo("Old Kitchen"));
            Assert.That(mod.Version, Is.EqualTo("1.4.0"));

            var item = mod.Items.Single();
            Assert.That(item.Prefab, Is.EqualTo(_prefab));
            var food = (FoodComponent)item.Component;
            Assert.That(food.Calories, Is.EqualTo(600));
            Assert.That(food.WeightKG, Is.EqualTo(0.5f));
            Assert.That(food.DescriptionLocalizationId.Id, Is.EqualTo("GAMEPLAY_OldStewDesc"));
            Assert.That(food.NormalModel.Id, Is.EqualTo("Mesh"));
            Assert.That(food.PickUpAudio.Id, Is.EqualTo("Play_SndInvCan"));
            Assert.That(food.InitialCondition, Is.EqualTo(InitialCondition.Perfect));

            var harvest = item.GetModule<HarvestableBehaviour>();
            Assert.That(harvest.Minutes, Is.EqualTo(15));
            Assert.That(harvest.Yields.Single().Item.Id, Is.EqualTo("GEAR_ScrapMetal"));
            Assert.That(harvest.Yields.Single().Count, Is.EqualTo(2));
            Assert.That(harvest.RequiredToolNames.Single().Id, Is.EqualTo("GEAR_Hacksaw"));

            var bp = mod.Blueprints.Single();
            Assert.That(bp.CraftedResult.Id, Is.EqualTo("GEAR_OldStew"));
            Assert.That(bp.RequiredGear.Single().Count, Is.EqualTo(2));

            Assert.That(mod.Localization.Get("GAMEPLAY_OldStew", "French (France)"), Is.EqualTo("Vieux ragoût"));
            Assert.That(mod.Spawns.Scenes.Single().Spawns.Single().Chance, Is.EqualTo(40f));
            Assert.That(V1Importer.IsImported(_definition), Is.True);
            Assert.That(_prefab.GetComponent<ModFoodComponent>(), Is.Not.Null, "1.x data is left alone");
        }

        [Test]
        public void Import_Twice_DoesNotDuplicate()
        {
            V1Importer.Import(_definition, dryRun: false);
            var second = V1Importer.Import(_definition, dryRun: false);

            Assert.That(second.Notes, Has.Some.Contains("already exists"));
            Assert.That(Validation.ModValidator.FindAllMods().Count(m => m.Id == "OldKitchen"), Is.EqualTo(1));
        }

        [Test]
        public void LegacyScripts_AreFlagged_AndRemovable()
        {
            var mod = V1Importer.Import(_definition, dryRun: false).Mod;
            var issue = Validation.ModValidator.Validate(mod).Errors.Single(e => e.RuleId == "item.legacy-components");

            issue.Fix();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/GEAR_OldStew.prefab");
            Assert.That(V1Importer.HasLegacyComponents(prefab), Is.False);
            Assert.That(prefab.transform.Find("Mesh"), Is.Not.Null, "the model is kept");
        }
    }
}
