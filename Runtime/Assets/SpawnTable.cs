using System;
using System.Collections.Generic;
using ModComponent.SDK.References;
using UnityEngine;

namespace ModComponent.SDK
{
    /// <summary>Where a mod's items appear: fixed spots in scenes, and entries in loot tables.</summary>
    [CreateAssetMenu(menuName = "ModComponent SDK/Spawn Table", fileName = "Spawns", order = 4)]
    public sealed class SpawnTable : ScriptableObject
    {
        // Entries are classes rather than structs so new rows start with usable values (chance 100, weight 1)
        // instead of zero, which ModComponent would accept and silently never spawn.
        [Serializable]
        public sealed class SceneSpawn
        {
            public GearRef Item;
            public Vector3 Position;

            [Tooltip("Euler angles.")]
            public Vector3 Rotation;

            [Range(0, 100), Tooltip("Spawn chance in percent, scaled by the game's difficulty settings unless 100.")]
            public float Chance = 100f;
        }

        [Serializable]
        public sealed class SceneGroup
        {
            public SceneRef Scene;

            [Tooltip("Optional spawn tag. Leave empty for the default.")]
            public string Tag = string.Empty;

            public List<SceneSpawn> Spawns = new();
        }

        [Serializable]
        public sealed class LootEntry
        {
            public GearRef Item;

            [Min(1)] public int Weight = 1;
        }

        [Serializable]
        public sealed class LootTableGroup
        {
            public LootTableRef LootTable;
            public List<LootEntry> Entries = new();
        }

        [SerializeField] private List<SceneGroup> scenes = new();
        [SerializeField] private List<LootTableGroup> lootTables = new();

        public List<SceneGroup> Scenes => scenes;
        public List<LootTableGroup> LootTables => lootTables;

        public bool IsEmpty => scenes.Count == 0 && lootTables.Count == 0;
    }
}
