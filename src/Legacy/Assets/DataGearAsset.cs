using UnityEngine;

namespace ModComponent.SDK.Components
{
    [System.Serializable]
    public class DataGearAsset : ScriptableObject
    {
        [HideInInspector]
        public string Name;

        [HideInInspector]
        public Texture2D Icon;
    }
}