using System.Linq;
using ModComponent.SDK.Editor.Integrations;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Editor.UI.Preview;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Modules;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Inspectors
{
    /// <summary>
    /// The item inspector: identity and visuals, one card per module, an "Add module" menu that greys out
    /// conflicts, and the item's validation issues with fixes.
    /// </summary>
    [CustomEditor(typeof(GearItem))]
    public sealed class GearItemEditor : UnityEditor.Editor
    {
        private VisualElement _root;
        private VisualElement _modules;
        private VisualElement _issues;
        private IVisualElementScheduledItem _pendingValidation;
        private ItemPreview _preview;
        private GameObject _previewPrefab;
        private Vector2 _dragAngles;

        private GearItem Item => (GearItem)target;

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement().WithStyles();

            _root.Add(BuildHeader());

            var identity = SdkUI.Section("Item", out var identityContent);
            identityContent.Add(new PropertyField(serializedObject.FindProperty("gearName"), "Gear name"));
            identityContent.Add(new PropertyField(serializedObject.FindProperty("prefab"), "Model prefab"));
            identityContent.Add(new PropertyField(serializedObject.FindProperty("icon"), "Inventory icon"));
            identityContent.Add(new PropertyField(serializedObject.FindProperty("craftingIcon"), "Crafting icon"));
            _root.Add(identity);
            _root.Add(BuildPreviewSection());

            _modules = new VisualElement();
            _root.Add(_modules);

            var addButton = new Button { text = "＋ Add module" };
            addButton.clicked += () => new AddModuleDropdown(Item, AddModule).Show(addButton.worldBound);
            _root.Add(addButton);

            var issues = SdkUI.Section("Issues", out var issuesContent);
            _issues = issuesContent;
            _root.Add(issues);

            RebuildModules();
            Revalidate();

            _root.TrackSerializedObjectValue(serializedObject, _ => ScheduleValidation());
            _root.Bind(serializedObject);
            return _root;
        }

        private void OnDisable()
        {
            _preview?.Dispose();
            _preview = null;
        }

        /// <summary>
        /// A live, approximate inspect view: drag to turn the model, then "Use current view" to store the angles.
        /// "Render icon" renders an inventory icon from the model.
        /// </summary>
        private VisualElement BuildPreviewSection()
        {
            var section = SdkUI.Section("Inspect preview", out var content);
            if (!ItemPreview.CanRender)
            {
                content.Add(new HelpBox("Previews need a graphics device.", HelpBoxMessageType.Info));
                return section;
            }

            IMGUIContainer view = null;
            view = new IMGUIContainer(() =>
            {
                if (target == null) return;
                var component = Item.Component;
                var rect = GUILayoutUtility.GetRect(10, 10000, 180, 180);
                if (Item.Prefab == null || component == null)
                {
                    EditorGUI.HelpBox(rect, "Assign a prefab and a component to preview the inspect view.", MessageType.None);
                    return;
                }

                if (_preview == null || _previewPrefab != Item.Prefab)
                {
                    _preview?.Dispose();
                    _preview = new ItemPreview(Item.Prefab);
                    _previewPrefab = Item.Prefab;
                }

                var e = Event.current;
                if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
                {
                    _dragAngles += new Vector2(e.delta.y, -e.delta.x) * 0.5f;
                    e.Use();
                    view.MarkDirtyRepaint();
                }

                _preview.PoseForInspect(component, component.InspectAngles + new Vector3(_dragAngles.x, _dragAngles.y, 0));
                _preview.Draw(rect);
            });
            view.RegisterCallback<MouseMoveEvent>(_ => view.MarkDirtyRepaint());
            content.Add(view);

            var hint = new Label("Drag to turn the model. The framing approximates the game's inspect camera.") { style = { opacity = 0.65f, whiteSpace = WhiteSpace.Normal } };
            content.Add(hint);
            content.Add(SdkUI.Row(
                new Button(() =>
                {
                    if (Item.Component == null) return;
                    Undo.RecordObject(Item, "Use current view");
                    Item.Component.InspectAngles += new Vector3(_dragAngles.x, _dragAngles.y, 0);
                    _dragAngles = Vector2.zero;
                    Changed();
                }) { text = "Use current view", tooltip = "Store the dragged rotation in Inspect angles" },
                new Button(() =>
                {
                    _dragAngles = Vector2.zero;
                    view.MarkDirtyRepaint();
                }) { text = "Reset view" },
                new Button(() =>
                {
                    try
                    {
                        IconRenderer.RenderAndAssign(Item);
                        Changed();
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        EditorUtility.DisplayDialog("Render icon", ex.Message, "OK");
                    }
                }) { text = "Render icon", tooltip = "Render the model to ico_GearItem__Name.png and use it as the inventory icon" },
                new Button(() =>
                {
                    try
                    {
                        IconRenderer.RenderAndAssign(Item, crafting: true);
                        Changed();
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        EditorUtility.DisplayDialog("Render crafting icon", ex.Message, "OK");
                    }
                }) { text = "Render crafting icon", tooltip = "Render ico_CraftItem__Name.png, needed when a blueprint makes this item" }));
            content.TrackSerializedObjectValue(serializedObject, _ => view.MarkDirtyRepaint());
            return section;
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("mcsdk-header");
            var thumb = new VisualElement();
            thumb.AddToClassList("mcsdk-header__thumb");
            header.Add(thumb);
            var text = new VisualElement();
            var title = new Label();
            title.AddToClassList("mcsdk-header__title");
            var subtitle = new Label();
            subtitle.AddToClassList("mcsdk-header__subtitle");
            text.Add(title);
            text.Add(subtitle);
            header.Add(text);

            void Refresh()
            {
                title.text = string.IsNullOrEmpty(Item.GearName) ? "(no gear name)" : Item.GearName;
                var mods = SdkUI.OwningMods(Item).Select(m => m.DisplayName).ToList();
                var component = ModuleRegistry.Get(Item.Component)?.DisplayName ?? "No component";
                subtitle.text = mods.Count > 0 ? $"{component} · {string.Join(", ", mods)}" : $"{component} · not in any mod";
                var image = Item.Icon != null ? Item.Icon : Item.Prefab != null ? AssetPreview.GetAssetPreview(Item.Prefab) : null;
                thumb.style.backgroundImage = image;
            }

            Refresh();
            header.schedule.Execute(Refresh).Every(1000);
            return header;
        }

        private void RebuildModules()
        {
            serializedObject.Update();
            _modules.Clear();

            var componentProperty = serializedObject.FindProperty("component");
            if (Item.Component == null)
            {
                var missing = new HelpBox("Every item needs exactly one component. Use \"Add module\" below.", HelpBoxMessageType.Warning);
                _modules.Add(missing);
            }
            else
            {
                _modules.Add(new ModuleCard(componentProperty, Item.Component, menu => FillMenu(menu, Item.Component, -1)));
            }

            AddList("behaviours", Item.Behaviours.Cast<ModuleData>().ToList());
            AddList("modifications", Item.Modifications.Cast<ModuleData>().ToList());

            var enabled = SdkUI.OwningMods(Item).SelectMany(m => m.Integrations).Where(i => i != null)
                .Select(i => i.GetType()).ToHashSet();
            foreach (var integration in IntegrationRegistry.All.Where(i => enabled.Contains(i.Type)))
            foreach (var data in integration.ItemData)
                _modules.Add(BuildIntegrationSection(integration, data));

            _modules.Bind(serializedObject);
        }

        private void AddList(string propertyName, System.Collections.Generic.List<ModuleData> modules)
        {
            var list = serializedObject.FindProperty(propertyName);
            for (var i = 0; i < modules.Count && i < list.arraySize; i++)
            {
                if (modules[i] == null) continue;
                var index = i;
                var module = modules[i];
                _modules.Add(new ModuleCard(list.GetArrayElementAtIndex(i), module, menu => FillMenu(menu, module, index)));
            }
        }

        /// <summary>
        /// One integration's data on this item: a toggle that adds or removes it, and its fields while it's there.
        /// </summary>
        private VisualElement BuildIntegrationSection(ModIntegrationInfo integration, ItemDataInfo data)
        {
            var section = SdkUI.Section(integration.DisplayName, out var content);
            var toggle = new Toggle(data.Label) { value = Item.GetIntegrationData(data.Type) != null, tooltip = integration.Description };
            toggle.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(Item, "Change " + integration.DisplayName);
                if (e.newValue) Item.GetOrAddIntegrationData(data.Type);
                else Item.RemoveIntegrationData(Item.GetIntegrationData(data.Type));
                Changed();
            });
            content.Add(toggle);

            var list = serializedObject.FindProperty("integrationData");
            var index = Item.IntegrationData.ToList().IndexOf(Item.GetIntegrationData(data.Type));
            if (index >= 0 && index < list.arraySize) content.Add(SdkUI.ChildFields(list.GetArrayElementAtIndex(index)));
            return section;
        }

        private void FillMenu(GenericMenu menu, ModuleData module, int index)
        {
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                Undo.RecordObject(Item, "Remove module");
                Item.RemoveModule(module);
                Changed();
            });
            menu.AddItem(new GUIContent("Reset to defaults"), false, () =>
            {
                Undo.RecordObject(Item, "Reset module");
                var fresh = ModuleRegistry.Get(module).CreateInstance();
                if (module is ComponentModule) Item.Component = (ComponentModule)fresh;
                else
                {
                    Item.RemoveModule(module);
                    Item.AddModule(fresh);
                }

                Changed();
            });

            if (module is ComponentModule current)
            {
                foreach (var info in ModuleRegistry.OfKind(ModuleKind.Component).Where(i => i.Type != module.GetType()))
                    menu.AddItem(new GUIContent($"Change to/{info.Category}/{info.DisplayName}"), false, () => ChangeComponent(current, info));
            }
        }

        /// <summary>
        /// Swaps the component, keeping every field the two types share (names, weight, audio, inspect, and
        /// equip settings between equippable types).
        /// </summary>
        private void ChangeComponent(ComponentModule current, ModuleInfo target)
        {
            Undo.RecordObject(Item, "Change component");
            var replacement = (ComponentModule)target.CreateInstance();
            foreach (var field in Build.Export.ModuleSerializer.GetExportedFields(current.GetType()))
                if (field.DeclaringType!.IsInstanceOfType(replacement))
                    field.SetValue(replacement, field.GetValue(current));
            Item.Component = replacement;
            Changed();
        }

        private void AddModule(ModuleInfo info)
        {
            Undo.RecordObject(Item, "Add " + info.DisplayName);
            if (info.Kind == ModuleKind.Component && Item.Component != null)
            {
                ChangeComponent(Item.Component, info);
                return;
            }

            Item.AddModule(info.CreateInstance());
            Changed();
        }

        private void Changed()
        {
            EditorUtility.SetDirty(Item);
            serializedObject.Update();
            RebuildModules();
            Revalidate();
        }

        private void ScheduleValidation()
        {
            _pendingValidation?.Pause();
            _pendingValidation = _root.schedule.Execute(Revalidate).StartingIn(400);
        }

        private void Revalidate()
        {
            if (_issues == null || Item == null) return;
            _issues.Clear();
            var mods = SdkUI.OwningMods(Item).ToList();
            if (mods.Count == 0)
            {
                _issues.Add(new HelpBox("This item isn't in any mod yet, so it won't be built. Add it to a mod's item list.", HelpBoxMessageType.Info));
                return;
            }

            var issues = mods.SelectMany(m => ModValidator.Validate(m).For(Item)).GroupBy(i => i.ToString()).Select(g => g.First());
            _issues.Add(SdkUI.IssueList(issues, Changed, showTarget: false));
        }
    }

    /// <summary>
    /// The "Add module" menu: every module grouped by kind and category. Ones that can't be added show
    /// greyed out with the reason.
    /// </summary>
    public sealed class AddModuleDropdown : AdvancedDropdown
    {
        private readonly GearItem _item;
        private readonly System.Action<ModuleInfo> _add;

        public AddModuleDropdown(GearItem item, System.Action<ModuleInfo> add) : base(new AdvancedDropdownState())
        {
            _item = item;
            _add = add;
            minimumSize = new Vector2(300, 340);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Add module");
            foreach (var kind in new[] { ModuleKind.Component, ModuleKind.Behaviour, ModuleKind.Modification })
            {
                var kindItem = new AdvancedDropdownItem(kind + (kind == ModuleKind.Component && _item.Component != null ? " (replaces current)" : "s"));
                root.AddChild(kindItem);
                foreach (var info in ModuleRegistry.OfKind(kind))
                {
                    var reason = ModuleRegistry.GetBlockReason(info, _item.AllModules);
                    var entry = new ModuleItem(reason == null ? info.DisplayName : $"{info.DisplayName} ({reason})", info)
                    {
                        enabled = reason == null && info.Type != _item.Component?.GetType()
                    };
                    kindItem.AddChild(entry);
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is ModuleItem module) _add(module.Info);
        }

        private sealed class ModuleItem : AdvancedDropdownItem
        {
            public ModuleItem(string name, ModuleInfo info) : base(name) => Info = info;
            public ModuleInfo Info { get; }
        }
    }
}
