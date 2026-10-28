using System;
using System.Collections.Generic;
using ModComponent.SDK.Modules;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>A starting set of modules for a new item, e.g. "Food" or "Fire starter".</summary>
    public sealed class ItemTemplate
    {
        public ItemTemplate(string name, string description, Func<ComponentModule> component, params Func<ModuleData>[] extras)
        {
            Name = name;
            Description = description;
            Component = component;
            Extras = extras;
        }

        public string Name { get; }
        public string Description { get; }
        public Func<ComponentModule> Component { get; }
        public IReadOnlyList<Func<ModuleData>> Extras { get; }

        public void Apply(GearItem item)
        {
            item.AddModule(Component());
            foreach (var extra in Extras) item.AddModule(extra());
        }
    }

    public static class ItemTemplates
    {
        public static readonly IReadOnlyList<ItemTemplate> All = new[]
        {
            new ItemTemplate("Generic", "Junk, materials and other items with no special use.",
                () => new GenericComponent { InventoryCategory = InventoryCategory.Material }),
            new ItemTemplate("Food", "Something to eat.",
                () => new FoodComponent { InventoryCategory = InventoryCategory.Food, DaysToDecayIndoors = 100, DaysToDecayOutdoors = 60 }),
            new ItemTemplate("Drink", "Something to drink.",
                () => new FoodComponent { InventoryCategory = InventoryCategory.Food, Drink = true, Calories = 50, ThirstEffect = 25 }),
            new ItemTemplate("Liquid container", "A bottle or can holding water or kerosene.",
                () => new LiquidComponent { InventoryCategory = InventoryCategory.Material }),
            new ItemTemplate("Clothing", "Something to wear.",
                () => new ClothingComponent { InventoryCategory = InventoryCategory.Clothing, WeightKG = 0.5f, MaxLayer = ClothingLayer.Top }),
            new ItemTemplate("Tool", "A tool for crafting, repairing or breaking down.",
                () => new ToolComponent { InventoryCategory = InventoryCategory.Tool, BreakDown = true }),
            new ItemTemplate("First aid", "Medicine or a bandage.",
                () => new FirstAidComponent { InventoryCategory = InventoryCategory.FirstAid, WeightKG = 0.1f }),
            new ItemTemplate("Fire starter", "Matches, a flint or similar.",
                () => new GenericComponent { InventoryCategory = InventoryCategory.Firestarting, WeightKG = 0.1f },
                () => new FireStarterBehaviour()),
            new ItemTemplate("Tinder", "Tinder for starting a fire.",
                () => new GenericComponent { InventoryCategory = InventoryCategory.Firestarting, WeightKG = 0.05f },
                () => new TinderBehaviour(), () => new StackableBehaviour()),
            new ItemTemplate("Fuel", "Something to burn.",
                () => new GenericComponent { InventoryCategory = InventoryCategory.Firestarting },
                () => new BurnableBehaviour()),
            new ItemTemplate("Book", "A book that trains a skill.",
                () => new ResearchComponent { InventoryCategory = InventoryCategory.Material, WeightKG = 0.4f }),
            new ItemTemplate("Ammunition", "Ammunition for a vanilla gun.",
                () => new AmmoComponent { InventoryCategory = InventoryCategory.Tool, WeightKG = 0.01f },
                () => new StackableBehaviour()),
            new ItemTemplate("Bed", "A bedroll or sleeping bag.",
                () => new BedComponent { InventoryCategory = InventoryCategory.Tool, WeightKG = 2f }),
            new ItemTemplate("Cooking pot", "A pot or pan to cook in.",
                () => new CookingPotComponent { InventoryCategory = InventoryCategory.Tool, Template = "GEAR_CookingPot" }),
        };
    }
}
