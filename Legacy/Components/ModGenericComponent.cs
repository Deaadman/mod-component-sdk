using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modgenericcomponent")]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class ModGenericComponent : MonoBehaviour
    {
        [Tooltip("In-game name key.")]
        public string DisplayNameLocalizationId = "GAMEPLAY_NameKey";

        [Tooltip("In-game description key.")]
        public string DescriptionLocalizatonId = "GAMEPLAY_DescriptionKey";

        [Tooltip("Inventory action text key. Defaults if empty.")]
        public string InventoryActionLocalizationId;

        [Tooltip("Weight (kg).")]
        public float WeightKG = 1;

        [Tooltip("Time to decay (days); 0 for non-decaying.")]
        public int DaysToDecay;

        [Tooltip("Maximum hit points.")]
        public float MaxHP = 100;

        [Tooltip("Initial condition on discovery or creation.")]
        public InitialCondition InitialCondition;

        [Tooltip("Inventory category. Auto for default.")]
        public InventoryCategory InventoryCategory;

        [Tooltip("Pickup sound effect.")]
        public DataSoundAsset PickUpAudio;

        [Tooltip("Drop sound effect.")]
        public DataSoundAsset PutBackAudio;

        [Tooltip("Holster sound effect.")]
        public DataSoundAsset StowAudio;

        [Tooltip("Worn-out sound effect.")]
        public DataSoundAsset WornOutAudio;

        [Tooltip("Inspect upon pickup?")]
        public bool InspectOnPickup = true;

        [Tooltip("Inspect camera distance.")]
        public float InspectDistance = 1;

        [Tooltip("Inspect rotation angles.")]
        public Vector3 InspectAngles;

        [Tooltip("Inspect offset.")]
        public Vector3 InspectOffset;

        [Tooltip("Inspect scale.")]
        public Vector3 InspectScale = new(1, 1, 1);

        [Tooltip("Model for normal view.")]
        public GameObject NormalModel;

        [Tooltip("Model for inspect view.")]
        public GameObject InspectModel;
    }
}