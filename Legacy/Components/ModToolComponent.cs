using ModComponent.SDK.Components;
using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modtoolcomponent")]
    [AddComponentMenu("")]
    public class ModToolComponent : ModGenericEquippableComponent
    {
        [Tooltip("Defines actions the tool can be used for.")]
        public ToolKind ToolType;

        [Tooltip("Specific tool type for more granular action distinction.")]
        public ToolKind ToolKind;

        [Tooltip("Condition lost per use.")]
        public float DegradeOnUse;

        [Tooltip("Usage type: crafting, repairing, or both.")]
        public ToolUsage Usage;

        [Tooltip("Skill improvement bonus when used.")]
        public int SkillBonus;

        [Tooltip("Time multiplier for crafting/repairing (%).")]
        public float CraftingTimeMultiplier;

        [Tooltip("Condition lost during crafting per hour.")]
        public float DegradePerHourCrafting;

        [Tooltip("Usable for item breakdown?")]
        public bool BreakDown;

        [Tooltip("Time multiplier for item breakdown (%).")]
        public float BreakDownTimeMultiplier;

        [Tooltip("Usable for forcing locks?")]
        public bool ForceLocks;

        [Tooltip("Sound for lock forcing.")]
        public DataSoundAsset ForceLockAudio;

        [Tooltip("Usable for clearing ice fishing holes?")]
        public bool IceFishingHole;

        [Tooltip("Condition lost clearing ice hole.")]
        public float IceFishingHoleDegradeOnUse;

        [Tooltip("Time to clear ice fishing hole (min).")]
        public int IceFishingHoleMinutes;

        [Tooltip("Sound for ice hole clearing.")]
        public DataSoundAsset IceFishingHoleAudio;

        [Tooltip("Usable for carcass harvesting?")]
        public bool CarcassHarvesting;

        [Tooltip("Time to harvest meat (min/kg).")]
        public int MinutesPerKgMeat;

        [Tooltip("Time to harvest frozen meat (min/kg).")]
        public int MinutesPerKgFrozenMeat;

        [Tooltip("Time to harvest a hide (min).")]
        public int MinutesPerHide;

        [Tooltip("Time to harvest a gut (min).")]
        public int MinutesPerGut;

        [Tooltip("Condition lost harvesting carcasses per hour.")]
        public float DegradePerHourHarvesting;

        [Tooltip("Bonus during wildlife struggle?")]
        public bool StruggleBonus;

        [Tooltip("Damage multiplier in struggle.")]
        public float DamageMultiplier;

        [Tooltip("Multiplier for animal flee chance in struggle.")]
        public float FleeChanceMultiplier;

        [Tooltip("Struggle bar fill multiplier per hit.")]
        public float TapMultiplier;

        [Tooltip("Can cause bleed out in animals?")]
        public bool CanPuncture;

        [Tooltip("Bleed out time multiplier after puncture.")]
        public float BleedoutMultiplier;
    }
}