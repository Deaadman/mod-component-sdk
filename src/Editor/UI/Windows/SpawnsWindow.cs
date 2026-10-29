using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Build.Export;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// Scene spawns and loot-table entries: groups on the left, rows on the right, a coordinate paste box and a
    /// preview of the exact gear-spawns file the build writes.
    /// </summary>
    public sealed class SpawnsWindow : EditorWindow
    {
        [SerializeField] private Mod _mod;
        [SerializeField] private bool _lootTables;
        [SerializeField] private int _group;

        private SerializedObject _serialized;

        public static void Open(Mod mod)
        {
            var window = GetWindow<SpawnsWindow>();
            window._mod = mod;
            window._group = 0;
            window.titleContent = new GUIContent($"Spawns · {mod.DisplayName}");
            window.minSize = new Vector2(640, 320);
            window.Rebuild();
        }

        private SpawnTable Table => _mod != null ? _mod.Spawns : null;
        private string ListName => _lootTables ? "lootTables" : "scenes";

        private void CreateGUI() => Rebuild();

        private void OnEnable() => Undo.undoRedoPerformed += Rebuild;

        private void OnDisable() => Undo.undoRedoPerformed -= Rebuild;

        private void Rebuild()
        {
            var root = rootVisualElement.WithStyles();
            root.Clear();
            if (Table == null)
            {
                root.Add(new HelpBox("Open this window from a mod that has a spawn table.", HelpBoxMessageType.Info));
                return;
            }

            _serialized = new SerializedObject(Table);
            var groups = _serialized.FindProperty(ListName);
            _group = Mathf.Clamp(_group, 0, Mathf.Max(0, groups.arraySize - 1));

            var toolbar = new Toolbar();
            var scenes = new ToolbarToggle { text = "Scenes", value = !_lootTables };
            var loot = new ToolbarToggle { text = "Loot tables", value = _lootTables };
            scenes.RegisterValueChangedCallback(e => { if (e.newValue) { _lootTables = false; _group = 0; Rebuild(); } });
            loot.RegisterValueChangedCallback(e => { if (e.newValue) { _lootTables = true; _group = 0; Rebuild(); } });
            toolbar.Add(scenes);
            toolbar.Add(loot);
            toolbar.Add(new ToolbarButton(AddGroup) { text = _lootTables ? "＋ Loot table" : "＋ Scene" });
            var spacer = new VisualElement();
            spacer.AddToClassList("mcsdk-toolbar__spacer");
            toolbar.Add(spacer);
            toolbar.Add(new ToolbarButton(PreviewFile) { text = "Preview file" });
            root.Add(toolbar);

            var split = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };

            var groupList = new ListView
            {
                itemsSource = Enumerable.Range(0, groups.arraySize).ToList(),
                makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 6 } },
                bindItem = (e, i) => ((Label)e).text = GroupLabel(groups.GetArrayElementAtIndex(i), i),
                selectionType = SelectionType.Single,
                fixedItemHeight = 20
            };
            groupList.SetSelectionWithoutNotify(new[] { _group });
            groupList.selectionChanged += _ =>
            {
                _group = groupList.selectedIndex;
                Rebuild();
            };
            split.Add(groupList);

            var right = new ScrollView { style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4 } };
            if (groups.arraySize == 0)
            {
                var empty = new Label(_lootTables ? "No loot tables yet. Add one to put items into vanilla containers."
                    : "No scenes yet. Add one to place items at fixed spots.");
                empty.AddToClassList("mcsdk-empty");
                right.Add(empty);
            }
            else
            {
                var group = groups.GetArrayElementAtIndex(_group);
                right.Add(new PropertyField(group.FindPropertyRelative(_lootTables ? "LootTable" : "Scene")));
                if (!_lootTables)
                {
                    right.Add(new PropertyField(group.FindPropertyRelative("Tag")));
                    right.Add(PasteBox(group.FindPropertyRelative("Spawns")));
                }

                right.Add(new PropertyField(group.FindPropertyRelative(_lootTables ? "Entries" : "Spawns")));
                if (_lootTables) right.Add(WeightSummary());
                right.Add(new Button(() => RemoveGroup(groups)) { text = _lootTables ? "Remove loot table" : "Remove scene" });
            }

            split.Add(right);
            root.Add(split);
            root.Bind(_serialized);
            root.TrackSerializedObjectValue(_serialized, _ => groupList.RefreshItems());
        }

        private string GroupLabel(SerializedProperty group, int index)
        {
            var id = group.FindPropertyRelative(_lootTables ? "LootTable" : "Scene").FindPropertyRelative("id").stringValue;
            var rows = group.FindPropertyRelative(_lootTables ? "Entries" : "Spawns").arraySize;
            return $"{(string.IsNullOrEmpty(id) ? $"(unnamed {index + 1})" : id)}   {rows}";
        }

        /// <summary>A text box that turns pasted coordinates into a new spawn row.</summary>
        private VisualElement PasteBox(SerializedProperty spawns)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4, marginBottom = 4 } };
            var text = new TextField("Paste coordinates") { style = { flexGrow = 1 }, tooltip = "e.g. p=412.3,18.75,-120.4 r=0,90,0, or (412.3, 18.8, -120.4)" };
            var add = new Button { text = "Add row" };
            add.clicked += () =>
            {
                if (!CoordinateParser.TryParse(text.value, out var position, out var rotation))
                {
                    text.tooltip = "Couldn't find three numbers in that text.";
                    return;
                }

                _serialized.Update();
                spawns.arraySize++;
                var element = spawns.GetArrayElementAtIndex(spawns.arraySize - 1);
                element.FindPropertyRelative("Position").vector3Value = position;
                element.FindPropertyRelative("Rotation").vector3Value = rotation ?? Vector3.zero;
                element.FindPropertyRelative("Chance").floatValue = 100f;
                element.FindPropertyRelative("Item").FindPropertyRelative("id").stringValue = string.Empty;
                _serialized.ApplyModifiedProperties();
                text.value = string.Empty;
            };
            row.Add(text);
            row.Add(add);
            return row;
        }

        private VisualElement WeightSummary()
        {
            var label = new Label { style = { opacity = 0.7f, whiteSpace = WhiteSpace.Normal, marginTop = 4 } };
            void Update()
            {
                var entries = Table.LootTables[_group].Entries.Where(e => e != null).ToList();
                var total = entries.Sum(e => e.Weight);
                label.text = total == 0 ? string.Empty
                    : string.Join("   ", entries.Select(e => $"{(e.Item.IsEmpty ? "?" : e.Item.Id)} {100f * e.Weight / total:0.#}% of these entries"));
            }

            Update();
            label.TrackSerializedObjectValue(_serialized, _ => Update());
            return label;
        }

        private void AddGroup()
        {
            Undo.RecordObject(Table, "Add spawn group");
            if (_lootTables) Table.LootTables.Add(new SpawnTable.LootTableGroup());
            else Table.Scenes.Add(new SpawnTable.SceneGroup());
            EditorUtility.SetDirty(Table);
            _group = _lootTables ? Table.LootTables.Count - 1 : Table.Scenes.Count - 1;
            Rebuild();
        }

        private void RemoveGroup(SerializedProperty groups)
        {
            groups.DeleteArrayElementAtIndex(_group);
            _serialized.ApplyModifiedProperties();
            _group = Mathf.Max(0, _group - 1);
            Rebuild();
        }

        private void PreviewFile()
        {
            var preview = GetWindow<TextPreviewWindow>(true, "gear-spawns preview");
            preview.ShowText(ModWriters.GearSpawns(Table));
        }
    }
}
