using System.Collections.Generic;
using UnityEngine;

namespace ModComponent.SDK.Components
{
    [HelpURL(""), CreateAssetMenu(menuName = "ModComponent SDK/Mod Item Rarities")]
    public class ModItemRarities : ScriptableObject
    {
        public GameObject[] Common;
        public GameObject[] Uncommon;
        public GameObject[] Rare;
        public GameObject[] Epic;
        public GameObject[] Legendary;
        public GameObject[] Mythic;
        
        private void OnValidate()
        {
            ValidateUniquePrefabs();
        }

        private void ValidateUniquePrefabs()
        {
            var uniquePrefabs = new HashSet<GameObject>();

            CheckAndAddToUniquePrefabs(Common, uniquePrefabs, nameof(Common));
            CheckAndAddToUniquePrefabs(Uncommon, uniquePrefabs, nameof(Uncommon));
            CheckAndAddToUniquePrefabs(Rare, uniquePrefabs, nameof(Rare));
            CheckAndAddToUniquePrefabs(Epic, uniquePrefabs, nameof(Epic));
            CheckAndAddToUniquePrefabs(Legendary, uniquePrefabs, nameof(Legendary));
            CheckAndAddToUniquePrefabs(Mythic, uniquePrefabs, nameof(Mythic));
        }

        private static void CheckAndAddToUniquePrefabs(GameObject[] prefabs, HashSet<GameObject> uniquePrefabs, string rarityName)
        {
            if (prefabs == null) return;

            for (var i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null || uniquePrefabs.Add(prefabs[i])) continue;
                prefabs[i] = null;
            }
        }
    }
}