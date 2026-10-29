using System.Collections;
using System.Linq;
using ModComponent.SDK.Editor.UI.Windows;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.Tests
{
    /// <summary>
    /// Opens the SDK's real windows and inspectors and checks what a modder would see: every section keeps its
    /// title after refreshing, and split windows show both panes.
    /// </summary>
    public class WindowUITests
    {
        private const string Folder = "Assets/ModComponentSDK_WindowTemp";

        private Mod _mod;
        private GearItem _item;
        private UITestHost _host;
        private UnityEditor.Editor _editor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Without a graphics device editor windows don't open, and UI Toolkit lays nothing out.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device in this test run, so editor windows can't open.");

            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.CreateFolder("Assets", "ModComponentSDK_WindowTemp");
            _item = ScriptableObject.CreateInstance<GearItem>();
            _item.GearName = "GEAR_WindowTest";
            AssetDatabase.CreateAsset(_item, $"{Folder}/GEAR_WindowTest.asset");
            var spawns = ScriptableObject.CreateInstance<SpawnTable>();
            spawns.Scenes.Add(new SpawnTable.SceneGroup { Scene = "CoastalRegion" });
            AssetDatabase.CreateAsset(spawns, $"{Folder}/Spawns.asset");
            _mod = ScriptableObject.CreateInstance<Mod>();
            _mod.DisplayName = "Window Test";
            _mod.Id = "WindowTest";
            _mod.Items.Add(_item);
            _mod.Spawns = spawns;
            AssetDatabase.CreateAsset(_mod, $"{Folder}/WindowTest.asset");
            AssetDatabase.SaveAssets();
            yield return null; // lets the project change reach the SDK's list of mods
        }

        [TearDown]
        public void TearDown()
        {
            if (_editor != null) Object.DestroyImmediate(_editor);
            if (_host != null) _host.Close();
            foreach (var type in new[] { typeof(ModHubWindow), typeof(BuildWindow), typeof(SpawnsWindow) })
            foreach (var window in Resources.FindObjectsOfTypeAll(type).Cast<EditorWindow>())
                window.Close();
            AssetDatabase.DeleteAsset(Folder);
        }

        private static string[] SectionTitles(VisualElement root) =>
            root.Query<Label>(className: "mcsdk-section__title").ToList().Select(l => l.text).ToArray();

        private IEnumerator Inspect(Object target, System.Action<VisualElement> check)
        {
            _host = ScriptableObject.CreateInstance<UITestHost>();
            _host.Show();
            _editor = UnityEditor.Editor.CreateEditor(target);
            var ui = _editor.CreateInspectorGUI();
            _host.rootVisualElement.Add(ui);
            yield return null;
            check(ui);
        }

        [UnityTest]
        public IEnumerator ModInspector_KeepsEverySectionTitle() => Inspect(_mod, ui =>
            Assert.That(SectionTitles(ui), Is.SupersetOf(new[] { "Mod", "Content", "Tables", "Integrations", "Build", "Issues" }),
                "refreshing Integrations and Issues must not remove their titles"));

        [UnityTest]
        public IEnumerator ItemInspector_KeepsItsIssuesTitle() => Inspect(_item, ui =>
            Assert.That(SectionTitles(ui), Does.Contain("Issues")));

        [UnityTest]
        public IEnumerator BuildWindow_KeepsItsStepsTitle_AndShowsBothPanes()
        {
            BuildWindow.Open(_mod);
            yield return WaitForLayout();
            var window = Resources.FindObjectsOfTypeAll<BuildWindow>().Single();

            Assert.That(SectionTitles(window.rootVisualElement), Is.SupersetOf(new[] { "Output", "The Long Dark", "Steps" }));
            AssertLeftPaneIsOpen(window, 300);
        }

        [UnityTest]
        public IEnumerator ModHub_ShowsTheModTree()
        {
            ModHubWindow.Open();
            yield return WaitForLayout();
            var window = Resources.FindObjectsOfTypeAll<ModHubWindow>().Single();

            var tree = window.rootVisualElement.Q<TreeView>();
            Assert.That(tree, Is.Not.Null);
            AssertLeftPaneIsOpen(window, 260);
        }

        [UnityTest]
        public IEnumerator SpawnsWindow_ShowsTheGroupList()
        {
            SpawnsWindow.Open(_mod);
            yield return WaitForLayout();
            var window = Resources.FindObjectsOfTypeAll<SpawnsWindow>().Single();

            AssertLeftPaneIsOpen(window, 200);
        }

        private static IEnumerator WaitForLayout()
        {
            for (var i = 0; i < 5; i++) yield return null;
        }

        /// <summary>The split's left pane is about its starting width, not collapsed.</summary>
        private static void AssertLeftPaneIsOpen(EditorWindow window, float expected)
        {
            var split = window.rootVisualElement.Q<TwoPaneSplitView>();
            Assert.That(split, Is.Not.Null);
            var pane = split.fixedPane;
            Assert.That(pane.layout.width, Is.GreaterThan(expected * 0.6f),
                $"{window.GetType().Name}'s left pane is {pane.layout.width:0} px wide in a {window.position.width:0} px window " +
                $"(flex-basis {pane.resolvedStyle.flexBasis}, which overrides its {pane.style.width} width unless auto)");
        }
    }
}
