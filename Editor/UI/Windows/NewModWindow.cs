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
    /// <summary>Creates a mod with its tables and folders. Every value can be changed later on the Mod asset.</summary>
    public sealed class NewModWindow : EditorWindow
    {
        private const string AuthorKey = "ModComponentSDK.DefaultAuthor";

        [MenuItem("ModComponent SDK/New Mod…", false, 20)]
        public static void Open()
        {
            var window = GetWindow<NewModWindow>(true, "New Mod", true);
            window.minSize = window.maxSize = new Vector2(420, 250);
        }

        public static Action<Mod> Created;

        private void CreateGUI()
        {
            var root = new VisualElement().WithStyles();
            root.AddToClassList("mcsdk-form");
            rootVisualElement.Add(root);

            var name = new TextField("Name") { value = "My Mod" };
            var id = new TextField("Id") { value = Mod.ToId(name.value), tooltip = "Letters, digits, '-' and '_'. Names the .modcomponent file." };
            var author = new TextField("Author") { value = EditorPrefs.GetString(AuthorKey, string.Empty) };
            var version = new TextField("Version") { value = "1.0.0" };
            var folder = new TextField("Folder") { value = $"{AssetFactory.DefaultModsFolder}/{id.value}" };
            var rarities = new Toggle("Item Rarities support");
            var error = new HelpBox(string.Empty, HelpBoxMessageType.Error) { style = { display = DisplayStyle.None } };

            var idEdited = false;
            var folderEdited = false;
            name.RegisterValueChangedCallback(e =>
            {
                if (!idEdited) id.SetValueWithoutNotify(Mod.ToId(e.newValue));
                if (!folderEdited) folder.SetValueWithoutNotify($"{AssetFactory.DefaultModsFolder}/{id.value}");
            });
            id.RegisterValueChangedCallback(_ => idEdited = true);
            folder.RegisterValueChangedCallback(_ => folderEdited = true);

            foreach (var element in new VisualElement[] { name, id, author, version, folder, rarities, error }) root.Add(element);

            var actions = new VisualElement();
            actions.AddToClassList("mcsdk-form__actions");
            actions.Add(new Button(Close) { text = "Cancel" });
            actions.Add(new Button(() =>
            {
                try
                {
                    if (!folder.value.StartsWith("Assets/", StringComparison.Ordinal))
                        throw new ArgumentException("The folder must be inside Assets/.");
                    if (ModValidator.FindAllMods().Any(m => string.Equals(m.Id, id.value, StringComparison.OrdinalIgnoreCase)))
                        throw new ArgumentException($"Another mod already uses the id {id.value}.");
                    EditorPrefs.SetString(AuthorKey, author.value);
                    var mod = AssetFactory.CreateMod(new AssetFactory.ModSettings
                    {
                        DisplayName = name.value, Id = id.value, Author = author.value, Version = version.value,
                        Folder = folder.value, ItemRarities = rarities.value
                    });
                    Selection.activeObject = mod;
                    Created?.Invoke(mod);
                    Close();
                }
                catch (Exception e)
                {
                    error.text = e.Message;
                    error.style.display = DisplayStyle.Flex;
                }
            }) { text = "Create" });
            root.Add(actions);
        }
    }
}
