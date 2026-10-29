using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModComponent.SDK.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/Workspace-Information#mod-localization-assets")]
    public class ModGearSpawns : ScriptableObject
    {
        [Serializable]
        public class SceneSpawnEntry
        {
            [Tooltip("Scene name.")]
            public DataSceneAsset sceneName;

            [Tooltip("Items to spawn in scene.")]
            public List<SceneItemSpawn> sceneItemSpawns = new();
        }

        [Serializable]
        public class LoottableSpawnEntry
        {
            [Tooltip("Loot table name.")]
            public DataLootTableAsset lootTableName;

            [Tooltip("Items in loot table.")]
            public List<LootTableSpawn> lootTableSpawns = new();
        }

        [Serializable]
        public class SceneItemSpawn
        {
            [Tooltip("Item name to spawn.")]
            public DataGearAsset itemName;

            [Tooltip("Spawn position (x,y,z).")]
            public Vector3 position;

            [Tooltip("Spawn rotation (euler angles).")]
            public Vector3 rotation;

            [Tooltip("Chance of spawning (%) 0-100.")]
            [Range(0, 100)]
            public int spawnChance;
        }

        [Serializable]
        public class LootTableSpawn
        {
            [Tooltip("Item name to spawn.")]
            public DataGearAsset itemName;

            [Tooltip("Item weight in loot table.")]
            public int weight;
        }

        [Tooltip("Scene-based item spawn configurations.")]
        public List<SceneSpawnEntry> sceneGearSpawnEntries = new();

        [Tooltip("Loot table item spawn configurations.")]
        public List<LoottableSpawnEntry> lootTableGearSpawnEntries = new();
    }
}