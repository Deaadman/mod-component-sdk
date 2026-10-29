using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Integrations;
using ModComponent.SDK.Editor.Updates;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Editor.Validation.Rules;
using ModComponent.SDK.Integrations;
using NUnit.Framework;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    // An integration as an add-on would define one. The registry finds these too, so they also show up in the
    // editor while the package's tests are compiled.

    [Serializable]
    public sealed class TestIntegration : ModIntegration
    {
        public string Setting = "value";

        public override string DisplayName => "Test Integration";
        public override string Description => "Only exists in the SDK's tests.";
    }

    [Serializable]
    [ItemIntegrationData(typeof(TestIntegration), Label = "Has test data")]
    public sealed class TestItemData : ItemIntegrationData
    {
        public int Value;
    }

    public sealed class TestIntegrationExporter : IntegrationExporter<TestIntegration>
    {
        private static readonly string[] Extensions = { ".test" };

        public override IReadOnlyCollection<string> RootExtensions => Extensions;

        protected override IEnumerable<KeyValuePair<string, string>> Export(Mod mod, TestIntegration integration)
        {
            var values = mod.Items.Where(i => i != null).Select(i => i.GetIntegrationData<TestItemData>()?.Value ?? 0);
            yield return new KeyValuePair<string, string>("data.test", integration.Setting + ":" + string.Join(",", values));
        }
    }

    public class IntegrationTests
    {
        private Mod _mod;

        [SetUp]
        public void SetUp() => _mod = ScriptableObject.CreateInstance<Mod>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _mod.Items.Where(i => i != null)) UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.Object.DestroyImmediate(_mod);
        }

        [Test]
        public void Registry_FindsIntegrationsFromAnyAssembly()
        {
            var info = IntegrationRegistry.Get(typeof(TestIntegration));

            Assert.That(info.DisplayName, Is.EqualTo("Test Integration"));
            Assert.That(info.Description, Is.EqualTo("Only exists in the SDK's tests."));
            Assert.That(info.Exporter, Is.InstanceOf<TestIntegrationExporter>());
            Assert.That(info.ItemData.Single().Type, Is.EqualTo(typeof(TestItemData)));
            Assert.That(info.ItemData.Single().Label, Is.EqualTo("Has test data"));
        }

        [Test]
        public void Registry_RootExtensionsComeFromEnabledIntegrationsOnly()
        {
            Assert.That(IntegrationRegistry.RootExtensions(_mod), Is.Empty);
            _mod.EnableIntegration(typeof(TestIntegration));
            Assert.That(IntegrationRegistry.RootExtensions(_mod), Is.EqualTo(new[] { ".test" }));
        }

        [Test]
        public void Mod_EnablesIntegrationsByType()
        {
            var a = _mod.EnableIntegration(typeof(TestIntegration));
            var b = _mod.EnableIntegration(typeof(TestIntegration));

            Assert.That(a, Is.SameAs(b));
            Assert.That(_mod.GetIntegration(typeof(TestIntegration)), Is.SameAs(a));
            Assert.That(_mod.DisableIntegration(typeof(TestIntegration)), Is.True);
            Assert.That(_mod.HasIntegration(typeof(TestIntegration)), Is.False);
            Assert.Throws<ArgumentException>(() => _mod.EnableIntegration(typeof(ModIntegration)));
            Assert.Throws<ArgumentException>(() => _mod.EnableIntegration(typeof(string)));
        }

        [Test]
        public void GearItem_AddsIntegrationDataByType()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            _mod.Items.Add(item);

            var data = item.GetOrAddIntegrationData(typeof(TestItemData));

            Assert.That(item.GetOrAddIntegrationData<TestItemData>(), Is.SameAs(data));
            Assert.That(item.GetIntegrationData(typeof(TestItemData)), Is.SameAs(data));
            Assert.Throws<ArgumentException>(() => item.GetOrAddIntegrationData(typeof(ItemIntegrationData)));
        }

        [Test]
        public void Exporter_WritesTheIntegrationsFiles()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GetOrAddIntegrationData<TestItemData>().Value = 3;
            _mod.Items.Add(item);
            var integration = _mod.EnableIntegration<TestIntegration>();

            var files = IntegrationRegistry.Get(integration).Exporter.Export(_mod, integration).ToList();

            Assert.That(files.Single().Key, Is.EqualTo("data.test"));
            Assert.That(files.Single().Value, Is.EqualTo("value:3"));
        }

        [Test]
        public void MissingAddOnRule_IsQuietWhenEveryTypeLoads()
        {
            _mod.EnableIntegration<TestIntegration>();
            Assert.That(new MissingAddOnRule().Check(new ValidationContext(_mod, null)), Is.Empty);
        }

        [Test]
        public void Update_AlsoInstallsAddOnsFromThisRepository()
        {
            var installed = new[]
            {
                "com.deadman.modcomponent.sdk@https://github.com/Deaadman/mod-component-sdk.git?path=src#2.0.0",
                "com.deadman.modcomponent.sdk.item-rarities@https://github.com/Deaadman/mod-component-sdk.git?path=/addons/item-rarities#2.0.0",
                "com.someone.else@https://github.com/someone/else.git?path=addons/other#1.0.0",
                "com.unity.addressables@2.7.6"
            };

            Assert.That(UpdateService.InstallUrls("2.0.1", installed), Is.EqualTo(new[]
            {
                "https://github.com/Deaadman/mod-component-sdk.git?path=src#2.0.1",
                "https://github.com/Deaadman/mod-component-sdk.git?path=addons/item-rarities#2.0.1"
            }));
        }
    }
}
