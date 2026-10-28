using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Modules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Inspectors
{
    /// <summary>
    /// One module drawn as a card: a header with its kind and name, tabs built from [Tab] attributes, and its
    /// fields bound through the item's SerializedObject so undo and multi-step edits work.
    /// </summary>
    public sealed class ModuleCard : VisualElement
    {
        private static readonly Dictionary<Type, string> SelectedTab = new();

        public ModuleCard(SerializedProperty moduleProperty, ModuleData module, Action<GenericMenu> fillMenu)
        {
            AddToClassList("mcsdk-card");
            var info = ModuleRegistry.Get(module);
            var displayName = info?.DisplayName ?? module.GetType().Name;

            var header = new VisualElement();
            header.AddToClassList("mcsdk-card__header");
            var kind = new Label(module.Kind.ToString().ToUpperInvariant());
            kind.AddToClassList("mcsdk-card__kind");
            if (module.Kind == ModuleKind.Behaviour) kind.AddToClassList("mcsdk-card__kind--behaviour");
            if (module.Kind == ModuleKind.Modification) kind.AddToClassList("mcsdk-card__kind--modification");
            header.Add(kind);
            var title = new Label(displayName) { tooltip = info?.Description };
            title.AddToClassList("mcsdk-card__title");
            header.Add(title);
            var menuButton = new Button { text = "⋮", tooltip = "Module options" };
            menuButton.AddToClassList("mcsdk-card__menu");
            menuButton.clicked += () =>
            {
                var menu = new GenericMenu();
                fillMenu(menu);
                menu.DropDown(menuButton.worldBound);
            };
            header.Add(menuButton);
            Add(header);

            var body = new VisualElement();
            body.AddToClassList("mcsdk-card__body");
            Add(body);

            var groups = GroupFields(module.GetType(), displayName);
            if (groups.Count == 0)
            {
                var empty = new Label("This module has no settings.");
                empty.AddToClassList("mcsdk-empty");
                body.Add(empty);
                return;
            }

            var tabs = new VisualElement();
            tabs.AddToClassList("mcsdk-tabs");
            var pages = new VisualElement();
            if (groups.Count > 1) body.Add(tabs);
            body.Add(pages);

            var buttons = new Dictionary<string, Button>();
            var pageByTab = new Dictionary<string, VisualElement>();
            foreach (var (tab, fields) in groups)
            {
                var page = new VisualElement();
                foreach (var field in fields)
                {
                    var property = moduleProperty.FindPropertyRelative(field.Name);
                    if (property == null) continue;
                    var propertyField = new PropertyField(property, LabelFor(field));
                    propertyField.BindProperty(property);
                    page.Add(propertyField);
                }

                pages.Add(page);
                pageByTab[tab] = page;

                var tabButton = new Button { text = tab };
                tabButton.AddToClassList("mcsdk-tab");
                tabButton.clicked += () => Show(tab);
                tabs.Add(tabButton);
                buttons[tab] = tabButton;
            }

            void Show(string tab)
            {
                SelectedTab[module.GetType()] = tab;
                foreach (var (name, page) in pageByTab) page.style.display = name == tab ? DisplayStyle.Flex : DisplayStyle.None;
                foreach (var (name, button) in buttons) button.EnableInClassList("mcsdk-tab--selected", name == tab);
            }

            Show(SelectedTab.TryGetValue(module.GetType(), out var last) && pageByTab.ContainsKey(last) ? last : groups[0].tab);
        }

        /// <summary>
        /// Fields grouped by tab: the module's own (untagged) fields first under its name, then [Tab] groups in
        /// the order they first appear.
        /// </summary>
        public static List<(string tab, List<FieldInfo> fields)> GroupFields(Type moduleType, string ownTab)
        {
            var groups = new List<(string tab, List<FieldInfo> fields)>();
            foreach (var field in ModuleSerializer.GetExportedFields(moduleType))
            {
                var tab = field.GetCustomAttribute<TabAttribute>()?.Name ?? ownTab;
                var index = groups.FindIndex(g => g.tab == tab);
                if (index < 0)
                {
                    groups.Add((tab, new List<FieldInfo>()));
                    index = groups.Count - 1;
                }

                groups[index].fields.Add(field);
            }

            var own = groups.FindIndex(g => g.tab == ownTab);
            if (own > 0)
            {
                var first = groups[own];
                groups.RemoveAt(own);
                groups.Insert(0, first);
            }

            return groups;
        }

        /// <summary>"WeightKG" with [Unit("kg")] → "Weight KG (kg)". Units are part of the label so they never overlap values.</summary>
        public static string LabelFor(FieldInfo field)
        {
            var label = ObjectNames.NicifyVariableName(field.Name);
            var unit = field.GetCustomAttribute<UnitAttribute>()?.Unit;
            return string.IsNullOrEmpty(unit) ? label : $"{label} ({unit})";
        }
    }
}
