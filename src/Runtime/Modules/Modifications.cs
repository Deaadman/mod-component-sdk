using System;
using UnityEngine;

namespace ModComponent.SDK.Modules
{
    [Serializable]
    [Module("ChangeLayer", DisplayName = "Change Layer", Category = "Prefab",
        Description = "Moves the prefab to another layer about a second after it loads.")]
    public sealed class ChangeLayerModification : ModificationModule
    {
        [Range(0, 31)] public int Layer;

        [Tooltip("Apply to every child as well.")]
        public bool Recursively;
    }
}
