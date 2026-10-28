using System;
using ModComponent.SDK.References;
using UnityEngine;

namespace ModComponent.SDK.Modules
{
    /// <summary>
    /// Base of every module stored on a <see cref="GearItem"/>. Modules are plain serializable classes held
    /// through [SerializeReference]; each public field is written to the item's JSON under its own name.
    /// </summary>
    [Serializable]
    public abstract class ModuleData
    {
        public abstract ModuleKind Kind { get; }
    }

    /// <summary>
    /// The one component that defines what an item is. Fields here are ModComponent's ModBaseComponent,
    /// which every component inherits.
    /// </summary>
    [Serializable]
    public abstract class ComponentModule : ModuleData
    {
        public sealed override ModuleKind Kind => ModuleKind.Component;

        [Tab("General"), Tooltip("Localization key of the in-game name.")]
        public LocalizationKey DisplayNameLocalizationId;

        [Tab("General"), Tooltip("Localization key of the in-game description."), ExportName("DescriptionLocalizatonId")]
        public LocalizationKey DescriptionLocalizationId;

        [Tab("General"), Tooltip("Localization key of the inventory action button. Leave empty for the default.")]
        public LocalizationKey InventoryActionLocalizationId;

        [Tab("General"), Unit("kg"), Min(0), Tooltip("Weight in kilograms.")]
        public float WeightKG = 1f;

        [Tab("General"), Unit("days"), Min(0), Tooltip("Days until fully decayed. 0 never decays.")]
        public float DaysToDecay;

        [Tab("General"), Unit("HP"), Min(0), Tooltip("Maximum hit points.")]
        public float MaxHP = 100f;

        [Tab("General"), Tooltip("Condition when found or crafted.")]
        public InitialCondition InitialCondition;

        [Tab("General"), Tooltip("Inventory tab the item appears under. Auto picks one from the component type.")]
        public InventoryCategory InventoryCategory;

        [Tab("Audio"), Tooltip("Played when picking the item up.")]
        public SoundRef PickUpAudio;

        [Tab("Audio"), Tooltip("Played when putting the item down.")]
        public SoundRef PutBackAudio;

        [Tab("Audio"), Tooltip("Played when the item is stowed.")]
        public SoundRef StowAudio = "Play_InventoryStow";

        [Tab("Audio"), Tooltip("Played when the item wears out.")]
        public SoundRef WornOutAudio;

        [Tab("Inspect"), Tooltip("Open the inspect view when the item is picked up.")]
        public bool InspectOnPickup = true;

        [Tab("Inspect"), Unit("m"), Min(0), Tooltip("Camera distance in the inspect view.")]
        public float InspectDistance = 0.4f;

        [Tab("Inspect"), Tooltip("Rotation in the inspect view (Euler angles).")]
        public Vector3 InspectAngles;

        [Tab("Inspect"), Tooltip("Offset in the inspect view.")]
        public Vector3 InspectOffset;

        [Tab("Inspect"), Tooltip("Scale in the inspect view. ModComponent treats a missing value as zero, so it is always written.")]
        public Vector3 InspectScale = Vector3.one;

        [Tab("Inspect"), Tooltip("Direct child shown in the world. Set both models to use a separate inspect model.")]
        public ChildRef NormalModel;

        [Tab("Inspect"), Tooltip("Direct child shown in the inspect view.")]
        public ChildRef InspectModel;
    }

    /// <summary>A component that can be held in the player's hands. ModComponent: ModBaseEquippableComponent.</summary>
    [Serializable]
    public abstract class EquippableComponentModule : ComponentModule
    {
        [Tab("Equip"), Tooltip("Addressables name of the prefab shown in first person while equipped.")]
        public string EquippedModelPrefab = string.Empty;

        [Tab("Equip"), Tooltip("Fully-qualified type name of a custom implementation from a code mod. Usually empty.")]
        public string ImplementationType = string.Empty;

        [Tab("Equip"), Tooltip("Played when the item is equipped.")]
        public SoundRef EquippingAudio;
    }

    /// <summary>Optional extra behaviour layered on top of the item's component.</summary>
    [Serializable]
    public abstract class BehaviourModule : ModuleData
    {
        public sealed override ModuleKind Kind => ModuleKind.Behaviour;
    }

    /// <summary>A change ModComponent applies to the prefab itself, such as its layer.</summary>
    [Serializable]
    public abstract class ModificationModule : ModuleData
    {
        public sealed override ModuleKind Kind => ModuleKind.Modification;
    }

    /// <summary>A gear item and a whole-number amount. Used by modules that export two parallel arrays.</summary>
    [Serializable]
    public sealed class GearCount
    {
        public GearRef Item;
        [Min(1)] public int Count = 1;

        public GearCount() { }

        public GearCount(GearRef item, int count)
        {
            Item = item;
            Count = count;
        }
    }
}
