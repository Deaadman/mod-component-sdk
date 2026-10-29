using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ModComponent.SDK.ItemRarities.Editor.Tests
{
    /// <summary>A window to host inspectors in, so UI Toolkit dispatches their events and runs their bindings.</summary>
    public sealed class ItemRaritiesUITestHost : EditorWindow
    {
    }

    /// <summary>Drives the SDK's real inspectors with Item Rarities installed, as a modder would use them.</summary>
    public class ItemRaritiesUITests
    {
        private const string Folder = "Assets/ItemRaritiesUITests";

        private ItemRaritiesUITestHost _host;
        private UnityEditor.Editor _editor;
        private Mod _mod;
        private GearItem _item;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Without a graphics device editor windows don't open, and UI Toolkit dispatches no events.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device in this test run, so editor windows can't open.");
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "ItemRaritiesUITests");
            _item = ScriptableObject.CreateInstance<GearItem>();
            _item.GearName = "GEAR_RarityUITest";
            AssetDatabase.CreateAsset(_item, $"{Folder}/GEAR_RarityUITest.asset");
            _mod = ScriptableObject.CreateInstance<Mod>();
            _mod.DisplayName = "Rarity UI Test";
            _mod.Id = "RarityUITest";
            _mod.Items.Add(_item);
            AssetDatabase.CreateAsset(_mod, $"{Folder}/RarityUITest.asset");
            AssetDatabase.SaveAssets();
            _host = ScriptableObject.CreateInstance<ItemRaritiesUITestHost>();
            _host.Show();
            yield return null; // lets the project change reach the SDK's list of mods
        }

        [TearDown]
        public void TearDown()
        {
            if (_editor != null) Object.DestroyImmediate(_editor);
            if (_host != null) _host.Close();
            AssetDatabase.DeleteAsset(Folder);
        }

        private VisualElement Inspect(Object target)
        {
            if (_editor != null) Object.DestroyImmediate(_editor);
            _editor = UnityEditor.Editor.CreateEditor(target);
            var ui = _editor.CreateInspectorGUI();
            _host.rootVisualElement.Clear();
            _host.rootVisualElement.Add(ui);
            return ui;
        }

        private static Toggle FindToggle(VisualElement root, string label) =>
            root.Query<Toggle>().Where(t => t.label == label).ToList().FirstOrDefault();

        private static void Click(Button button)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        [UnityTest]
        public IEnumerator TurningItOnAndPickingARarity()
        {
            var modUI = Inspect(_mod);
            var toggle = FindToggle(modUI, "Item Rarities");
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.tooltip, Is.EqualTo("Write rarities.ir for the Item Rarities mod."));
            toggle.value = true;
            yield return null;
            Assert.That(_mod.HasIntegration<ItemRaritiesIntegration>(), Is.True);

            var itemUI = Inspect(_item);
            var section = itemUI.Query<Label>().Where(l => l.text == "Item Rarities").ToList();
            Assert.That(section, Has.Count.EqualTo(1), "the item has an Item Rarities section");
            var hasRarity = FindToggle(itemUI, "Has a rarity");
            Assert.That(hasRarity.value, Is.False);
            Assert.That(RarityField(itemUI), Is.Null);

            hasRarity.value = true;
            yield return null;
            yield return null;

            var rarity = RarityField(itemUI);
            Assert.That(rarity, Is.Not.Null, "a Rarity field appears once the item has a rarity");
            // PropertyField draws an enum inside a [SerializeReference] object as an EnumField, or as a popup of
            // its names when it can't resolve the enum type. Either way it's bound to the item's data.
            switch (rarity)
            {
                case EnumField field:
                    Assert.That(field.value, Is.EqualTo(Rarity.Common));
                    field.value = Rarity.Epic;
                    break;
                case PopupField<string> popup:
                    Assert.That(popup.value, Is.EqualTo("Common"));
                    popup.value = "Epic";
                    break;
                default:
                    Assert.Fail($"Unexpected Rarity field: {rarity.GetType().Name}");
                    break;
            }

            yield return null;
            Assert.That(_item.GetIntegrationData<ItemRarityData>().Rarity, Is.EqualTo(Rarity.Epic));

            FindToggle(itemUI, "Has a rarity").value = false;
            yield return null;
            Assert.That(_item.GetIntegrationData<ItemRarityData>(), Is.Null);
            Assert.That(RarityField(itemUI), Is.Null);
        }

        /// <summary>The field the item inspector draws for ItemRarityData.Rarity, or null when there isn't one.</summary>
        private static VisualElement RarityField(VisualElement root)
        {
            var property = root.Query<PropertyField>().Where(f => f.bindingPath != null && f.bindingPath.EndsWith(".Rarity")).ToList().SingleOrDefault();
            return property?.Query<VisualElement>().Where(e => e is EnumField || e is PopupField<string>).ToList().FirstOrDefault();
        }

        [UnityTest]
        public IEnumerator IssuesListOffersToMakeEveryItemCommon()
        {
            _mod.EnableIntegration<ItemRaritiesIntegration>();
            EditorUtility.SetDirty(_mod);

            var modUI = Inspect(_mod);
            yield return null;
            var fix = modUI.Query<Button>().Where(b => b.text == "Make them all Common").ToList();
            Assert.That(fix, Has.Count.EqualTo(1), "the warning shows in the Mod's issues with its fix");

            Click(fix.Single());
            yield return null;

            Assert.That(_item.GetIntegrationData<ItemRarityData>().Rarity, Is.EqualTo(Rarity.Common));
            Assert.That(modUI.Query<Button>().Where(b => b.text == "Make them all Common").ToList(), Is.Empty,
                "the issues list refreshes after the fix");
        }
    }
}
