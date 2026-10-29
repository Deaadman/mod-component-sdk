using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Integrations;
using UnityEngine;

namespace ModComponent.SDK
{
    /// <summary>
    /// A mod: one .modcomponent file. Owns the item list, blueprints, localization and spawns.
    /// Every field can be changed at any time, including the name.
    /// </summary>
    [CreateAssetMenu(menuName = "ModComponent SDK/Mod", fileName = "NewMod", order = 0)]
    public sealed class Mod : ScriptableObject
    {
        [Tooltip("Name shown to players, and written to buildinfo.json.")]
        [SerializeField] private string displayName = "New Mod";

        [Tooltip("File-safe id. Used as the .modcomponent file name and in build paths.")]
        [SerializeField] private string id = "NewMod";

        [SerializeField] private string author = string.Empty;

        [Tooltip("Semantic version, e.g. 1.2.0.")]
        [SerializeField] private string version = "1.0.0";

        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;

        [Tooltip(".modcomponent files (without extension) this mod needs, e.g. ModdersGearToolbox.")]
        [SerializeField] private List<string> requiredMods = new();

        [Tooltip("Requires the Tales from the Far Territory DLC.")]
        [SerializeField] private bool requiresDLC;

        [SerializeField] private List<GearItem> items = new();
        [SerializeField] private List<Blueprint> blueprints = new();
        [SerializeField] private LocalizationTable localization;
        [SerializeField] private SpawnTable spawns;

        [Tooltip("Other assets to put in the bundle, addressed by their asset name: paper-doll textures, " +
                 "first-person clothing prefabs, equipped models and so on. Item prefabs and icons are added automatically.")]
        [SerializeField] private List<UnityEngine.Object> bundledAssets = new();

        [Tooltip("Extra files copied into the archive as-is, such as Wwise .bnk sound banks.")]
        [SerializeField] private List<UnityEngine.Object> extraFiles = new();

        [SerializeReference] private List<ModIntegration> integrations = new();

        public string DisplayName { get => displayName; set => displayName = value; }
        public string Id { get => id; set => id = value; }
        public string Author { get => author; set => author = value; }
        public string Version { get => version; set => version = value; }
        public string Description { get => description; set => description = value; }
        public bool RequiresDLC { get => requiresDLC; set => requiresDLC = value; }
        public LocalizationTable Localization { get => localization; set => localization = value; }
        public SpawnTable Spawns { get => spawns; set => spawns = value; }

        public List<string> RequiredMods => requiredMods;
        public List<GearItem> Items => items;
        public List<Blueprint> Blueprints => blueprints;
        public List<UnityEngine.Object> BundledAssets => bundledAssets;
        public List<UnityEngine.Object> ExtraFiles => extraFiles;
        public IReadOnlyList<ModIntegration> Integrations => integrations;

        public T GetIntegration<T>() where T : ModIntegration => integrations.OfType<T>().FirstOrDefault();

        public ModIntegration GetIntegration(Type type) => integrations.FirstOrDefault(i => i != null && i.GetType() == type);

        public bool HasIntegration<T>() where T : ModIntegration => GetIntegration<T>() != null;

        public bool HasIntegration(Type type) => GetIntegration(type) != null;

        public T EnableIntegration<T>() where T : ModIntegration, new() => (T)EnableIntegration(typeof(T));

        /// <summary>Adds an integration of <paramref name="type"/>, or returns the one already enabled.</summary>
        public ModIntegration EnableIntegration(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(ModIntegration).IsAssignableFrom(type))
                throw new ArgumentException($"{type?.Name ?? "null"} isn't a ModIntegration type.", nameof(type));
            var existing = GetIntegration(type);
            if (existing != null) return existing;
            var integration = (ModIntegration)Activator.CreateInstance(type);
            integrations.Add(integration);
            return integration;
        }

        public bool DisableIntegration<T>() where T : ModIntegration => DisableIntegration(typeof(T));

        public bool DisableIntegration(Type type) =>
            integrations.RemoveAll(i => i != null && i.GetType() == type) > 0;

        /// <summary>Turns a display name into an id: ASCII letters, digits, '-' and '_' only.</summary>
        public static string ToId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var chars = name.Trim().Where(IsIdChar).ToArray();
            return new string(chars);
        }

        public static bool IsValidId(string value) => !string.IsNullOrEmpty(value) && value.All(IsIdChar);

        private static bool IsIdChar(char c) =>
            c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_';

        public GearItem FindItem(string gearName) =>
            items.FirstOrDefault(i => i != null && string.Equals(i.GearName, gearName, StringComparison.Ordinal));
    }
}
