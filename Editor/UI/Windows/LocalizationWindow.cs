using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Editor.Validation.Rules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// The localization table as a spreadsheet: one row per key, one column per chosen language, a "missing only"
    /// view and CSV import/export for translators.
    /// </summary>
    public sealed class LocalizationWindow : EditorWindow
    {
        private const string LanguagesKey = "ModComponentSDK.Localization.Languages";

        [SerializeField] private Mod _mod;

        private MultiColumnListView _grid;
        private ToolbarSearchField _search;
        private ToolbarToggle _missingOnly;
        private Label _status;
        private List<LocalizationTable.Entry> _rows = new();
        private Dictionary<string, int> _usage = new();

        public static void Open(Mod mod)
        {
            var window = GetWindow<LocalizationWindow>();
            window._mod = mod;
            window.titleContent = new GUIContent($"Localization · {mod.DisplayName}");
            window.minSize = new Vector2(640, 300);
            window.Rebuild();
        }

        private LocalizationTable Table => _mod != null ? _mod.Localization : null;

        private static IReadOnlyList<string> AllLanguages => GameDataCatalog.Default.Languages;

        private List<string> ShownLanguages
        {
            get
            {
                var saved = EditorPrefs.GetString(LanguagesKey, LocalizationTable.DefaultLanguage);
                var shown = saved.Split('|').Where(l => AllLanguages.Contains(l)).ToList();
                if (!shown.Contains(LocalizationTable.DefaultLanguage)) shown.Insert(0, LocalizationTable.DefaultLanguage);
                return AllLanguages.Where(shown.Contains).ToList();
            }
            set => EditorPrefs.SetString(LanguagesKey, string.Join("|", value));
        }

        private void CreateGUI() => Rebuild();

        private void OnEnable() => Undo.undoRedoPerformed += Refresh;

        private void OnDisable() => Undo.undoRedoPerformed -= Refresh;

        private void Rebuild()
        {
            var root = rootVisualElement.WithStyles();
            root.Clear();
            if (Table == null)
            {
                root.Add(new HelpBox("Open this window from a mod that has a localization table.", HelpBoxMessageType.Info));
                return;
            }

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(AddKey) { text = "＋ Key" });
            toolbar.Add(new ToolbarButton(RemoveSelected) { text = "Remove" });
            toolbar.Add(new ToolbarButton(ImportCsv) { text = "Import CSV…" });
            toolbar.Add(new ToolbarButton(ExportCsv) { text = "Export CSV…" });
            var spacer = new VisualElement();
            spacer.AddToClassList("mcsdk-toolbar__spacer");
            toolbar.Add(spacer);
            _missingOnly = new ToolbarToggle { text = "Missing only" };
            _missingOnly.RegisterValueChangedCallback(_ => Refresh());
            toolbar.Add(_missingOnly);
            var languages = new ToolbarMenu { text = "Languages" };
            foreach (var language in AllLanguages)
            {
                languages.menu.AppendAction(language, _ =>
                {
                    var shown = ShownLanguages;
                    if (shown.Contains(language)) shown.Remove(language);
                    else shown.Add(language);
                    ShownLanguages = shown;
                    Rebuild();
                }, _ => language == LocalizationTable.DefaultLanguage ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled
                    : ShownLanguages.Contains(language) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            toolbar.Add(languages);
            _search = new ToolbarSearchField();
            _search.RegisterValueChangedCallback(_ => Refresh());
            toolbar.Add(_search);
            root.Add(toolbar);

            _grid = new MultiColumnListView
            {
                selectionType = SelectionType.Multiple,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = { flexGrow = 1 }
            };
            _grid.columns.Add(TextColumn("Key", 220, e => e.Key, (e, v) =>
            {
                var key = v?.Trim();
                if (string.IsNullOrEmpty(key) || Table.Contains(key)) return false;
                // Renames the key on every item and recipe that uses it, not just in the table.
                LocalizationRefactor.RenameKey(_mod, e.Key, key);
                CountUsage();
                return true;
            }));
            foreach (var language in ShownLanguages)
                _grid.columns.Add(TextColumn(language, 200, e => e.Get(language) ?? string.Empty, (e, v) =>
                {
                    e.Set(language, v);
                    return true;
                }, highlightEmpty: true));
            _grid.columns.Add(new Column
            {
                title = "Used by", width = 120,
                makeCell = () => new Label { style = { opacity = 0.7f, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (e, i) => ((Label)e).text = UsedBy(_rows[i].Key)
            });
            root.Add(_grid);

            _status = new Label();
            _status.AddToClassList("mcsdk-footer");
            root.Add(_status);

            Refresh();
        }

        private Column TextColumn(string title, float width, System.Func<LocalizationTable.Entry, string> get,
            System.Func<LocalizationTable.Entry, string, bool> set, bool highlightEmpty = false) => new()
        {
            title = title,
            width = width,
            makeCell = () =>
            {
                var field = new TextField { multiline = true, isDelayed = true };
                field.style.whiteSpace = WhiteSpace.Normal;
                field.RegisterValueChangedCallback(e =>
                {
                    if (field.userData is not LocalizationTable.Entry entry) return;
                    Undo.RecordObject(Table, "Edit localization");
                    if (!set(entry, e.newValue))
                    {
                        field.SetValueWithoutNotify(e.previousValue);
                        return;
                    }

                    EditorUtility.SetDirty(Table);
                    UpdateStatus();
                    if (highlightEmpty) Highlight(field);
                });
                return field;
            },
            bindCell = (element, index) =>
            {
                var field = (TextField)element;
                field.userData = _rows[index];
                field.SetValueWithoutNotify(get(_rows[index]));
                if (highlightEmpty) Highlight(field);
            }
        };

        private static void Highlight(TextField field) =>
            field.style.backgroundColor = string.IsNullOrEmpty(field.value) ? new Color(0.85f, 0.6f, 0.15f, 0.18f) : StyleKeyword.Null;

        private void Refresh()
        {
            if (_grid == null || Table == null) return;
            var filter = _search?.value?.Trim() ?? string.Empty;
            var shown = ShownLanguages;
            _rows = Table.Entries
                .Where(e => filter.Length == 0 || e.Key.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0
                            || e.Translations.Any(t => (t.Text ?? string.Empty).IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0))
                .Where(e => _missingOnly == null || !_missingOnly.value || shown.Any(l => string.IsNullOrEmpty(e.Get(l))))
                .ToList();
            CountUsage();
            _grid.itemsSource = _rows;
            _grid.Rebuild();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_status == null) return;
            var shown = ShownLanguages;
            var missing = Table.Entries.Sum(e => shown.Count(l => string.IsNullOrEmpty(e.Get(l))));
            var unused = Table.Entries.Count(e => UsedBy(e.Key).Length == 0);
            var englishMissing = Table.Entries.Count(e => string.IsNullOrEmpty(e.Get(LocalizationTable.DefaultLanguage)));
            _status.text = $"{Table.Entries.Count} keys · {missing} empty cells in shown languages · " +
                           $"{(englishMissing == 0 ? "English complete" : $"{englishMissing} without English")} · {unused} unused";
        }

        /// <summary>How many assets use each key, computed once per refresh rather than per cell.</summary>
        private void CountUsage() =>
            _usage = LocalizationRule.UsedKeys(_mod)
                .GroupBy(u => u.key)
                .ToDictionary(g => g.Key, g => g.Select(u => u.asset).Distinct().Count());

        private string UsedBy(string key)
        {
            var count = _usage.TryGetValue(key ?? string.Empty, out var n) ? n : 0;
            return count == 0 ? string.Empty : count == 1 ? "1 asset" : $"{count} assets";
        }

        private void AddKey()
        {
            Undo.RecordObject(Table, "Add localization key");
            var key = "GAMEPLAY_NewKey";
            for (var i = 2; Table.Contains(key); i++) key = $"GAMEPLAY_NewKey{i}";
            Table.GetOrAdd(key);
            EditorUtility.SetDirty(Table);
            _search.value = string.Empty;
            Refresh();
            _grid.ScrollToItem(_rows.Count - 1);
        }

        private void RemoveSelected()
        {
            var selected = _grid.selectedItems.OfType<LocalizationTable.Entry>().ToList();
            if (selected.Count == 0) return;
            Undo.RecordObject(Table, "Remove localization keys");
            foreach (var entry in selected) Table.Remove(entry.Key);
            EditorUtility.SetDirty(Table);
            Refresh();
        }

        private void ExportCsv()
        {
            var path = EditorUtility.SaveFilePanel("Export localization", string.Empty, $"{_mod.Id}-localization", "csv");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, LocalizationCsv.Export(Table, AllLanguages), new System.Text.UTF8Encoding(true));
            EditorUtility.RevealInFinder(path);
        }

        private void ImportCsv()
        {
            var path = EditorUtility.OpenFilePanel("Import localization", string.Empty, "csv");
            if (string.IsNullOrEmpty(path)) return;
            Undo.RecordObject(Table, "Import localization");
            try
            {
                var result = LocalizationCsv.Import(Table, File.ReadAllText(path), AllLanguages.ToList());
                EditorUtility.SetDirty(Table);
                var skipped = result.UnknownLanguages.Count > 0 ? $"\nSkipped unknown columns: {string.Join(", ", result.UnknownLanguages)}" : string.Empty;
                EditorUtility.DisplayDialog("Import localization", $"{result.Added} keys added, {result.Updated} updated.{skipped}", "OK");
            }
            catch (System.FormatException e)
            {
                EditorUtility.DisplayDialog("Import localization", e.Message, "OK");
            }

            Refresh();
        }
    }
}
