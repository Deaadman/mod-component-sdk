using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.UI.Windows;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Integrations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Inspectors
{
    /// <summary>The Mod inspector: identity, content, tables, integrations, build, and every issue in the mod.</summary>
    [CustomEditor(typeof(Mod))]
    public sealed class ModEditor : UnityEditor.Editor
    {
        private VisualElement _issues;
        private Label _buildStatus;
        private IVisualElementScheduledItem _pendingValidation;

        private Mod Mod => (Mod)target;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement().WithStyles();

            var identity = SdkUI.Section("Mod", out var c);
            c.Add(new PropertyField(serializedObject.FindProperty("displayName"), "Name"));
            var idRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var idField = new PropertyField(serializedObject.FindProperty("id"), "Id") { style = { flexGrow = 1 } };
            idRow.Add(idField);
            idRow.Add(new Button(() =>
            {
                serializedObject.FindProperty("id").stringValue = Mod.ToId(Mod.DisplayName);
                serializedObject.ApplyModifiedProperties();
            }) { text = "From name", tooltip = "Make the id from the name" });
            c.Add(idRow);
            c.Add(new PropertyField(serializedObject.FindProperty("author")));
            c.Add(new PropertyField(serializedObject.FindProperty("version")));
            c.Add(new PropertyField(serializedObject.FindProperty("description")));
            c.Add(new PropertyField(serializedObject.FindProperty("requiredMods"), "Required mods"));
            c.Add(new PropertyField(serializedObject.FindProperty("requiresDLC"), "Requires DLC"));
            root.Add(identity);

            var content = SdkUI.Section("Content", out c);
            c.Add(new PropertyField(serializedObject.FindProperty("items")));
            c.Add(SdkUI.Row(
                new Button(() => NewItemWindow.Open(Mod)) { text = "New item…" },
                new Button(() =>
                {
                    var bp = AssetFactory.CreateBlueprint(Mod, "BP_New");
                    Selection.activeObject = bp;
                }) { text = "New blueprint" }));
            c.Add(new PropertyField(serializedObject.FindProperty("blueprints")));
            c.Add(new PropertyField(serializedObject.FindProperty("bundledAssets"), "Bundled assets"));
            c.Add(new PropertyField(serializedObject.FindProperty("extraFiles"), "Extra files"));
            root.Add(content);

            var tables = SdkUI.Section("Tables", out c);
            c.Add(new PropertyField(serializedObject.FindProperty("localization")));
            c.Add(new PropertyField(serializedObject.FindProperty("spawns")));
            c.Add(SdkUI.Row(
                new Button(() => { if (Mod.Localization == null) AssetFactory.CreateLocalizationTable(Mod); LocalizationWindow.Open(Mod); }) { text = "Edit localization" },
                new Button(() => { if (Mod.Spawns == null) AssetFactory.CreateSpawnTable(Mod); SpawnsWindow.Open(Mod); }) { text = "Edit spawns" }));
            root.Add(tables);

            var integrations = SdkUI.Section("Integrations", out c);
            var rarities = new Toggle("Item Rarities") { value = Mod.HasIntegration<ItemRaritiesIntegration>(), tooltip = "Write rarities.ir for the Item Rarities mod." };
            rarities.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(Mod, "Toggle Item Rarities");
                if (e.newValue) Mod.EnableIntegration<ItemRaritiesIntegration>();
                else Mod.DisableIntegration<ItemRaritiesIntegration>();
                EditorUtility.SetDirty(Mod);
            });
            c.Add(rarities);
            root.Add(integrations);

            var build = SdkUI.Section("Build", out c);
            _buildStatus = new Label();
            _buildStatus.style.whiteSpace = WhiteSpace.Normal;
            c.Add(_buildStatus);
            c.Add(SdkUI.Row(
                new Button(() => BuildWindow.Open(Mod)) { text = "Build…" },
                new Button(Revalidate) { text = "Validate" }));
            root.Add(build);
            RefreshBuildStatus();

            var issues = SdkUI.Section("Issues", out _issues);
            root.Add(issues);
            Revalidate();

            root.TrackSerializedObjectValue(serializedObject, _ =>
            {
                _pendingValidation?.Pause();
                _pendingValidation = root.schedule.Execute(Revalidate).StartingIn(400);
            });
            return root;
        }

        private void RefreshBuildStatus()
        {
            var output = BuildPreferences.GetOutputDirectory(Mod);
            _buildStatus.text = string.IsNullOrEmpty(output)
                ? "Not built yet. Choose an output folder in the build window."
                : $"Builds to {Path.Combine(output, ModBuildPipeline.ArchiveFileName(Mod))}";
        }

        private void Revalidate()
        {
            if (_issues == null || Mod == null) return;
            _issues.Clear();
            _issues.Add(SdkUI.IssueList(ModValidator.Validate(Mod).Issues, Revalidate));
        }
    }

    /// <summary>Default fields for a blueprint, plus its validation issues.</summary>
    [CustomEditor(typeof(Blueprint))]
    public sealed class BlueprintEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement().WithStyles();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var issuesSection = SdkUI.Section("Issues", out var issues);
            root.Add(issuesSection);

            void Revalidate()
            {
                issues.Clear();
                var bp = (Blueprint)target;
                var mods = SdkUI.OwningMods(bp).ToList();
                if (mods.Count == 0)
                {
                    issues.Add(new HelpBox("This blueprint isn't in any mod yet, so it won't be built.", HelpBoxMessageType.Info));
                    return;
                }

                issues.Add(SdkUI.IssueList(mods.SelectMany(m => ModValidator.Validate(m).For(bp)), Revalidate, showTarget: false));
            }

            Revalidate();
            IVisualElementScheduledItem pending = null;
            root.TrackSerializedObjectValue(serializedObject, _ =>
            {
                pending?.Pause();
                pending = root.schedule.Execute(Revalidate).StartingIn(400);
            });
            return root;
        }
    }
}
