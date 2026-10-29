using System;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Editor.Validation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>
    /// Creates an item from a model and a template: a GEAR_ prefab that passes validation, the GearItem with
    /// starting modules, and English name and description keys.
    /// </summary>
    public sealed class NewItemWindow : EditorWindow
    {
        private Mod _mod;

        [MenuItem("ModComponent SDK/New Item…", false, 21)]
        public static void OpenFromMenu() => Open(Selection.activeObject as Mod ?? ModValidator.FindAllMods().FirstOrDefault());

        public static void Open(Mod mod)
        {
            var window = GetWindow<NewItemWindow>(true, "New Item", true);
            window._mod = mod;
            window.minSize = window.maxSize = new Vector2(460, 300);
            window.Rebuild();
        }

        public static Action<GearItem> Created;

        private void CreateGUI() => Rebuild();

        private void Rebuild()
        {
            rootVisualElement.Clear();
            var root = new VisualElement().WithStyles();
            root.AddToClassList("mcsdk-form");
            rootVisualElement.Add(root);

            var mods = ModValidator.FindAllMods().ToList();
            if (mods.Count == 0)
            {
                root.Add(new HelpBox("Create a mod first.", HelpBoxMessageType.Info));
                root.Add(new Button(() => { Close(); NewModWindow.Open(); }) { text = "New Mod…" });
                return;
            }

            _mod ??= mods[0];
            var modField = new PopupField<Mod>("Mod", mods, mods.Contains(_mod) ? _mod : mods[0], m => m.DisplayName, m => m.DisplayName);
            var gearName = new TextField("Gear name") { value = "GEAR_", tooltip = "GEAR_ followed by letters, digits or '_'." };
            var displayName = new TextField("Display name") { tooltip = "English name. Stored in the mod's localization table." };
            var model = new ObjectField("Model") { objectType = typeof(GameObject), allowSceneObjects = false, tooltip = "An FBX or prefab. Optional." };
            var template = new PopupField<ItemTemplate>("Start from", ItemTemplates.All.ToList(), 0, t => t.Name, t => t.Name);
            var templateHelp = new Label(template.value.Description) { style = { opacity = 0.7f, marginLeft = 3, whiteSpace = WhiteSpace.Normal } };
            template.RegisterValueChangedCallback(e => templateHelp.text = e.newValue.Description);
            var error = new HelpBox(string.Empty, HelpBoxMessageType.Error) { style = { display = DisplayStyle.None } };

            displayName.RegisterValueChangedCallback(e =>
            {
                if (gearName.value is "GEAR_" or "" || gearName.userData is true)
                {
                    try
                    {
                        gearName.SetValueWithoutNotify(AssetFactory.NormalizeGearName(e.newValue));
                        gearName.userData = true;
                    }
                    catch (ArgumentException) { }
                }
            });
            gearName.RegisterValueChangedCallback(_ => gearName.userData = false);

            foreach (var element in new VisualElement[] { modField, displayName, gearName, model, template, templateHelp, error }) root.Add(element);

            var actions = new VisualElement();
            actions.AddToClassList("mcsdk-form__actions");
            actions.Add(new Button(Close) { text = "Cancel" });
            actions.Add(new Button(() =>
            {
                try
                {
                    var item = AssetFactory.CreateItem(new AssetFactory.ItemSettings
                    {
                        Mod = modField.value, GearName = gearName.value, DisplayName = displayName.value,
                        Model = (GameObject)model.value, Template = template.value
                    });
                    Selection.activeObject = item;
                    Created?.Invoke(item);
                    Close();
                }
                catch (Exception e)
                {
                    error.text = e.Message;
                    error.style.display = DisplayStyle.Flex;
                }
            }) { text = "Create item" });
            root.Add(actions);
        }
    }
}
