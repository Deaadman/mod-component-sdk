using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.References;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.References
{
    /// <summary>One choice in a reference picker.</summary>
    public readonly struct ReferenceOption
    {
        public ReferenceOption(string id, string group, string hint = null)
        {
            Id = id;
            Group = group;
            Hint = hint;
        }

        public string Id { get; }
        public string Group { get; }
        public string Hint { get; }
    }

    /// <summary>What each reference type can pick from, and whether a typed name is known.</summary>
    public static class ReferenceSources
    {
        public static IEnumerable<ReferenceOption> For(Type referenceType, UnityEngine.Object owner)
        {
            var catalog = GameDataCatalog.Default;
            if (referenceType == typeof(GearRef))
            {
                foreach (var mod in ModValidator.FindAllMods())
                foreach (var item in mod.Items.Where(i => i != null))
                    yield return new ReferenceOption(item.GearName, $"Project/{mod.DisplayName}");
                foreach (var gear in catalog.Gear)
                    yield return new ReferenceOption(gear.Id, gear.IsVanilla ? "Vanilla" : gear.Source);
            }
            else if (referenceType == typeof(SoundRef)) foreach (var s in catalog.Sounds) yield return new ReferenceOption(s, SoundGroup(s));
            else if (referenceType == typeof(SceneRef)) foreach (var s in catalog.Scenes) yield return new ReferenceOption(s, "Scenes");
            else if (referenceType == typeof(LootTableRef)) foreach (var s in catalog.LootTables) yield return new ReferenceOption(s, "Loot tables");
            else if (referenceType == typeof(LiquidRef)) foreach (var s in catalog.Liquids) yield return new ReferenceOption(s, "Liquids");
            else if (referenceType == typeof(PowderRef)) foreach (var s in catalog.Powders) yield return new ReferenceOption(s, "Powders");
            else if (referenceType == typeof(LocalizationKey))
            {
                foreach (var table in Tables(owner))
                foreach (var entry in table.Entries.Where(e => !string.IsNullOrEmpty(e.Key)))
                    yield return new ReferenceOption(entry.Key, table.name, entry.Get(LocalizationTable.DefaultLanguage));
            }
            else if (referenceType == typeof(ChildRef) && owner is GearItem { Prefab: not null } item)
            {
                foreach (Transform child in item.Prefab.transform) yield return new ReferenceOption(child.name, item.Prefab.name);
            }
        }

        /// <summary>Null when the name can't be judged (empty, or no list to check against).</summary>
        public static bool? IsKnown(Type referenceType, string id, UnityEngine.Object owner)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (referenceType == typeof(LocalizationKey)) return Tables(owner).Any(t => t.Contains(id));
            if (referenceType == typeof(ChildRef))
                return owner is GearItem { Prefab: not null } item ? item.Prefab.transform.Find(id) != null : null;
            if (referenceType == typeof(GearRef) && ModValidator.FindAllMods().Any(m => m.FindItem(id) != null)) return true;

            var kind = KindOf(referenceType);
            return kind.HasValue ? GameDataCatalog.Default.Contains(kind.Value, id) : null;
        }

        /// <summary>The English text for a localization key, if the owner's mod has it.</summary>
        public static string Hint(Type referenceType, string id, UnityEngine.Object owner) =>
            referenceType == typeof(LocalizationKey) && !string.IsNullOrEmpty(id)
                ? Tables(owner).Select(t => t.Get(id)).FirstOrDefault(t => !string.IsNullOrEmpty(t))
                : null;

        public static CatalogKind? KindOf(Type referenceType)
        {
            if (referenceType == typeof(GearRef)) return CatalogKind.Gear;
            if (referenceType == typeof(SoundRef)) return CatalogKind.Sound;
            if (referenceType == typeof(SceneRef)) return CatalogKind.Scene;
            if (referenceType == typeof(LootTableRef)) return CatalogKind.LootTable;
            if (referenceType == typeof(LiquidRef)) return CatalogKind.Liquid;
            if (referenceType == typeof(PowderRef)) return CatalogKind.Powder;
            return null;
        }

        private static IEnumerable<LocalizationTable> Tables(UnityEngine.Object owner)
        {
            IEnumerable<Mod> mods = owner switch
            {
                GearItem item => SdkUI.OwningMods(item),
                Blueprint bp => SdkUI.OwningMods(bp),
                Mod mod => new[] { mod },
                _ => Enumerable.Empty<Mod>()
            };
            var tables = mods.Select(m => m.Localization).Where(t => t != null).Distinct().ToList();
            return tables.Count > 0
                ? tables
                : AssetDatabase.FindAssets("t:" + nameof(LocalizationTable))
                    .Select(g => AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(t => t != null);
        }

        /// <summary>Groups sounds by their first word after Play_, so 1,600 events stay browsable.</summary>
        private static string SoundGroup(string id)
        {
            var name = id.StartsWith("Play_") ? id.Substring(5) : id;
            var end = 1;
            while (end < name.Length && !char.IsUpper(name[end]) && name[end] != '_') end++;
            return "Sounds/" + name.Substring(0, end);
        }
    }

    /// <summary>A searchable dropdown of <see cref="ReferenceOption"/>s, grouped by source.</summary>
    public sealed class ReferenceDropdown : AdvancedDropdown
    {
        private readonly string _title;
        private readonly IReadOnlyList<ReferenceOption> _options;
        private readonly Action<string> _picked;

        public ReferenceDropdown(string title, IEnumerable<ReferenceOption> options, Action<string> picked)
            : base(new AdvancedDropdownState())
        {
            _title = title;
            _options = options.ToList();
            _picked = picked;
            minimumSize = new Vector2(320, 360);
        }

        public int OptionCount => _options.Count;

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(_title);
            root.AddChild(new OptionItem("(none)", string.Empty));
            var groups = new Dictionary<string, AdvancedDropdownItem>();
            foreach (var option in _options)
            {
                var parent = GroupFor(root, groups, option.Group);
                var label = string.IsNullOrEmpty(option.Hint) ? option.Id : $"{option.Id}  —  {Shorten(option.Hint)}";
                parent.AddChild(new OptionItem(label, option.Id));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is OptionItem option) _picked(option.Value);
        }

        private static AdvancedDropdownItem GroupFor(AdvancedDropdownItem root, Dictionary<string, AdvancedDropdownItem> groups, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            if (groups.TryGetValue(path, out var existing)) return existing;
            var slash = path.LastIndexOf('/');
            var parent = slash < 0 ? root : GroupFor(root, groups, path.Substring(0, slash));
            var group = new AdvancedDropdownItem(slash < 0 ? path : path.Substring(slash + 1));
            parent.AddChild(group);
            groups[path] = group;
            return group;
        }

        private static string Shorten(string text) => text.Length > 40 ? text.Substring(0, 39) + "…" : text;

        private sealed class OptionItem : AdvancedDropdownItem
        {
            public OptionItem(string name, string value) : base(name) => Value = value;
            public string Value { get; }
        }
    }

    /// <summary>
    /// Draws every reference type as an editable text field plus a picker button. Unknown names get a
    /// yellow dot; localization keys show their English text.
    /// </summary>
    [CustomPropertyDrawer(typeof(GearRef))]
    [CustomPropertyDrawer(typeof(SoundRef))]
    [CustomPropertyDrawer(typeof(SceneRef))]
    [CustomPropertyDrawer(typeof(LootTableRef))]
    [CustomPropertyDrawer(typeof(LiquidRef))]
    [CustomPropertyDrawer(typeof(PowderRef))]
    [CustomPropertyDrawer(typeof(LocalizationKey))]
    [CustomPropertyDrawer(typeof(ChildRef))]
    public sealed class ReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var idProperty = property.FindPropertyRelative("id");
            var referenceType = fieldInfo.FieldType.IsGenericType ? fieldInfo.FieldType.GetGenericArguments()[0]
                : fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;
            var owner = property.serializedObject.targetObject;

            var root = new VisualElement().WithStyles();
            root.AddToClassList("mcsdk-ref");

            var field = new TextField(preferredLabel ?? property.displayName) { isDelayed = true };
            field.AddToClassList("mcsdk-ref__field");
            field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            field.BindProperty(idProperty);
            root.Add(field);

            var hint = new Label();
            hint.AddToClassList("mcsdk-ref__hint");
            root.Add(hint);

            var state = new VisualElement();
            state.AddToClassList("mcsdk-ref__state");
            root.Add(state);

            var pick = new Button { text = "▾", tooltip = "Pick from the list" };
            pick.AddToClassList("mcsdk-ref__pick");
            pick.clicked += () =>
            {
                var dropdown = new ReferenceDropdown(ObjectNames.NicifyVariableName(referenceType.Name),
                    ReferenceSources.For(referenceType, owner), id =>
                    {
                        idProperty.stringValue = id;
                        idProperty.serializedObject.ApplyModifiedProperties();
                    });
                dropdown.Show(pick.worldBound);
            };
            root.Add(pick);

            void Refresh(string id)
            {
                var known = ReferenceSources.IsKnown(referenceType, id, owner);
                state.EnableInClassList("mcsdk-ref__state--unknown", known == false);
                state.tooltip = known == false ? $"\"{id}\" isn't in the list. That's fine for names from other mods." : string.Empty;
                var text = ReferenceSources.Hint(referenceType, id, owner);
                hint.text = text ?? string.Empty;
                hint.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            Refresh(idProperty.stringValue);
            field.RegisterValueChangedCallback(e => Refresh(e.newValue));
            return root;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("id"), label);
        }
    }
}
