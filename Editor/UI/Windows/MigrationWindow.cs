using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Migration;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using V1Definition = ModComponent.SDK.Components.ModDefinition;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// Brings 1.x mods into 2.0. A dry run shows what will be created; importing writes new assets beside the old
    /// ones and changes nothing from 1.x. Removing the old scripts from prefabs is a separate, explicit step.
    /// </summary>
    public sealed class MigrationWindow : EditorWindow
    {
        private readonly HashSet<V1Definition> _selected = new();
        private VisualElement _results;

        [MenuItem("ModComponent SDK/Import 1.x Mods…", false, 60)]
        public static void Open()
        {
            var window = GetWindow<MigrationWindow>("Import 1.x mods");
            window.minSize = new Vector2(640, 380);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement.WithStyles();
            root.Clear();
            var body = new ScrollView { style = { paddingLeft = 10, paddingRight = 10, paddingTop = 8, flexGrow = 1 } };
            root.Add(body);

            var definitions = V1Importer.FindDefinitions().ToList();
            body.Add(new HelpBox(definitions.Count == 0
                ? "No mods made with SDK 1.x were found in this project."
                : $"Found {definitions.Count} mod(s) made with SDK 1.x. Importing creates new 2.0 assets in Assets/Mods and " +
                  "doesn't change or delete anything from 1.x.", HelpBoxMessageType.Info));

            foreach (var definition in definitions)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 } };
                var imported = V1Importer.IsImported(definition);
                var toggle = new Toggle { value = !imported };
                if (!imported) _selected.Add(definition);
                toggle.RegisterValueChangedCallback(e =>
                {
                    if (e.newValue) _selected.Add(definition);
                    else _selected.Remove(definition);
                });
                row.Add(toggle);
                var items = definition.Items?.Count(i => i != null) ?? 0;
                var keys = definition.modLocalization?.localizationEntries?.Count ?? 0;
                row.Add(new Label($"{definition.Name}   ·   {items} items · {keys} localization keys" + (imported ? "   ·   already imported" : string.Empty)));
                body.Add(row);
            }

            _results = new VisualElement { style = { marginTop = 8 } };
            body.Add(_results);

            var actions = new VisualElement();
            actions.AddToClassList("mcsdk-form__actions");
            actions.style.paddingRight = 10;
            actions.style.paddingBottom = 8;
            actions.Add(new Button(() => Run(true)) { text = "Dry run" });
            actions.Add(new Button(() => Run(false)) { text = "Import" });
            root.Add(actions);
        }

        private void Run(bool dryRun)
        {
            _results.Clear();
            var imported = new List<Mod>();
            foreach (var definition in _selected.ToList())
            {
                var report = V1Importer.Import(definition, dryRun);
                var section = SdkUI.Section($"{definition.Name}{(dryRun ? " (dry run)" : string.Empty)}", out var content);
                content.Add(new Label($"{report.Items.Count} items, {report.Blueprints.Count} blueprints, " +
                                      $"{report.LocalizationKeys} localization keys, {report.Spawns} spawns" +
                                      (dryRun ? " would be created." : " created.")));
                foreach (var note in report.Notes)
                    content.Add(new Label("• " + note) { style = { whiteSpace = WhiteSpace.Normal, opacity = 0.85f } });
                if (!dryRun && report.Mod != null) imported.Add(report.Mod);
                _results.Add(section);
            }

            if (dryRun || imported.Count == 0) return;

            var prefabs = imported.SelectMany(m => m.Items).Select(i => i.Prefab).Where(V1Importer.HasLegacyComponents).Distinct().ToList();
            if (prefabs.Count > 0)
            {
                var section = SdkUI.Section("Next: remove 1.x scripts", out var content);
                content.Add(new Label($"{prefabs.Count} prefab(s) still carry 1.x scripts, which would show as missing scripts in game. " +
                                      "Remove them once you've checked the import. This edits the prefab files and can't be undone with Ctrl+Z, so commit or back up your project first.")
                    { style = { whiteSpace = WhiteSpace.Normal } });
                content.Add(new Button(() =>
                {
                    var removed = prefabs.Sum(V1Importer.RemoveLegacyComponents);
                    content.Add(new Label($"Removed {removed} script(s)."));
                }) { text = "Remove 1.x scripts" });
                _results.Add(section);
            }

            Selection.activeObject = imported[0];
        }
    }
}
