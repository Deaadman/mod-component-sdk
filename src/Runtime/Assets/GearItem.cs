using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Integrations;
using ModComponent.SDK.Modules;
using UnityEngine;

namespace ModComponent.SDK
{
    /// <summary>
    /// One item in a mod. Holds all of the item's data and points at a model prefab, which stays
    /// pure visuals: the build never adds or removes anything on the prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "ModComponent SDK/Gear Item", fileName = "GEAR_NewItem", order = 1)]
    public sealed class GearItem : ScriptableObject
    {
        public const string GearPrefix = "GEAR_";
        public const string InventoryIconPrefix = "ico_GearItem__";
        public const string CraftingIconPrefix = "ico_CraftItem__";

        [Tooltip("The prefab name ModComponent loads, e.g. GEAR_WoolBalaclava. Must start with GEAR_.")]
        [SerializeField] private string gearName = GearPrefix;

        [Tooltip("The item's model. Its root is the item; meshes and colliders live on children.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Inventory icon. Exported under the address ico_GearItem__<Name>.")]
        [SerializeField] private Texture2D icon;

        [Tooltip("Crafting icon, needed when a blueprint makes this item. Exported as ico_CraftItem__<Name>.")]
        [SerializeField] private Texture2D craftingIcon;

        [SerializeReference] private ComponentModule component;
        [SerializeReference] private List<BehaviourModule> behaviours = new();
        [SerializeReference] private List<ModificationModule> modifications = new();
        [SerializeReference] private List<ItemIntegrationData> integrationData = new();

        public string GearName
        {
            get => gearName;
            set => gearName = value;
        }

        /// <summary>The name without the GEAR_ prefix, used for file names and icon addresses.</summary>
        public string ShortName => gearName != null && gearName.StartsWith(GearPrefix, StringComparison.Ordinal)
            ? gearName.Substring(GearPrefix.Length)
            : gearName ?? string.Empty;

        public string InventoryIconAddress => InventoryIconPrefix + ShortName;
        public string CraftingIconAddress => CraftingIconPrefix + ShortName;

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public Texture2D Icon
        {
            get => icon;
            set => icon = value;
        }

        public Texture2D CraftingIcon
        {
            get => craftingIcon;
            set => craftingIcon = value;
        }

        public ComponentModule Component
        {
            get => component;
            set => component = value;
        }

        public IReadOnlyList<BehaviourModule> Behaviours => behaviours;
        public IReadOnlyList<ModificationModule> Modifications => modifications;
        public IReadOnlyList<ItemIntegrationData> IntegrationData => integrationData;

        /// <summary>Every module on the item: the component first, then behaviours, then modifications.</summary>
        public IEnumerable<ModuleData> AllModules
        {
            get
            {
                if (component != null) yield return component;
                foreach (var b in behaviours.Where(b => b != null)) yield return b;
                foreach (var m in modifications.Where(m => m != null)) yield return m;
            }
        }

        public T GetModule<T>() where T : ModuleData => AllModules.OfType<T>().FirstOrDefault();

        /// <summary>
        /// Adds a module of the matching kind. A component replaces the existing one;
        /// a behaviour or modification already on the item is not added twice.
        /// </summary>
        public T AddModule<T>(T module) where T : ModuleData
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            switch (module)
            {
                case ComponentModule c:
                    component = c;
                    break;
                case BehaviourModule b:
                    if (behaviours.Any(x => x != null && x.GetType() == b.GetType()))
                        throw new InvalidOperationException($"{gearName} already has a {b.GetType().Name}.");
                    var conflict = behaviours.FirstOrDefault(x => x != null && Conflict(x.GetType(), b.GetType()));
                    if (conflict != null)
                        throw new InvalidOperationException(
                            $"{b.GetType().Name} conflicts with {conflict.GetType().Name} on {gearName}; ModComponent only loads one of them.");
                    behaviours.Add(b);
                    break;
                case ModificationModule m:
                    if (modifications.Any(x => x != null && x.GetType() == m.GetType()))
                        throw new InvalidOperationException($"{gearName} already has a {m.GetType().Name}.");
                    modifications.Add(m);
                    break;
                default:
                    throw new ArgumentException($"Unknown module kind {module.GetType().Name}.", nameof(module));
            }

            return module;
        }

        private static bool Conflict(Type a, Type b) =>
            ConflictsOf(a).Contains(b) || ConflictsOf(b).Contains(a);

        private static Type[] ConflictsOf(Type type) =>
            ((ConflictsWithAttribute)Attribute.GetCustomAttribute(type, typeof(ConflictsWithAttribute), false))?.Modules
            ?? Array.Empty<Type>();

        public bool RemoveModule(ModuleData module)
        {
            switch (module)
            {
                case ComponentModule c when ReferenceEquals(c, component):
                    component = null;
                    return true;
                case BehaviourModule b:
                    return behaviours.Remove(b);
                case ModificationModule m:
                    return modifications.Remove(m);
                default:
                    return false;
            }
        }

        public T GetIntegrationData<T>() where T : ItemIntegrationData =>
            integrationData.OfType<T>().FirstOrDefault();

        public T GetOrAddIntegrationData<T>() where T : ItemIntegrationData, new()
        {
            var data = GetIntegrationData<T>();
            if (data != null) return data;
            data = new T();
            integrationData.Add(data);
            return data;
        }

        public bool RemoveIntegrationData(ItemIntegrationData data) => integrationData.Remove(data);
    }
}
