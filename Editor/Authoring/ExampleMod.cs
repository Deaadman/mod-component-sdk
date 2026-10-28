using System.Linq;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Modules;
using UnityEditor;
using UnityEngine;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>
    /// Creates a small, complete mod to learn from: a food item, a tool and a scarf, a blueprint, localization in
    /// two languages, a scene spawn and a loot-table entry. Built from primitives, so it needs no art.
    /// </summary>
    public static class ExampleMod
    {
        public const string Id = "ExampleMod";

        [MenuItem("ModComponent SDK/Create Example Mod", false, 80)]
        private static void CreateFromMenu()
        {
            var existing = ModValidator.FindAllMods().FirstOrDefault(m => m.Id == Id);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Selection.activeObject = Create();
            UI.Windows.ModHubWindow.Open();
        }

        public static Mod Create(string folder = AssetFactory.DefaultModsFolder + "/" + Id)
        {
            var mod = AssetFactory.CreateMod(new AssetFactory.ModSettings
            {
                DisplayName = "Example Mod", Id = Id, Author = "ModComponent SDK", Version = "1.0.0", Folder = folder
            });
            mod.Description = "A food item, a tool and a scarf showing each part of the SDK.";

            var modelFolder = AssetFactory.EnsureFolder(folder + "/Models");

            var tin = AssetFactory.CreateItem(new AssetFactory.ItemSettings
            {
                Mod = mod, GearName = "GEAR_ExampleTinCan", DisplayName = "Tin of Stew",
                Description = "A dented tin of beef stew. Heat it for a warm meal.",
                Model = Model(modelFolder, "TinCanModel", PrimitiveType.Cylinder, new Vector3(0.07f, 0.05f, 0.07f), new Color(0.72f, 0.72f, 0.75f)),
                Template = ItemTemplates.All.First(t => t.Name == "Food")
            });
            var food = (FoodComponent)tin.Component;
            food.WeightKG = 0.45f;
            food.Calories = 450;
            food.Canned = true;
            food.Opening = true;
            food.OpeningWithCanOpener = food.OpeningWithKnife = food.OpeningWithHatchet = food.OpeningWithSmashing = true;
            food.EatingPackagedAudio = "Play_EatingCan";
            food.PickUpAudio = "Play_SndInvMetalSmall";
            food.InspectAngles = new Vector3(0, 30, 0);
            tin.AddModule(new StackableBehaviour { UnitsPerItem = 1, ChanceFull = 100 });
            EditorUtility.SetDirty(tin);

            var knife = AssetFactory.CreateItem(new AssetFactory.ItemSettings
            {
                Mod = mod, GearName = "GEAR_ExampleWhittlingKnife", DisplayName = "Whittling Knife",
                Description = "A small folding knife. Good for breaking down light materials.",
                Model = Model(modelFolder, "KnifeModel", PrimitiveType.Cube, new Vector3(0.02f, 0.01f, 0.16f), new Color(0.35f, 0.3f, 0.25f)),
                Template = ItemTemplates.All.First(t => t.Name == "Tool")
            });
            var tool = (ToolComponent)knife.Component;
            tool.WeightKG = 0.15f;
            tool.ToolType = CuttingToolType.Knife;
            tool.DegradeOnUse = 1f;
            tool.CarcassHarvesting = true;
            tool.MinutesPerKgMeat = 12;
            tool.MinutesPerHide = 20;
            tool.MinutesPerGut = 10;
            EditorUtility.SetDirty(knife);

            var scarf = AssetFactory.CreateItem(new AssetFactory.ItemSettings
            {
                Mod = mod, GearName = "GEAR_ExampleWoolScarf", DisplayName = "Wool Scarf",
                Description = "Knitted from old sweaters. Warm even when wet.",
                Model = Model(modelFolder, "ScarfModel", PrimitiveType.Capsule, new Vector3(0.1f, 0.03f, 0.1f), new Color(0.55f, 0.2f, 0.18f)),
                Template = ItemTemplates.All.First(t => t.Name == "Clothing")
            });
            var clothing = (ClothingComponent)scarf.Component;
            clothing.Region = ClothingRegion.Accessory;
            clothing.Warmth = 1f;
            clothing.WarmthWhenWet = 0.6f;
            clothing.Windproof = 0.5f;
            clothing.DaysToDecayWornOutside = 200;
            clothing.DaysToDecayWornInside = 300;
            clothing.MovementSound = ClothingMovementSound.Wool;
            scarf.AddModule(new RepairableBehaviour
            {
                Minutes = 30, Condition = 25, RequiredTools = { "GEAR_SewingKit" }, Materials = { new GearCount("GEAR_Cloth", 1) }
            });
            EditorUtility.SetDirty(scarf);

            UI.Preview.IconRenderer.PlaceholderAndAssign(tin, new Color(0.72f, 0.72f, 0.75f));
            UI.Preview.IconRenderer.PlaceholderAndAssign(knife, new Color(0.45f, 0.38f, 0.3f));
            UI.Preview.IconRenderer.PlaceholderAndAssign(knife, new Color(0.45f, 0.38f, 0.3f), crafting: true);
            UI.Preview.IconRenderer.PlaceholderAndAssign(scarf, new Color(0.55f, 0.2f, 0.18f));

            var blueprint = AssetFactory.CreateBlueprint(mod, "BP_ExampleWhittlingKnife", knife);
            blueprint.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_ScrapMetal", Count = 1 });
            blueprint.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_Stick", Count = 1 });
            blueprint.RequiredTool = "GEAR_Hacksaw";
            blueprint.RequiredCraftingLocation = CraftingLocation.Workbench;
            blueprint.DurationMinutes = 90;
            blueprint.AppliedSkill = SkillType.ToolRepair;
            blueprint.ImprovedSkill = SkillType.ToolRepair;
            EditorUtility.SetDirty(blueprint);

            var table = mod.Localization;
            table.Set("GAMEPLAY_ExampleTinCan", "German", "Eintopf in der Dose");
            table.Set("GAMEPLAY_ExampleWhittlingKnife", "German", "Schnitzmesser");
            table.Set("GAMEPLAY_ExampleWoolScarf", "German", "Wollschal");
            EditorUtility.SetDirty(table);

            mod.Spawns.Scenes.Add(new SpawnTable.SceneGroup
            {
                Scene = "CoastalRegion",
                Spawns = { new SpawnTable.SceneSpawn { Item = "GEAR_ExampleTinCan", Position = new Vector3(412.3f, 18.75f, -120.4f), Chance = 100 } }
            });
            mod.Spawns.LootTables.Add(new SpawnTable.LootTableGroup
            {
                LootTable = "LootTableVehicleGloveBox", Entries = { new SpawnTable.LootEntry { Item = "GEAR_ExampleWoolScarf", Weight = 5 } }
            });
            EditorUtility.SetDirty(mod.Spawns);
            EditorUtility.SetDirty(mod);
            AssetDatabase.SaveAssets();
            return mod;
        }

        private static GameObject Model(string folder, string name, PrimitiveType shape, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());

            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var material = new Material(shader) { color = color, name = name + "Material" };
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.mat"));
                go.GetComponent<Renderer>().sharedMaterial = material;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
