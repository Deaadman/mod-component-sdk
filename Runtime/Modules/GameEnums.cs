// Enum names must match the game's enums: ModComponent parses them by name (case-insensitive).
namespace ModComponent.SDK.Modules
{
    /// <summary>Game: GearStartCondition.</summary>
    public enum InitialCondition { Random, Perfect, High, Medium, Low }

    /// <summary>ModComponent: ModBaseComponent.ItemCategory.</summary>
    public enum InventoryCategory { Auto, Clothing, FirstAid, Firestarting, Food, Material, Tool }

    /// <summary>Game: GunType.</summary>
    public enum GunType { Rifle, Revolver, FlareGun }

    /// <summary>ModComponent: ModClothingComponent.BodyRegion.</summary>
    public enum ClothingRegion { Head, Hands, Chest, Legs, Feet, Accessory }

    /// <summary>Game: ClothingLayer.</summary>
    public enum ClothingLayer { Base, Mid, Top, Top2 }

    /// <summary>ModComponent: ModClothingComponent.MovementSounds.</summary>
    public enum ClothingMovementSound { None, HeavyNylon, LeatherHide, LightCotton, LightNylon, SoftCloth, Wool }

    /// <summary>Game: DamageReason.</summary>
    public enum DamageReason
    {
        Unspecified, Player, Wolf, Bear, Falling, Burns, IceCracking, Electrical, BloodLoss, CabinFever,
        Dysentery, Dehydrated, EmergencyStim, Exhausted, FirstAid, FoodPoisoning, Freezing, FrostBite,
        Hypothermia, Infection, IntestinalParasites, Sleeping, Starving, WillPower, Anxiety, Fear, ToxicFog,
        Suffocating, BulletWound, NoiseMaker, Scurvy, Cougar, WeakJoints
    }

    /// <summary>Game: NGUIText.Alignment.</summary>
    public enum TextAlignment { Automatic, Left, Center, Right, Justified }

    /// <summary>Game: CookableType.</summary>
    public enum CookableType { Grub, Liquid, Meat }

    /// <summary>ModComponent: ModFirstAidComponent.FirstAidKind.</summary>
    public enum FirstAidType { Antibiotics, Bandage, Disinfectant, PainKiller }

    /// <summary>ModComponent: ModLiquidComponent.LiquidKind.</summary>
    public enum LiquidType { Water, Kerosene }

    /// <summary>Game: SkillType.</summary>
    public enum SkillType
    {
        None, Firestarting, CarcassHarvesting, IceFishing, Cooking, Rifle, Archery, ClothingRepair,
        ToolRepair, Revolver, Gunsmithing
    }

    /// <summary>Game: ToolsItem.CuttingToolType.</summary>
    public enum CuttingToolType { None, Hacksaw, Hatchet, Hammer, Knife }

    /// <summary>Game: ToolsItem.ToolType.</summary>
    public enum ToolUsage { All, CraftOnly, RepairOnly }

    /// <summary>Game: ScentRangeCategory.</summary>
    public enum ScentCategory { RAW_MEAT, RAW_FISH, COOKED_MEAT, COOKED_FISH, GUTS, QUARTER }

    /// <summary>Game: CraftingLocation.</summary>
    public enum CraftingLocation { Anywhere, Workbench, Forge, AmmoWorkbench }

    /// <summary>CraftingRevisions: how a required gear amount is measured.</summary>
    public enum RequirementUnits { Count, Kilograms }
}
