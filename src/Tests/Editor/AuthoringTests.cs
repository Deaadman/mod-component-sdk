using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.UI.Inspectors;
using ModComponent.SDK.Editor.UI.References;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    public class AuthoringTests
    {
        private const string Folder = "Assets/ModComponentSDK_AuthoringTemp";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Folder);

        private Mod CreateMod() => AssetFactory.CreateMod(new AssetFactory.ModSettings
        {
            DisplayName = "Authoring Test", Author = "Tests", Folder = Folder
        });

        [Test]
        public void CreateMod_MakesTablesAndFolders()
        {
            var mod = CreateMod();

            Assert.That(mod.Id, Is.EqualTo("AuthoringTest"));
            Assert.That(mod.Localization, Is.Not.Null);
            Assert.That(mod.Spawns, Is.Not.Null);
            Assert.That(AssetDatabase.IsValidFolder($"{Folder}/Items"), Is.True);
            Assert.That(AssetDatabase.IsValidFolder($"{Folder}/Blueprints"), Is.True);
        }

        [Test]
        public void CreateItem_FromModel_PassesItemValidation()
        {
            var mod = CreateMod();
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Thermos";
            var modelPrefab = PrefabUtility.SaveAsPrefabAsset(model, $"{Folder}/ThermosModel.prefab");
            Object.DestroyImmediate(model);

            var item = AssetFactory.CreateItem(new AssetFactory.ItemSettings
            {
                Mod = mod, GearName = "Thermos Flask", DisplayName = "Thermos Flask",
                Model = modelPrefab, Template = ItemTemplates.All.First(t => t.Name == "Liquid container")
            });

            Assert.That(item.GearName, Is.EqualTo("GEAR_ThermosFlask"));
            Assert.That(item.Prefab.name, Is.EqualTo("GEAR_ThermosFlask"));
            Assert.That(item.Prefab.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(item.Prefab.transform.GetChild(0).name, Is.EqualTo("ThermosModel"));
            Assert.That(item.Component, Is.TypeOf<LiquidComponent>());
            Assert.That(item.Component.NormalModel.Id, Is.EqualTo("ThermosModel"));
            Assert.That(mod.Localization.Get("GAMEPLAY_ThermosFlask"), Is.EqualTo("Thermos Flask"));
            Assert.That(mod.Items, Does.Contain(item));

            var itemErrors = ModValidator.Validate(mod).Errors.Where(e => e.Target == item).ToList();
            Assert.That(itemErrors, Is.Empty, string.Join("\n", itemErrors));
        }

        [Test]
        public void EveryTemplate_ProducesAnItemWithoutModuleErrors()
        {
            var mod = CreateMod();
            foreach (var template in ItemTemplates.All)
            {
                var item = AssetFactory.CreateItem(new AssetFactory.ItemSettings
                {
                    Mod = mod, GearName = "GEAR_Template" + template.Name.Replace(" ", string.Empty), Template = template
                });
                Assert.That(item.Component, Is.Not.Null, template.Name);
            }

            var errors = ModValidator.Validate(mod).Errors.Where(e => e.RuleId is "item.module-values" or "item.component").ToList();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void CreateItem_RefusesWhenThePrefabAlreadyExists()
        {
            var mod = CreateMod();
            var orphan = new GameObject("GEAR_Orphan");
            PrefabUtility.SaveAsPrefabAsset(orphan, $"{Folder}/Items/GEAR_Orphan.prefab");
            Object.DestroyImmediate(orphan);

            Assert.Throws<System.ArgumentException>(() =>
                AssetFactory.CreateItem(new AssetFactory.ItemSettings { Mod = mod, GearName = "GEAR_Orphan" }));
            Assert.That(mod.Items, Is.Empty);
        }

        [TestCase("wool hat", "GEAR_Woolhat")]
        [TestCase("GEAR_WoolHat", "GEAR_WoolHat")]
        [TestCase("  Pine-Tea!  ", "GEAR_PineTea")]
        public void NormalizeGearName(string input, string expected) =>
            Assert.That(AssetFactory.NormalizeGearName(input), Is.EqualTo(expected));

        [Test]
        public void Inspectors_BuildWithoutErrors()
        {
            var mod = CreateMod();
            var item = AssetFactory.CreateItem(new AssetFactory.ItemSettings { Mod = mod, GearName = "GEAR_InspectMe", Template = ItemTemplates.All[1] });
            item.AddModule(new RepairableBehaviour());

            foreach (var asset in new Object[] { item, mod, AssetFactory.CreateBlueprint(mod, "BP_Test", item) })
            {
                var editor = UnityEditor.Editor.CreateEditor(asset);
                try
                {
                    Assert.That(editor.CreateInspectorGUI(), Is.Not.Null, asset.GetType().Name);
                }
                finally
                {
                    Object.DestroyImmediate(editor);
                }
            }
        }

        [Test]
        public void ModuleCard_PutsOwnFieldsOnTheFirstTab()
        {
            var groups = ModuleCard.GroupFields(typeof(FoodComponent), "Food");

            Assert.That(groups[0].tab, Is.EqualTo("Food"));
            Assert.That(groups.Select(g => g.tab), Is.SupersetOf(new[] { "General", "Audio", "Inspect", "Cooking", "Effects" }));
            Assert.That(groups.SelectMany(g => g.fields).Count(), Is.EqualTo(Build.Export.ModuleSerializer.GetExportedFields(typeof(FoodComponent)).Length));
        }

        [Test]
        public void ReferenceSources_ListProjectAndVanillaGear()
        {
            var mod = CreateMod();
            AssetFactory.CreateItem(new AssetFactory.ItemSettings { Mod = mod, GearName = "GEAR_Listed" });

            var options = ReferenceSources.For(typeof(GearRef), null).ToList();

            Assert.That(options.Select(o => o.Id), Does.Contain("GEAR_Listed").And.Contain("GEAR_Hatchet"));
            Assert.That(options.First(o => o.Id == "GEAR_Listed").Group, Is.EqualTo("Project/Authoring Test"));
            Assert.That(ReferenceSources.IsKnown(typeof(GearRef), "GEAR_Listed", null), Is.True);
            Assert.That(ReferenceSources.IsKnown(typeof(SoundRef), "Play_NotASound", null), Is.False);
            Assert.That(ReferenceSources.IsKnown(typeof(SoundRef), "", null), Is.Null);
        }
    }
}
