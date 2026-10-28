using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.UI.Preview;
using ModComponent.SDK.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace ModComponent.SDK.Editor.Tests
{
    public class ExampleModTests
    {
        private const string Folder = "Assets/ModComponentSDK_ExampleTemp";
        private string _output;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            _output = Path.Combine(Path.GetTempPath(), "mcsdk-example-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            if (Directory.Exists(_output)) Directory.Delete(_output, true);
        }

        [Test]
        public void ExampleMod_HasNoErrors_AndBuilds()
        {
            var mod = ExampleMod.Create(Folder);

            var errors = ModValidator.Validate(mod).Errors.ToList();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(mod.Items.Count, Is.EqualTo(3));
            Assert.That(mod.Localization.Get("GAMEPLAY_ExampleWoolScarf", "German"), Is.EqualTo("Wollschal"));

            if (!ProjectSetup.IsJsonCatalogEnabled) Assert.Ignore("This project builds binary Addressables catalogs; run ProjectSetup.EnableJsonCatalog once.");
            var report = ModBuildPipeline.Build(mod, new ModBuildOptions { OutputDirectory = _output });
            Assert.That(report.Succeeded, Is.True, string.Join("\n", report.Messages));
            Assert.That(report.Entries, Is.SupersetOf(new[]
            {
                "auto-mapped/exampletincan.json", "auto-mapped/examplewhittlingknife.json", "auto-mapped/examplewoolscarf.json",
                "blueprints/bp_examplewhittlingknife.json", "localizations/examplemod.json", "gear-spawns/examplemod.txt"
            }));
        }

        [Test]
        public void IconRenderer_WritesAndAssignsIcon()
        {
            if (!ItemPreview.CanRender) Assert.Ignore("No graphics device in this test run.");
            var mod = ExampleMod.Create(Folder);
            var item = mod.Items[0];

            var icon = IconRenderer.RenderAndAssign(item, 64);

            Assert.That(icon, Is.Not.Null);
            Assert.That(item.Icon, Is.EqualTo(icon));
            Assert.That(icon.name, Is.EqualTo(item.InventoryIconAddress));
        }
    }
}
