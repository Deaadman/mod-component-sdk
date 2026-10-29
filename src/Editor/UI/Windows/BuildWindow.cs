using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Validation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// Builds one mod: output and after-build options (saved per user), the pipeline's steps, and the
    /// build log with errors and warnings highlighted.
    /// </summary>
    public sealed class BuildWindow : EditorWindow
    {
        private static readonly (BuildStep step, string label)[] Steps =
        {
            (BuildStep.Check, "Check"), (BuildStep.Stage, "Stage"), (BuildStep.Write, "Write data files"),
            (BuildStep.Bundle, "Build bundles"), (BuildStep.Package, "Package"), (BuildStep.Copy, "Copy to game")
        };

        [SerializeField] private Mod _mod;

        private VisualElement _steps;
        private ListView _log;
        private Label _summary;
        private BuildReport _report;

        public static void Open(Mod mod)
        {
            var window = GetWindow<BuildWindow>();
            window._mod = mod;
            window.titleContent = new GUIContent($"Build · {mod.DisplayName}");
            window.minSize = new Vector2(620, 340);
            window._report = null;
            window.Rebuild();
        }

        private void CreateGUI() => Rebuild();

        private void Rebuild()
        {
            var root = rootVisualElement.WithStyles();
            root.Clear();
            if (_mod == null)
            {
                root.Add(new HelpBox("Open this window from a mod.", HelpBoxMessageType.Info));
                return;
            }

            var split = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };

            var left = new ScrollView { style = { paddingLeft = 8, paddingRight = 8, paddingTop = 6 } };
            var options = SdkUI.Section("Output", out var c);
            var output = new TextField("Folder") { value = BuildPreferences.GetOutputDirectory(_mod), isReadOnly = true };
            c.Add(output);
            c.Add(SdkUI.Row(new Button(() =>
            {
                var picked = EditorUtility.SaveFolderPanel("Build folder", output.value, string.Empty);
                if (string.IsNullOrEmpty(picked)) return;
                BuildPreferences.SetOutputDirectory(_mod, picked);
                output.value = picked;
            }) { text = "Choose…" }, new Button(() =>
            {
                if (Directory.Exists(output.value)) EditorUtility.RevealInFinder(output.value);
            }) { text = "Open" }));
            left.Add(options);

            var game = SdkUI.Section("The Long Dark", out c);
            var gameFolder = new TextField("Game folder") { value = BuildPreferences.GameFolder, isReadOnly = true, tooltip = "The folder that contains TheLongDark.exe." };
            var copy = new Toggle("Copy to Mods folder") { value = BuildPreferences.GetCopyToGame(_mod) };
            copy.SetEnabled(!string.IsNullOrEmpty(BuildPreferences.GameModsFolder));
            copy.RegisterValueChangedCallback(e => BuildPreferences.SetCopyToGame(_mod, e.newValue));
            c.Add(gameFolder);
            c.Add(SdkUI.Row(new Button(() =>
            {
                var picked = EditorUtility.OpenFolderPanel("The Long Dark folder", gameFolder.value, string.Empty);
                if (string.IsNullOrEmpty(picked)) return;
                if (!File.Exists(Path.Combine(picked, "TheLongDark.exe")))
                    EditorUtility.DisplayDialog("The Long Dark folder", "That folder doesn't contain TheLongDark.exe.", "OK");
                BuildPreferences.GameFolder = picked;
                gameFolder.value = picked;
                copy.SetEnabled(!string.IsNullOrEmpty(BuildPreferences.GameModsFolder));
            }) { text = "Choose…" }));
            c.Add(copy);
            left.Add(game);

            var stepsSection = SdkUI.Section("Steps", out _steps);
            left.Add(stepsSection);
            left.Add(new Button(RunBuild) { text = "Build", style = { height = 28, marginTop = 6 } });
            split.Add(left);

            var right = new VisualElement { style = { flexGrow = 1 } };
            _summary = new Label { style = { paddingLeft = 6, paddingTop = 4, paddingBottom = 4, whiteSpace = WhiteSpace.Normal } };
            right.Add(_summary);
            _log = new ListView
            {
                makeItem = () => new Label { style = { whiteSpace = WhiteSpace.Normal, paddingLeft = 6, unityTextAlign = TextAnchor.MiddleLeft } },
                bindItem = (e, i) =>
                {
                    var message = _report.Messages[i];
                    var label = (Label)e;
                    label.text = message.Text;
                    label.style.color = message.Severity switch
                    {
                        BuildMessageSeverity.Error => new Color(1f, 0.55f, 0.5f),
                        BuildMessageSeverity.Warning => new Color(0.95f, 0.75f, 0.4f),
                        _ => StyleKeyword.Null
                    };
                },
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = { flexGrow = 1 }
            };
            right.Add(_log);
            var actions = SdkUI.Row(new Button(() =>
            {
                if (_report?.ArchivePath != null) EditorUtility.RevealInFinder(_report.ArchivePath);
            }) { text = "Show file" });
            right.Add(actions);
            split.Add(right);
            root.Add(split);

            ShowReport();
        }

        private void RunBuild()
        {
            if (string.IsNullOrEmpty(BuildPreferences.GetOutputDirectory(_mod)))
            {
                var picked = EditorUtility.SaveFolderPanel("Build folder", string.Empty, string.Empty);
                if (string.IsNullOrEmpty(picked)) return;
                BuildPreferences.SetOutputDirectory(_mod, picked);
            }

            var mod = _mod;
            _report = BuildActions.Build(mod);
            // An Addressables build can reload assets; look the mod up again before rebuilding the view.
            if (_mod == null) _mod = AssetDatabase.LoadAssetAtPath<Mod>(AssetDatabase.GetAssetPath(mod));
            Rebuild();
        }

        private void ShowReport()
        {
            _steps.Clear();
            foreach (var (step, text) in Steps)
            {
                if (step == BuildStep.Copy && !BuildPreferences.GetCopyToGame(_mod)) continue;
                var failed = _report is { Succeeded: false } && step == _report.Step;
                var done = _report != null && (_report.Succeeded ? step <= _report.Step : step < _report.Step);
                var icon = failed ? "✕" : done ? "✓" : "○";
                var label = new Label($"{icon}  {text}");
                if (failed) label.style.color = new Color(1f, 0.55f, 0.5f);
                _steps.Add(label);
            }

            if (_report == null)
            {
                var result = ModValidator.Validate(_mod);
                _summary.text = $"{_mod.DisplayName} {_mod.Version}: {result.Errors.Count()} errors, {result.Warnings.Count()} warnings before building.";
                _log.itemsSource = new System.Collections.Generic.List<BuildMessage>();
                _report = null;
                return;
            }

            _summary.text = _report.Succeeded
                ? $"Built in {_report.Duration.TotalSeconds:0.0} s: {_report.ArchivePath} ({_report.Entries.Count} files, {_report.Warnings.Count()} warnings)."
                : $"Build failed with {_report.Errors.Count()} errors.";
            _log.itemsSource = _report.Messages;
            _log.Rebuild();
        }
    }
}
