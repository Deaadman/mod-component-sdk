using System;
using System.Linq;
using System.Reflection;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Integrations;
using ModComponent.SDK.Modules;
using NUnit.Framework;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    public class ModuleRegistryTests
    {
        [Test]
        public void EveryConcreteModule_IsRegisteredWithUniqueExportName()
        {
            var concrete = typeof(ModuleData).Assembly.GetTypes()
                .Where(t => typeof(ModuleData).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            Assert.That(concrete.Where(t => t.GetCustomAttribute<ModuleAttribute>(false) == null), Is.Empty,
                "Concrete module types without [Module].");
            Assert.That(ModuleRegistry.All.Select(m => m.ExportName), Is.Unique);
            Assert.That(ModuleRegistry.All.Count, Is.GreaterThanOrEqualTo(concrete.Count));
        }

        [Test]
        public void Registry_HasEveryLoaderType()
        {
            string[] expected =
            {
                "ModAmmoComponent", "ModBedComponent", "ModBodyHarvestComponent", "ModCharcoalComponent",
                "ModClothingComponent", "ModCollectibleComponent", "ModCookableComponent", "ModCookingPotComponent",
                "ModFirstAidComponent", "ModFoodComponent", "ModGenericComponent", "ModGenericEquippableComponent",
                "ModLiquidComponent", "ModPowderComponent", "ModPurificationComponent", "ModRandomItemComponent",
                "ModRandomWeightedItemComponent", "ModResearchComponent", "ModToolComponent",
                "ModAccelerantBehaviour", "ModBurnableBehaviour", "ModCarryingCapacityBehaviour", "ModEvolveBehaviour",
                "ModFireStarterBehaviour", "ModHarvestableBehaviour", "ModMillableBehaviour", "ModRepairableBehaviour",
                "ModScentBehaviour", "ModSharpenableBehaviour", "ModStackableBehaviour", "ModTinderBehaviour",
                "ChangeLayer"
            };

            Assert.That(ModuleRegistry.All.Select(m => m.ExportName), Is.SupersetOf(expected));
        }

        [Test]
        public void Kinds_FollowBaseClasses()
        {
            Assert.That(ModuleRegistry.Get(typeof(FoodComponent)).Kind, Is.EqualTo(ModuleKind.Component));
            Assert.That(ModuleRegistry.Get(typeof(StackableBehaviour)).Kind, Is.EqualTo(ModuleKind.Behaviour));
            Assert.That(ModuleRegistry.Get(typeof(ChangeLayerModification)).Kind, Is.EqualTo(ModuleKind.Modification));
            Assert.That(ModuleRegistry.Get(typeof(RandomWeightedItemComponent)).DisplayName, Is.EqualTo("Random Weighted Item"));
            Assert.That(ModuleRegistry.Get(typeof(BodyHarvestComponent)).DisplayName, Is.EqualTo("Body Harvest"));
        }

        [Test]
        public void FireBehaviours_ConflictBothWays()
        {
            Type[] fire = { typeof(AccelerantBehaviour), typeof(BurnableBehaviour), typeof(FireStarterBehaviour), typeof(TinderBehaviour) };
            foreach (var a in fire)
            foreach (var b in fire.Where(t => t != a))
                Assert.That(ModuleRegistry.Get(a).Conflicts, Does.Contain(b), $"{a.Name} should conflict with {b.Name}");
        }

        [Test]
        public void BlockReason_ExplainsConflictsAndDuplicates()
        {
            ModuleData[] existing = { new GenericComponent(), new TinderBehaviour() };

            Assert.That(ModuleRegistry.GetBlockReason(ModuleRegistry.Get(typeof(BurnableBehaviour)), existing),
                Is.EqualTo("conflicts with Tinder"));
            Assert.That(ModuleRegistry.GetBlockReason(ModuleRegistry.Get(typeof(TinderBehaviour)), existing),
                Is.EqualTo("already on this item"));
            Assert.That(ModuleRegistry.GetBlockReason(ModuleRegistry.Get(typeof(StackableBehaviour)), existing), Is.Null);
            Assert.That(ModuleRegistry.GetBlockReason(ModuleRegistry.Get(typeof(FoodComponent)), existing), Is.Null,
                "Adding a component replaces the current one.");
        }
    }

    public class AssetModelTests
    {
        [Test]
        public void GearItem_DerivesNamesFromGearName()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = "GEAR_WoolBalaclava";

            Assert.That(item.ShortName, Is.EqualTo("WoolBalaclava"));
            Assert.That(item.InventoryIconAddress, Is.EqualTo("ico_GearItem__WoolBalaclava"));
            Assert.That(item.CraftingIconAddress, Is.EqualTo("ico_CraftItem__WoolBalaclava"));
            UnityEngine.Object.DestroyImmediate(item);
        }

        [Test]
        public void GearItem_ComponentReplaces_BehaviourCannotRepeat()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.AddModule(new GenericComponent());
            var food = item.AddModule(new FoodComponent());
            item.AddModule(new StackableBehaviour());

            Assert.That(item.Component, Is.SameAs(food));
            Assert.Throws<InvalidOperationException>(() => item.AddModule(new StackableBehaviour()));
            item.AddModule(new TinderBehaviour());
            Assert.Throws<InvalidOperationException>(() => item.AddModule(new BurnableBehaviour()),
                "Fire behaviours conflict; ModComponent only loads one.");
            item.RemoveModule(item.GetModule<TinderBehaviour>());
            Assert.That(item.AllModules.Count(), Is.EqualTo(2));
            Assert.That(item.RemoveModule(food), Is.True);
            Assert.That(item.Component, Is.Null);
            UnityEngine.Object.DestroyImmediate(item);
        }

        [Test]
        public void GearItem_IntegrationDataIsAddedOnce()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            var a = item.GetOrAddIntegrationData<ItemRarityData>();
            var b = item.GetOrAddIntegrationData<ItemRarityData>();

            Assert.That(a, Is.SameAs(b));
            Assert.That(item.IntegrationData.Count, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(item);
        }

        [Test]
        public void ComponentDefaults_AvoidLoaderZeroTraps()
        {
            var food = new FoodComponent();

            Assert.That(food.InspectScale, Is.EqualTo(Vector3.one));
            Assert.That(food.StowAudio.Id, Is.EqualTo("Play_InventoryStow"));
            Assert.That(food.InspectDistance, Is.EqualTo(0.4f));
            Assert.That(new PowderComponent().ChanceFull, Is.EqualTo(100f));
            Assert.That(new ClothingComponent().HoursToFreeze, Is.GreaterThan(0f));
        }

        [Test]
        public void LocalizationTable_StoresOnlyNonEmptyText()
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.Set("GAMEPLAY_Thermos", "English", "Thermos Flask");
            table.Set("GAMEPLAY_Thermos", "German", "Thermoskanne");
            table.Set("GAMEPLAY_Thermos", "German", "");

            Assert.That(table.Get("GAMEPLAY_Thermos"), Is.EqualTo("Thermos Flask"));
            Assert.That(table.Get("GAMEPLAY_Thermos", "German"), Is.Null);
            Assert.That(table.Find("GAMEPLAY_Thermos").Translations.Count, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void Mod_ToId_KeepsFileSafeCharacters()
        {
            Assert.That(Mod.ToId(" Warm Gear! v2 "), Is.EqualTo("WarmGearv2"));
            Assert.That(Mod.ToId("bushcraft-tools_2"), Is.EqualTo("bushcraft-tools_2"));
            Assert.That(Mod.ToId("Café Ёлка"), Is.EqualTo("Caf"));
            Assert.That(Mod.ToId("   "), Is.Empty);
            Assert.That(Mod.IsValidId("Warm-Gear_2"), Is.True);
            Assert.That(Mod.IsValidId("Warm Gear"), Is.False);
        }

        [Test]
        public void NewRows_StartWithUsableValues()
        {
            Assert.That(new SpawnTable.SceneSpawn().Chance, Is.EqualTo(100f));
            Assert.That(new SpawnTable.LootEntry().Weight, Is.EqualTo(1));
            Assert.That(new GearCount().Count, Is.EqualTo(1));
            Assert.That(new Blueprint.GearRequirement().Count, Is.EqualTo(1));
        }

        [Test]
        public void Mod_IntegrationsToggle()
        {
            var mod = ScriptableObject.CreateInstance<Mod>();
            mod.EnableIntegration<ItemRaritiesIntegration>();
            mod.EnableIntegration<ItemRaritiesIntegration>();

            Assert.That(mod.Integrations.Count, Is.EqualTo(1));
            Assert.That(mod.DisableIntegration<ItemRaritiesIntegration>(), Is.True);
            Assert.That(mod.HasIntegration<ItemRaritiesIntegration>(), Is.False);
            UnityEngine.Object.DestroyImmediate(mod);
        }
    }
}
