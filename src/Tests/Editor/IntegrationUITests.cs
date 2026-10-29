using System.Collections;
using System.Linq;
using ModComponent.SDK.Editor.Integrations;
using ModComponent.SDK.Editor.UI.Windows;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.Tests
{
    /// <summary>A window to host inspectors in, so UI Toolkit dispatches their events and runs their bindings.</summary>
    public sealed class UITestHost : EditorWindow
    {
    }

    /// <summary>
    /// Drives the real integration UI: the Mod inspector's toggles, the item inspector's data sections and New Mod,
    /// using the tests' own integration.
    /// </summary>
    public class IntegrationUITests
    {
        private const string Folder = "Assets/ModComponentSDK_UITemp";

        private UITestHost _host;
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
            AssetDatabase.CreateFolder("Assets", "ModComponentSDK_UITemp");
            _item = ScriptableObject.CreateInstance<GearItem>();
            _item.GearName = "GEAR_UITest";
            AssetDatabase.CreateAsset(_item, $"{Folder}/GEAR_UITest.asset");
            _mod = ScriptableObject.CreateInstance<Mod>();
            _mod.DisplayName = "UI Test";
            _mod.Id = "UITest";
            _mod.Items.Add(_item);
            AssetDatabase.CreateAsset(_mod, $"{Folder}/UITest.asset");
            AssetDatabase.SaveAssets();
            _host = ScriptableObject.CreateInstance<UITestHost>();
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

        private static bool HasField(VisualElement root, string fieldName) =>
            root.Query<PropertyField>().Where(f => f.bindingPath != null && f.bindingPath.EndsWith("." + fieldName)).ToList().Count > 0;

        [UnityTest]
        public IEnumerator ModInspector_TogglesAnIntegrationAndShowsItsFields()
        {
            var ui = Inspect(_mod);
            var toggle = FindToggle(ui, "Test Integration");
            Assert.That(toggle, Is.Not.Null, "one toggle per installed integration");
            Assert.That(toggle.value, Is.False);
            Assert.That(toggle.tooltip, Is.EqualTo("Only exists in the SDK's tests."));
            Assert.That(HasField(ui, "Setting"), Is.False);

            toggle.value = true;
            yield return null;
            yield return null;

            Assert.That(_mod.HasIntegration<TestIntegration>(), Is.True);
            Assert.That(HasField(ui, "Setting"), Is.True, "an enabled integration's fields are drawn");
            var setting = ui.Query<TextField>().Where(t => t.label == "Setting").ToList().Single();
            Assert.That(setting.value, Is.EqualTo("value"), "the field is bound to the integration");

            setting.value = "changed";
            yield return null;
            Assert.That(_mod.GetIntegration<TestIntegration>().Setting, Is.EqualTo("changed"), "edits reach the integration");

            FindToggle(ui, "Test Integration").value = false;
            yield return null;

            Assert.That(_mod.HasIntegration<TestIntegration>(), Is.False);
            Assert.That(HasField(ui, "Setting"), Is.False);
        }

        [UnityTest]
        public IEnumerator ItemInspector_ShowsDataOnlyForIntegrationsItsModEnables()
        {
            var ui = Inspect(_item);
            Assert.That(FindToggle(ui, "Has test data"), Is.Null, "the integration is off");

            _mod.EnableIntegration<TestIntegration>();
            EditorUtility.SetDirty(_mod);
            ui = Inspect(_item);
            var toggle = FindToggle(ui, "Has test data");
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.value, Is.False);
            Assert.That(HasField(ui, "Value"), Is.False);

            toggle.value = true;
            yield return null;
            yield return null;

            Assert.That(_item.GetIntegrationData<TestItemData>(), Is.Not.Null);
            var field = ui.Query<IntegerField>().Where(f => f.label == "Value").ToList().Single();
            field.value = 7;
            yield return null;
            Assert.That(_item.GetIntegrationData<TestItemData>().Value, Is.EqualTo(7), "edits reach the item's data");

            FindToggle(ui, "Has test data").value = false;
            yield return null;

            Assert.That(_item.GetIntegrationData<TestItemData>(), Is.Null);
            Assert.That(HasField(ui, "Value"), Is.False);
        }

        [UnityTest]
        public IEnumerator NewMod_ListsIntegrationsAndEnablesTheTickedOnes()
        {
            Mod created = null;
            void OnCreated(Mod mod) => created = mod;
            NewModWindow.Created += OnCreated;
            try
            {
                NewModWindow.Open();
                yield return null;
                var window = Resources.FindObjectsOfTypeAll<NewModWindow>().Single();
                var root = window.rootVisualElement;

                var toggles = root.Query<Toggle>().Where(t => t.label.EndsWith(" support")).ToList();
                Assert.That(toggles.Select(t => t.label), Is.EquivalentTo(IntegrationRegistry.All.Select(i => i.DisplayName + " support")));

                root.Query<TextField>().Where(t => t.label == "Name").ToList().Single().value = "UI Made Mod";
                root.Query<TextField>().Where(t => t.label == "Folder").ToList().Single().value = $"{Folder}/UIMadeMod";
                FindToggle(root, "Test Integration support").value = true;
                Click(root.Query<Button>().Where(b => b.text == "Create").ToList().Single());
                yield return null;

                Assert.That(created, Is.Not.Null, "Create made a mod");
                Assert.That(AssetDatabase.GetAssetPath(created), Is.EqualTo($"{Folder}/UIMadeMod/UIMadeMod.asset"));
                Assert.That(created.Integrations.Select(i => i.GetType()), Is.EqualTo(new[] { typeof(TestIntegration) }),
                    "only the ticked integration is on");
            }
            finally
            {
                NewModWindow.Created -= OnCreated;
                foreach (var window in Resources.FindObjectsOfTypeAll<NewModWindow>()) window.Close();
            }
        }

        /// <summary>Presses a button the way the keyboard does, which runs its click handler.</summary>
        internal static void Click(Button button)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }
    }
}
