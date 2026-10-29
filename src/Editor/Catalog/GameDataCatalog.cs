using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ModComponent.SDK.Editor.Catalog
{
    /// <summary>Kinds of names the reference pickers offer.</summary>
    public enum CatalogKind
    {
        Gear,
        Sound,
        Scene,
        LootTable,
        Liquid,
        Powder
    }

    /// <summary>
    /// Names of vanilla game data (gear, sounds, scenes, loot tables, liquids, powders) and the game's
    /// language names, read from the package's Data/catalog.json.
    /// </summary>
    public sealed class GameDataCatalog
    {
        [Serializable]
        private sealed class GearJson
        {
            public string id;
            public string source;
        }

        [Serializable]
        private sealed class CatalogJson
        {
            public string game;
            public string modComponent;
            public List<string> languages = new();
            public List<GearJson> gear = new();
            public List<string> liquids = new();
            public List<string> powders = new();
            public List<string> lootTables = new();
            public List<string> scenes = new();
            public List<string> sounds = new();
        }

        /// <summary>A gear name and where it comes from: "vanilla", or the mod that adds it.</summary>
        public readonly struct GearEntry
        {
            public GearEntry(string id, string source)
            {
                Id = id;
                Source = source;
            }

            public string Id { get; }
            public string Source { get; }
            public bool IsVanilla => Source == VanillaSource;
        }

        public const string VanillaSource = "vanilla";

        private static GameDataCatalog _default;
        private static Exception _defaultError;

        private readonly Dictionary<CatalogKind, HashSet<string>> _lookup;
        private readonly string[] _gearIds;

        private GameDataCatalog(CatalogJson json)
        {
            GameVersion = json.game ?? string.Empty;
            ModComponentVersion = json.modComponent ?? string.Empty;
            Languages = json.languages.ToArray();
            Gear = json.gear.Select(g => new GearEntry(g.id, string.IsNullOrEmpty(g.source) ? VanillaSource : g.source)).ToArray();
            Sounds = json.sounds.ToArray();
            Scenes = json.scenes.ToArray();
            LootTables = json.lootTables.ToArray();
            Liquids = json.liquids.ToArray();
            Powders = json.powders.ToArray();

            _gearIds = Gear.Select(g => g.Id).ToArray();
            _lookup = new Dictionary<CatalogKind, HashSet<string>>
            {
                [CatalogKind.Gear] = new(_gearIds, StringComparer.Ordinal),
                [CatalogKind.Sound] = new(Sounds, StringComparer.OrdinalIgnoreCase),
                [CatalogKind.Scene] = new(Scenes, StringComparer.OrdinalIgnoreCase),
                [CatalogKind.LootTable] = new(LootTables.Select(NormalizeLootTable), StringComparer.Ordinal),
                [CatalogKind.Liquid] = new(Liquids, StringComparer.Ordinal),
                [CatalogKind.Powder] = new(Powders, StringComparer.Ordinal),
            };
        }

        /// <summary>Game version the lists were taken from. May be older than <see cref="SdkInfo.TargetGameVersion"/>.</summary>
        public string GameVersion { get; }
        public string ModComponentVersion { get; }
        public IReadOnlyList<string> Languages { get; }
        public IReadOnlyList<GearEntry> Gear { get; }
        public IReadOnlyList<string> Sounds { get; }
        public IReadOnlyList<string> Scenes { get; }
        public IReadOnlyList<string> LootTables { get; }
        public IReadOnlyList<string> Liquids { get; }
        public IReadOnlyList<string> Powders { get; }

        /// <summary>
        /// The catalog shipped with the package. Loaded once per domain reload; a failed load is remembered
        /// so callers get the same error without re-reading the disk.
        /// </summary>
        public static GameDataCatalog Default
        {
            get
            {
                if (_default != null) return _default;
                if (_defaultError != null) throw _defaultError;
                try
                {
                    return _default = LoadFromFile(DefaultPath);
                }
                catch (Exception e)
                {
                    _defaultError = e;
                    throw;
                }
            }
        }

        public static string DefaultPath
        {
            get
            {
                var root = SdkInfo.PackagePath;
                if (string.IsNullOrEmpty(root))
                    throw new InvalidOperationException(
                        $"{SdkInfo.DisplayName} must be installed as the {SdkInfo.PackageName} package to find its game data catalog.");
                return Path.Combine(root, "Data", "catalog.json");
            }
        }

        public static GameDataCatalog LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Game data catalog not found at {path}.", path);
            return Parse(File.ReadAllText(path));
        }

        public static GameDataCatalog Parse(string json)
        {
            var data = JsonUtility.FromJson<CatalogJson>(json) ?? throw new FormatException("Game data catalog is empty.");
            return new GameDataCatalog(data);
        }

        /// <summary>
        /// Whether the catalog knows the name, matching the way the game resolves it: scenes and sounds
        /// case-insensitively, loot tables by GearSpawner's normalized name.
        /// </summary>
        public bool Contains(CatalogKind kind, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return _lookup[kind].Contains(kind == CatalogKind.LootTable ? NormalizeLootTable(id) : id);
        }

        /// <summary>
        /// GearSpawner's loot table name normalization: "VehicleGloveBox" and "LootTableVehicleGloveBox"
        /// are the same table.
        /// </summary>
        public static string NormalizeLootTable(string name)
        {
            var lower = name.ToLowerInvariant();
            if (lower.StartsWith("loot", StringComparison.Ordinal)) return lower;
            if (lower.StartsWith("cargo", StringComparison.Ordinal)) return "loot" + lower;
            return "loottable" + lower;
        }

        public IReadOnlyList<string> GetIds(CatalogKind kind) => kind switch
        {
            CatalogKind.Gear => _gearIds,
            CatalogKind.Sound => Sounds,
            CatalogKind.Scene => Scenes,
            CatalogKind.LootTable => LootTables,
            CatalogKind.Liquid => Liquids,
            CatalogKind.Powder => Powders,
            _ => Array.Empty<string>()
        };
    }
}
