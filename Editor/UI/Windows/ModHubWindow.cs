using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Validation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// The SDK's home window: every mod in the project with its items, blueprints and tables, issue counts on
    /// each row, and the selected asset's inspector beside it.
    /// </summary>
    public sealed class ModHubWindow : EditorWindow
    {
        private sealed class Node
        {
            public string Label;
            public Object Asset;
            public Mod Mod;
            public string Icon;
            public int Errors;
            public int Warnings;
        }

        /// <summary>Extra rows at the top of the window, such as the update banner. Added by other features.</summary>
        public static event System.Action<VisualElement> BuildingBanners;

        [SerializeField] private string _selectedGuid;

        private TreeView _tree;
        private VisualElement _inspector;
        private Label _footer;
        private VisualElement _banners;
        private ToolbarSearchField _search;
        private int _nextId;

        [MenuItem("ModComponent SDK/Mod Hub", false, 0)]
        public static void Open()
        {
            var window = GetWindow<ModHubWindow>("Mod Hub");
            window.minSize = new Vector2(720, 420);
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Refresh;
            NewModWindow.Created += SelectAsset;
            NewItemWindow.Created += SelectAsset;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Refresh;
            NewModWindow.Created -= SelectAsset;
            NewItemWindow.Created -= SelectAsset;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement.WithStyles();

            _banners = new VisualElement();
            root.Add(_banners);

            var toolbar = new Toolbar();
            var create = new ToolbarMenu { text = "New" };
            create.menu.AppendAction("Mod…", _ => NewModWindow.Open());
            create.menu.AppendAction("Item…", _ => NewItemWindow.Open(SelectedMod), _ => ModValidator.FindAllMods().Any()
                ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            create.menu.AppendAction("Blueprint", _ =>
            {
                var bp = AssetFactory.CreateBlueprint(SelectedMod, "BP_New");
                SelectAsset(bp);
            }, _ => SelectedMod != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbar.Add(create);
            var spacer = new VisualElement();
            spacer.AddToClassList("mcsdk-toolbar__spacer");
            toolbar.Add(spacer);
            _search = new ToolbarSearchField();
            _search.RegisterValueChangedCallback(_ => Refresh());
            toolbar.Add(_search);
            toolbar.Add(new ToolbarButton(Refresh) { text = "Validate", tooltip = "Check every mod again" });
            toolbar.Add(new ToolbarButton(BuildSelected) { text = "Build", tooltip = "Build the selected mod" });
            root.Add(toolbar);

            var split = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };
            _tree = new TreeView { fixedItemHeight = 20, selectionType = SelectionType.Single };
            _tree.makeItem = MakeRow;
            _tree.bindItem = BindRow;
            _tree.selectionChanged += selection => ShowAsset((selection.FirstOrDefault() as Node)?.Asset);
            _tree.itemsChosen += chosen =>
            {
                var asset = (chosen.FirstOrDefault() as Node)?.Asset;
                if (asset != null) EditorGUIUtility.PingObject(asset);
            };
            split.Add(_tree);

            var right = new ScrollView();
            _inspector = new VisualElement { style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4 } };
            right.Add(_inspector);
            split.Add(right);
            root.Add(split);

            _footer = new Label();
            _footer.AddToClassList("mcsdk-footer");
            root.Add(_footer);

            BuildingBanners?.Invoke(_banners);
            Refresh();
        }

        private Mod SelectedMod => (_tree?.selectedItem as Node)?.Mod ?? ModValidator.FindAllMods().FirstOrDefault();

        private void Refresh()
        {
            if (_tree == null) return;
            _nextId = 0;
            var filter = _search?.value?.Trim() ?? string.Empty;
            var roots = new List<TreeViewItemData<Node>>();

            foreach (var mod in ModValidator.FindAllMods().OrderBy(m => m.DisplayName))
            {
                var result = ModValidator.Validate(mod);
                (int, int) Counts(Object asset) => (result.For(asset).Count(i => i.Severity == Severity.Error),
                    result.For(asset).Count(i => i.Severity == Severity.Warning));

                var children = new List<TreeViewItemData<Node>>();
                var items = mod.Items.Where(i => i != null && Matches(i.GearName, filter)).Select(i => Leaf(i.GearName, i, mod, "Prefab Icon", Counts(i))).ToList();
                if (items.Count > 0) children.Add(Group($"Items ({items.Count})", mod, "Folder Icon", items));
                var blueprints = mod.Blueprints.Where(b => b != null && Matches(b.name, filter)).Select(b => Leaf(b.name, b, mod, "ScriptableObject Icon", Counts(b))).ToList();
                if (blueprints.Count > 0) children.Add(Group($"Blueprints ({blueprints.Count})", mod, "Folder Icon", blueprints));
                if (mod.Localization != null && string.IsNullOrEmpty(filter))
                    children.Add(Leaf($"Localization ({mod.Localization.Entries.Count} keys)", mod.Localization, mod, "TextAsset Icon", Counts(mod.Localization)));
                if (mod.Spawns != null && string.IsNullOrEmpty(filter))
                    children.Add(Leaf("Spawns", mod.Spawns, mod, "SceneAsset Icon", Counts(mod.Spawns)));

                if (children.Count == 0 && !Matches(mod.DisplayName, filter)) continue;
                var node = new Node
                {
                    Label = mod.DisplayName, Asset = mod, Mod = mod, Icon = "d_Package Manager",
                    Errors = result.Errors.Count(), Warnings = result.Warnings.Count()
                };
                roots.Add(new TreeViewItemData<Node>(_nextId++, node, children));
            }

            _tree.SetRootItems(roots);
            _tree.Rebuild();
            _tree.ExpandAll();

            if (roots.Count == 0)
                ShowWelcome();
            else if (!RestoreSelection())
                _tree.SetSelection(0);

            var last = BuildPreferences.LastReport;
            _footer.text = (last == null
                    ? "No builds this session"
                    : $"Last build: {last.Mod?.DisplayName} · {(last.Succeeded ? "succeeded" : "failed")} · {last.Duration.TotalSeconds:0.0} s")
                + $"   ·   SDK {SdkInfo.Version} · ModComponent {SdkInfo.TargetModComponentVersion} · TLD {SdkInfo.TargetGameVersion} · Unity {SdkInfo.RequiredUnityVersion}";
        }

        private TreeViewItemData<Node> Leaf(string label, Object asset, Mod mod, string icon, (int errors, int warnings) counts) =>
            new(_nextId++, new Node { Label = label, Asset = asset, Mod = mod, Icon = icon, Errors = counts.errors, Warnings = counts.warnings });

        private TreeViewItemData<Node> Group(string label, Mod mod, string icon, List<TreeViewItemData<Node>> children) =>
            new(_nextId++, new Node
            {
                Label = label, Mod = mod, Icon = icon,
                Errors = children.Sum(c => c.data.Errors), Warnings = children.Sum(c => c.data.Warnings)
            }, children);

        private static bool Matches(string text, string filter) =>
            string.IsNullOrEmpty(filter) || (text ?? string.Empty).IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("mcsdk-tree-row");
            var icon = new Image();
            icon.AddToClassList("mcsdk-tree-row__icon");
            row.Add(icon);
            row.Add(new Label());
            var badge = new Label();
            badge.AddToClassList("mcsdk-tree-row__badge");
            row.Add(badge);
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            var node = _tree.GetItemDataForIndex<Node>(index);
            row.Q<Image>().image = EditorGUIUtility.IconContent(node.Icon).image;
            row.Q<Label>().text = node.Label;
            var badge = row.Q<Label>(className: "mcsdk-tree-row__badge");
            badge.EnableInClassList("mcsdk-tree-row__badge--error", node.Errors > 0);
            badge.EnableInClassList("mcsdk-tree-row__badge--warning", node.Errors == 0 && node.Warnings > 0);
            badge.text = node.Errors > 0 ? node.Errors.ToString() : node.Warnings > 0 ? node.Warnings.ToString() : string.Empty;
            badge.tooltip = $"{node.Errors} errors, {node.Warnings} warnings";
            badge.style.display = node.Errors + node.Warnings > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowAsset(Object asset)
        {
            _inspector.Clear();
            if (asset == null) return;
            _selectedGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            _inspector.Add(new InspectorElement(asset));
        }

        private void ShowWelcome()
        {
            _inspector.Clear();
            var section = SdkUI.Section($"Welcome to {SdkInfo.DisplayName}", out var content);
            content.Add(new Label("There are no mods in this project yet. A mod is one .modcomponent file: items, blueprints, " +
                                  "localization and spawns, built together.") { style = { whiteSpace = WhiteSpace.Normal } });
            content.Add(SdkUI.Row(new Button(NewModWindow.Open) { text = "Create a mod…" },
                new Button(() => Application.OpenURL(SdkInfo.RepositoryUrl + "/wiki")) { text = "Documentation" }));
            _inspector.Add(section);
        }

        private bool RestoreSelection()
        {
            if (string.IsNullOrEmpty(_selectedGuid)) return false;
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(_selectedGuid));
            return asset != null && Select(asset);
        }

        private void SelectAsset(Object asset)
        {
            Refresh();
            Select(asset);
        }

        private bool Select(Object asset)
        {
            for (var i = 0; i < _nextId; i++)
            {
                var node = _tree.GetItemDataForId<Node>(i);
                if (node?.Asset != asset) continue;
                _tree.SetSelectionById(i);
                _tree.ScrollToItemById(i);
                return true;
            }

            return false;
        }

        private void BuildSelected()
        {
            var mod = SelectedMod;
            if (mod == null) return;
            BuildActions.Build(mod);
            Refresh();
        }
    }
}
