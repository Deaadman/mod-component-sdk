using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Catalog;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Modules;
using NUnit.Framework;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    public class ValidationTests
    {
        private static readonly GameDataCatalog Catalog = GameDataCatalog.Parse(
            "{\"game\":\"2.58\",\"languages\":[\"English\",\"German\"]," +
            "\"gear\":[{\"id\":\"GEAR_Hatchet\",\"source\":\"vanilla\"},{\"id\":\"GEAR_Cloth\",\"source\":\"vanilla\"}]," +
            "\"sounds\":[\"Play_SndInvClothLight\"],\"scenes\":[\"CoastalRegion\"],\"lootTables\":[\"LootTableVehicleGloveBox\"]," +
            "\"liquids\":[\"LIQUID_Kerosene\"],\"powders\":[\"POWDER_Gunpowder\"]}");

        private readonly List<Object> _created = new();
        private Mod _mod;

        [SetUp]
        public void SetUp()
        {
            _mod = Make<Mod>();
            _mod.DisplayName = "Warm Gear";
            _mod.Id = "WarmGear";
            _mod.Author = "Deadman";
            _mod.Version = "1.0.0";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _created.Add(o);
            return o;
        }

        private GearItem Item(string gearName, ComponentModule component = null)
        {
            var item = Make<GearItem>();
            item.name = gearName;
            item.GearName = gearName;
            if (component != null) item.AddModule(component);
            _mod.Items.Add(item);
            return item;
        }

        private ValidationResult Validate(bool forBuild = false, params Mod[] others) =>
            ModValidator.Validate(_mod, forBuild, Catalog, others);

        private IEnumerable<string> Messages(string ruleId) =>
            Validate().Issues.Where(i => i.RuleId == ruleId).Select(i => i.Message);

        [Test]
        public void Rules_AreDiscoveredWithUniqueIds()
        {
            Assert.That(ModValidator.Rules.Select(r => r.Id), Is.Unique);
            Assert.That(ModValidator.Rules.Count, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void CleanMod_HasNoErrors()
        {
            Item("GEAR_WoolHat", new GenericComponent());
            Assert.That(Validate().Errors.Where(e => e.RuleId != "item.prefab"), Is.Empty);
        }

        [Test]
        public void ModIdentity_OffersAFixForABadId()
        {
            _mod.Id = "Warm Gear!";
            var issue = Validate().Errors.Single(i => i.RuleId == "mod.identity");

            Assert.That(issue.HasFix, Is.True);
            issue.Fix();
            Assert.That(_mod.Id, Is.EqualTo("WarmGear"));
        }

        [Test]
        public void ModIdentity_FlagsAnotherModWithTheSameId()
        {
            var other = Make<Mod>();
            other.Id = "warmgear";
            Assert.That(Validate(false, other).Errors.Select(e => e.Message), Has.Some.Contains("same id"));
        }

        [Test]
        public void GearName_RejectsBadAndVanillaNames()
        {
            Item("GEAR_Wool Hat", new GenericComponent());
            Item("GEAR_Hatchet", new GenericComponent());

            var messages = Messages("item.gear-name").ToList();
            Assert.That(messages, Has.Some.Contains("GEAR_ followed by"));
            Assert.That(messages, Has.Some.Contains("vanilla item"));
        }

        [Test]
        public void Component_IsRequired()
        {
            Item("GEAR_Empty");
            Assert.That(Messages("item.component"), Has.Some.Contains("no component"));
        }

        [Test]
        public void ModuleValues_CatchesLoaderTraps()
        {
            Item("GEAR_Stew", new FoodComponent { Cooking = true });
            Item("GEAR_Pot", new CookingPotComponent());
            Item("GEAR_Coat", new ClothingComponent { HoursToFreeze = 0 });
            var stack = Item("GEAR_Rag", new GenericComponent());
            stack.AddModule(new StackableBehaviour { ShareStackWithGear = { "" } });
            var weighted = Item("GEAR_Box", new RandomWeightedItemComponent { Items = { new GearCount("GEAR_Cloth", 0) } });

            var messages = Messages("item.module-values").ToList();
            Assert.That(messages, Has.Some.Contains("cooking result"));
            Assert.That(messages, Has.Some.Contains("template"));
            Assert.That(messages, Has.Some.Contains("above 0"));
            Assert.That(messages, Has.Some.Contains("empty entry"));
            Assert.That(messages, Has.Some.Contains("at least 1"));
        }

        [Test]
        public void References_WarnOnUnknownNames_AndAcceptProjectItems()
        {
            var cloth = Item("GEAR_WoolScrap", new GenericComponent());
            var hat = Item("GEAR_WoolHat", new GenericComponent { PickUpAudio = "Play_Nope" });
            hat.AddModule(new RepairableBehaviour
            {
                RequiredTools = { "GEAR_WoolScrap" },
                Materials = { new GearCount("GEAR_WoolScrap", 1), new GearCount("GEAR_Unknown", 1), new GearCount("Cloth", 1) }
            });

            var issues = Validate().Issues.Where(i => i.RuleId == "item.references").ToList();

            Assert.That(issues.Select(i => i.Message), Has.Some.Contains("GEAR_Unknown"));
            Assert.That(issues.Select(i => i.Message), Has.Some.Contains("Play_Nope"));
            Assert.That(issues.Where(i => i.Severity == Severity.Error).Select(i => i.Message), Has.Some.Contains("isn't a gear name"));
            Assert.That(issues.Where(i => i.Severity == Severity.Error).Select(i => i.Message), Has.Some.Contains("isn't a tool"));
            Assert.That(issues.Select(i => i.Message), Has.None.Contains("GEAR_WoolScrap isn't a vanilla"));
            Assert.That(issues.Select(i => i.Field), Has.Some.EqualTo("Repairable › Materials[1]"));
        }

        [Test]
        public void Localization_FlagsMissingKeysAndUnknownLanguages()
        {
            Item("GEAR_WoolHat", new GenericComponent { DisplayNameLocalizationId = "GAMEPLAY_WoolHat" });
            Assert.That(Messages("localization"), Has.Some.Contains("no localization table"));

            var table = Make<LocalizationTable>();
            table.Set("GAMEPLAY_Other", "German", "Andere");
            table.Set("GAMEPLAY_Other", "Klingon", "x");
            _mod.Localization = table;

            var issues = Validate().Issues.Where(i => i.RuleId == "localization").ToList();
            Assert.That(issues.Select(i => i.Message), Has.Some.Contains("no English text"));
            Assert.That(issues.Select(i => i.Message), Has.Some.Contains("\"Klingon\""));
            var missing = issues.Single(i => i.Message.Contains("GAMEPLAY_WoolHat"));
            missing.Fix();
            Assert.That(table.Contains("GAMEPLAY_WoolHat"), Is.True);
        }

        [Test]
        public void Blueprint_NeedsResultIngredientsAndValidCounts()
        {
            var bp = Make<Blueprint>();
            bp.name = "BP_Hat";
            _mod.Blueprints.Add(bp);
            Assert.That(Messages("blueprint"), Has.Some.Contains("crafted result").And.Some.Contains("ingredient"));

            bp.CraftedResult = "GEAR_Hatchet";
            bp.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_Cloth", Count = 0, Units = RequirementUnits.Kilograms, Quantity = 1 });
            bp.RequiredLiquid.Add(new Blueprint.LiquidRequirement { Liquid = "LIQUID_Water" });
            var messages = Messages("blueprint").ToList();
            Assert.That(messages, Has.Some.Contains("count of at least 1"));
            Assert.That(messages, Has.Some.Contains("liquid type"));

            bp.IsCookingRecipe = true;
            Assert.That(Messages("blueprint"), Has.None.Contains("count of at least 1"), "recipes may use kilograms alone");
        }

        [Test]
        public void Blueprint_ResultFromThisModNeedsCraftingIcon()
        {
            Item("GEAR_WoolHat", new GenericComponent());
            var bp = Make<Blueprint>();
            bp.CraftedResult = "GEAR_WoolHat";
            bp.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_Cloth" });
            _mod.Blueprints.Add(bp);

            Assert.That(Messages("item.icons"), Has.Some.Contains("crafting icon"));
        }

        [Test]
        public void Spawns_RejectNonWordNames()
        {
            var spawns = Make<SpawnTable>();
            spawns.Scenes.Add(new SpawnTable.SceneGroup { Scene = "CoastalRegion", Tag = "winter v2", Spawns = { new SpawnTable.SceneSpawn { Item = "GEAR_Wool-Hat" } } });
            spawns.LootTables.Add(new SpawnTable.LootTableGroup { LootTable = "VehicleGloveBox", Entries = { new SpawnTable.LootEntry { Item = "GEAR_Cloth", Weight = 0 } } });
            _mod.Spawns = spawns;

            var issues = Validate().Issues.Where(i => i.RuleId == "spawns").ToList();
            Assert.That(issues.Where(i => i.Severity == Severity.Error).Select(i => i.Message),
                Has.Some.Contains("Tag").And.Some.Contains("GEAR_Wool-Hat").And.Some.Contains("weights"));
            Assert.That(issues.Select(i => i.Message), Has.None.Contains("isn't a known loot table"), "short loot table names are valid");
        }

        [Test]
        public void ProjectSetup_OnlyReportsWhenBuilding()
        {
            Assert.That(Validate(forBuild: false).Issues.Where(i => i.RuleId == "project.setup"), Is.Empty);
        }

        [Test]
        public void CrashingRule_BecomesAnErrorInsteadOfThrowing()
        {
            _mod.Items.Add(null);
            Assert.DoesNotThrow(() => Validate());
            Assert.That(Validate().Errors.Select(e => e.RuleId), Does.Contain("mod.content"));
        }
    }
}
