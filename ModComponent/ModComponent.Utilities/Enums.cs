namespace ModComponent.Utilities
{
    public enum AmmoForGunType
    {
        FlareGun,
        Revolver,
        Rifle
    }

    public enum ClothingLayer
    {
        Base,
        Mid,
        Top,
        Top2
    }

    public enum ClothingMovementSound
    {
        None,
        HeavyNylon,
        LeatherHide,
        LightCotton,
        LightNylon,
        SoftCloth,
        Wool
    }

    public enum ClothingRegion
    {
        Accessory,
        Chest,
        Feet,
        Hands,
        Head,
        Legs
    }

    public enum CookableType
    {
        Grub,
        Liquid,
        Meat
    }

    public enum DamageSource
    {
        Unspecified,
        Player,
        Wolf,
        Bear,
        Falling,
        Burns,
        IceCracking,
        Electrical,
        BloodLoss,
        CabinFever,
        Dysentery,
        Dehydrated,
        EmergencyStim,
        Exhausted,
        FirstAid,
        FoodPoisoning,
        Freezing,
        FrostBite,
        Hypothermia,
        Infection,
        IntestinalParasites,
        Sleeping,
        Starving,
        WillPower,
        Anxiety,
        Fear,
        ToxicFog,
        Suffocating,
        BulletWound,
        NoiseMaker,
        Scurvy,
        Cougar,
        WeakJoints
    }
    
    public enum FirstAidType
    {
        Antibiotics,
        Bandage,
        Disinfectant,
        Painkiller
    }

    public enum FootwearType
    {
        Boots,
        Deerskin,
        None,
        Shoes
    }

    public enum InitialCondition
    {
        Random,
        Perfect,
        High,
        Medium,
        Low
    }

    public enum InventoryCategory
    {
        Auto,
        Clothing,
        FirstAid,
        Firestarting,
        Food,
        Material,
        Tool
    }

    public enum LiquidType
    {
        Kerosene,
        Water
    }

    public enum PowderType
    {
        Gunpowder
    }

    public enum ScentCategory
    {
        COOKED_FISH,
        COOKED_MEAT,
        GUTS,
        QUARTER,
        RAW_FISH,
        RAW_MEAT
    }

    // Last Updated v2.26
    public enum SkillType
    {
        None,
        Firestarting,
        CarcassHarvesting,
        IceFishing,
        Cooking,
        Rifle,
        Archery,
        ClothingRepair,
        ToolRepair,
        Revolver,
        Gunsmithing
    }

    public enum TextAlignment
    {
        Automatic,
        Center,
        Justified,
        Left,
        Right
    }

    public enum ToolKind
    {
        None,
        HackSaw,
        Hatchet,
        Hammer,
        Knife
    }

    public enum ToolUsage
    {
        All,
        CraftOnly,
        RepairOnly
    }

    public enum UnitsType
    {        
        Count,
        Kilograms
    }

    public enum WorkbenchType
    {
        Anywhere,
        Workbench,
        Forge,
        AmmoWorkbench
    }
}