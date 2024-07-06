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
    }
}