using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.Editor.Modules;
using ModComponent.SDK.Modules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    /// <summary>
    /// Pins the JSON contract to ModComponent 7.0.0: for every module, the exported keys must be exactly the
    /// keys the loader reads (taken from its InitializeComponent methods), minus ones it ignores.
    /// </summary>
    public class LoaderContractTests
    {
        private static readonly string[] Base =
        {
            "DisplayNameLocalizationId", "DescriptionLocalizatonId", "InventoryActionLocalizationId", "WeightKG",
            "DaysToDecay", "MaxHP", "InitialCondition", "InventoryCategory", "PickUpAudio", "PutBackAudio", "StowAudio",
            "WornOutAudio", "InspectOnPickup", "InspectDistance", "InspectAngles", "InspectOffset", "InspectScale",
            "NormalModel", "InspectModel"
        };

        private static readonly string[] Equippable = { "EquippedModelPrefab", "ImplementationType", "EquippingAudio" };

        private static readonly string[] Cookable =
        {
            "BurntMinutes", "Cooking", "CookingAudio", "StartCookingAudio", "CookingMinutes", "CookingResult",
            "CookingUnitsRequired", "CookingWaterRequired", "Type"
        };

        private static readonly Dictionary<string, string[]> Expected = new()
        {
            ["ModGenericComponent"] = Base,
            ["ModGenericEquippableComponent"] = Base.Concat(Equippable).ToArray(),
            ["ModAmmoComponent"] = Base.Append("AmmoForGunType").ToArray(),
            ["ModBedComponent"] = Base.Concat(new[]
            {
                "ConditionGainPerHour", "AdditionalConditionGainPerHour", "WarmthBonusCelsius", "DegradePerHour",
                "BearAttackModifier", "WolfAttackModifier", "OpenAudio", "CloseAudio", "PackedMesh", "UsableMesh"
            }).ToArray(),
            ["ModBodyHarvestComponent"] = Base.Concat(new[]
            {
                "CanCarry", "HarvestAudio", "GutPrefab", "GutQuantity", "GutWeightKgPerUnit", "HidePrefab", "HideQuantity",
                "HideWeightKgPerUnit", "MeatPrefab", "MeatAvailableMinKG", "MeatAvailableMaxKG", "GutLabelOverride",
                "HideLabelOverride", "MeatLabelOverride"
            }).ToArray(),
            ["ModCharcoalComponent"] = Base.Concat(new[] { "SurveyGameMinutes", "SurveyRealSeconds", "SurveySkillExtendedHours", "SurveyLoopAudio" }).ToArray(),
            ["ModClothingComponent"] = Base.Concat(new[]
            {
                "Region", "MinLayer", "MaxLayer", "MovementSound", "DaysToDecayWornOutside", "DaysToDecayWornInside", "Warmth",
                "WarmthWhenWet", "Windproof", "Waterproofness", "Toughness", "SprintBarReduction", "DecreaseAttackChance",
                "IncreaseFleeChance", "HoursToDryNearFire", "HoursToDryWithoutFire", "HoursToFreeze", "MainTexture",
                "BlendTexture", "DrawLayer", "ImplementationType", "FirstPersonPrefabMale", "FirstPersonPrefabFemale",
                "PreventAllDamageFromSource"
            }).ToArray(),
            ["ModCollectibleComponent"] = Base.Concat(new[] { "HudMessageLocalizationId", "NarrativeTextLocalizationId", "TextAlignment" }).ToArray(),
            ["ModCookableComponent"] = Base.Concat(Cookable).ToArray(),
            ["ModCookingPotComponent"] = Base.Concat(new[] { "CanCookLiquid", "CanCookGrub", "CanCookMeat", "Capacity", "Template" }).ToArray(),
            ["ModFirstAidComponent"] = Base.Concat(new[] { "ProgressBarMessage", "RemedyText", "InstantHealing", "FirstAidType", "TimeToUseSeconds", "UnitsPerUse", "UseAudio" }).ToArray(),
            ["ModFoodComponent"] = Base.Concat(Cookable).Concat(new[]
            {
                "DaysToDecayOutdoors", "DaysToDecayIndoors", "Calories", "EatingTime", "EatingAudio", "EatingPackagedAudio",
                "ThirstEffect", "FoodPoisoning", "FoodPoisoningLowCondition", "ParasiteRiskIncrements", "Natural", "Raw",
                "Drink", "Meat", "Fish", "Canned", "Opening", "OpeningWithCanOpener", "OpeningWithKnife", "OpeningWithHatchet",
                "OpeningWithSmashing", "AffectCondition", "ConditionRestBonus", "ConditionRestMinutes", "AffectRest",
                "InstantRestChange", "RestFactorMinutes", "AffectCold", "InstantColdChange", "ColdFactorMinutes",
                "ContainsAlcohol", "AlcoholPercentage", "AlcoholUptakeMinutes", "VitaminC"
            }).ToArray(),
            ["ModLiquidComponent"] = Base.Concat(new[] { "LiquidType", "LiquidCapacityLiters", "RandomizedQuantity", "LiquidLiters" }).ToArray(),
            ["ModPowderComponent"] = Base.Concat(new[] { "CapacityKG", "ChanceFull" }).ToArray(),
            ["ModPurificationComponent"] = Base.Concat(new[] { "LitersPurify", "ProgressBarDurationSeconds", "ProgressBarLocalizationID", "PurifyAudio" }).ToArray(),
            ["ModRandomItemComponent"] = Base.Append("ItemNames").ToArray(),
            ["ModRandomWeightedItemComponent"] = Base.Concat(new[] { "ItemNames", "ItemWeights" }).ToArray(),
            ["ModResearchComponent"] = Base.Concat(new[] { "SkillType", "TimeRequirementHours", "SkillPoints", "NoBenefitAtSkillLevel", "ReadAudio" }).ToArray(),
            ["ModToolComponent"] = Base.Concat(Equippable).Concat(new[]
            {
                "ToolType", "DegradeOnUse", "Usage", "SkillBonus", "CraftingTimeMultiplier", "DegradePerHourCrafting",
                "BreakDown", "BreakDownTimeMultiplier", "ForceLocks", "ForceLockAudio", "IceFishingHole",
                "IceFishingHoleDegradeOnUse", "IceFishingHoleMinutes", "IceFishingHoleAudio", "CarcassHarvesting",
                "MinutesPerKgMeat", "MinutesPerKgFrozenMeat", "MinutesPerHide", "MinutesPerGut", "DegradePerHourHarvesting",
                "StruggleBonus", "DamageMultiplier", "FleeChanceMultiplier", "TapMultiplier", "CanPuncture", "BleedoutMultiplier"
            }).ToArray(),
            ["ModAccelerantBehaviour"] = new[] { "DestroyedOnUse" },
            ["ModBurnableBehaviour"] = new[] { "BurningMinutes", "BurningMinutesBeforeAllowedToAdd", "TempIncrease" },
            ["ModCarryingCapacityBehaviour"] = new[] { "MaxCarryCapacityKGBuff" },
            ["ModEvolveBehaviour"] = new[] { "TargetItemName", "EvolveHours", "IndoorsOnly" },
            ["ModFireStarterBehaviour"] = new[] { "DestroyedOnUse", "NumberOfUses", "OnUseSoundEvent", "RequiresSunLight", "SecondsToIgniteTinder", "SecondsToIgniteTorch" },
            ["ModHarvestableBehaviour"] = new[] { "Audio", "Minutes", "YieldCounts", "YieldNames", "RequiredToolNames" },
            ["ModMillableBehaviour"] = new[] { "RepairDurationMinutes", "RepairRequiredGear", "RepairRequiredGearUnits", "CanRestoreFromWornOut", "RecoveryDurationMinutes", "RestoreRequiredGear", "RestoreRequiredGearUnits", "Skill" },
            ["ModRepairableBehaviour"] = new[] { "Audio", "Minutes", "Condition", "RequiredTools", "MaterialNames", "MaterialCounts" },
            ["ModScentBehaviour"] = new[] { "ScentCategory" },
            ["ModSharpenableBehaviour"] = new[] { "Audio", "MinutesMin", "MinutesMax", "ConditionMin", "ConditionMax", "Tools" },
            ["ModStackableBehaviour"] = new[] { "MultipleUnitTextId", "StackSprite", "SingleUnitTextId", "UnitsPerItem", "ChanceFull", "ShareStackWithGear", "InstantiateStackItem", "StackConditionDifferenceConstraint" },
            ["ModTinderBehaviour"] = Array.Empty<string>(),
            ["ChangeLayer"] = new[] { "Layer", "Recursively" },
        };

        [Test]
        public void EveryModule_ExportsExactlyTheLoaderKeys()
        {
            var failures = new List<string>();
            foreach (var info in ModuleRegistry.All)
            {
                if (!Expected.TryGetValue(info.ExportName, out var expected))
                {
                    failures.Add($"{info.ExportName}: no expected key list");
                    continue;
                }

                var actual = ModuleSerializer.GetExportedKeys(info.Type).ToList();
                var missing = expected.Except(actual).ToList();
                var extra = actual.Except(expected).ToList();
                if (missing.Count > 0 || extra.Count > 0)
                    failures.Add($"{info.ExportName}: missing [{string.Join(", ", missing)}] extra [{string.Join(", ", extra)}]");
                if (actual.Distinct().Count() != actual.Count)
                    failures.Add($"{info.ExportName}: duplicate keys");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void EveryModule_SerializesWithDefaults()
        {
            foreach (var info in ModuleRegistry.All)
            {
                var json = ModuleSerializer.Serialize(info.CreateInstance());
                Assert.That(json.Properties().Select(p => p.Name), Is.EquivalentTo(ModuleSerializer.GetExportedKeys(info.Type)), info.ExportName);
            }
        }
    }

    public class ModuleSerializerTests
    {
        [Test]
        public void Values_UseLoaderFriendlyTypes()
        {
            var food = new FoodComponent
            {
                DisplayNameLocalizationId = "GAMEPLAY_Tea",
                WeightKG = 0.25f,
                InitialCondition = InitialCondition.Perfect,
                InspectAngles = new Vector3(0, 35, 0),
                ParasiteRiskIncrements = new List<float> { 1.5f, 2f },
                Calories = 120
            };

            var json = ModuleSerializer.Serialize(food);

            Assert.That(json["DisplayNameLocalizationId"].Type, Is.EqualTo(JTokenType.String));
            Assert.That(json["DescriptionLocalizatonId"], Is.Not.Null, "Loader's misspelled key");
            Assert.That(json["WeightKG"].Type, Is.EqualTo(JTokenType.Float));
            Assert.That(json["Calories"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That(json["InitialCondition"].Value<string>(), Is.EqualTo("Perfect"));
            Assert.That(json["InspectAngles"].ToObject<float[]>(), Is.EqualTo(new[] { 0f, 35f, 0f }));
            Assert.That(json["InspectScale"].ToObject<float[]>(), Is.EqualTo(new[] { 1f, 1f, 1f }));
            Assert.That(json["ParasiteRiskIncrements"].ToObject<float[]>(), Is.EqualTo(new[] { 1.5f, 2f }));
            Assert.That(json["StowAudio"].Value<string>(), Is.EqualTo("Play_InventoryStow"));
            Assert.That(json["NormalModel"].Value<string>(), Is.EqualTo(""));
        }

        [Test]
        public void SplitArrays_WriteParallelLists()
        {
            var harvest = new HarvestableBehaviour
            {
                Yields = new List<GearCount> { new("GEAR_Stick", 2), new("GEAR_Cloth", 1) },
                RequiredToolNames = new List<References.GearRef> { "GEAR_Hatchet" }
            };

            var json = ModuleSerializer.Serialize(harvest);

            Assert.That(json["YieldNames"].ToObject<string[]>(), Is.EqualTo(new[] { "GEAR_Stick", "GEAR_Cloth" }));
            Assert.That(json["YieldCounts"].ToObject<int[]>(), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(json["RequiredToolNames"].ToObject<string[]>(), Is.EqualTo(new[] { "GEAR_Hatchet" }));
            Assert.That(json["Yields"], Is.Null);
        }

        [Test]
        public void Item_UsesExportNamesAndLowercaseFileName()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = "GEAR_ThermosFlask";
            item.AddModule(new LiquidComponent());
            item.AddModule(new StackableBehaviour());
            item.AddModule(new ChangeLayerModification { Layer = 8 });

            var json = ModWriters.Item(item);

            Assert.That(ModWriters.ItemFileName(item), Is.EqualTo("thermosflask.json"));
            Assert.That(json.Properties().Select(p => p.Name),
                Is.EqualTo(new[] { "ModLiquidComponent", "ModStackableBehaviour", "ChangeLayer" }));
            Assert.That(json["ChangeLayer"]["Layer"].Value<int>(), Is.EqualTo(8));
            UnityEngine.Object.DestroyImmediate(item);
        }

        [Test]
        public void ItemFileName_RemovesEveryGearPrefixLikeTheLoader()
        {
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.GearName = "GEAR_Big_GEAR_Box";
            Assert.That(ModWriters.ItemFileName(item), Is.EqualTo("big_box.json"));
            UnityEngine.Object.DestroyImmediate(item);
        }
    }

    public class LoaderQuirkTests
    {
        [Test]
        public void Clothing_FirstPersonPrefabsAreSwapped()
        {
            var json = ModuleSerializer.Serialize(new ClothingComponent { FirstPersonPrefabMale = "Arms_M", FirstPersonPrefabFemale = "Arms_F" });
            var notes = LoaderQuirks.Apply("ModClothingComponent", json).ToList();

            Assert.That(json["FirstPersonPrefabMale"].Value<string>(), Is.EqualTo("Arms_F"));
            Assert.That(json["FirstPersonPrefabFemale"].Value<string>(), Is.EqualTo("Arms_M"));
            Assert.That(notes, Has.Count.EqualTo(1));
        }

        [Test]
        public void Cookable_EmptyCookingResultIsFilled_SetOneIsKept()
        {
            var empty = ModuleSerializer.Serialize(new FoodComponent());
            LoaderQuirks.Apply("ModFoodComponent", empty).ToList();
            Assert.That(empty["CookingResult"].Value<string>(), Is.EqualTo(LoaderQuirks.SafeCookingResult));

            var set = ModuleSerializer.Serialize(new CookableComponent { CookingResult = "GEAR_CookedStew" });
            Assert.That(LoaderQuirks.Apply("ModCookableComponent", set), Is.Empty);
            Assert.That(set["CookingResult"].Value<string>(), Is.EqualTo("GEAR_CookedStew"));
        }

        [Test]
        public void Quirks_OnlyApplyToTheirLoaderVersion()
        {
            var json = ModuleSerializer.Serialize(new ClothingComponent { FirstPersonPrefabMale = "M" });
            Assert.That(LoaderQuirks.Apply("ModClothingComponent", json, "9.9.9"), Is.Empty);
            Assert.That(json["FirstPersonPrefabMale"].Value<string>(), Is.EqualTo("M"));
        }
    }

    public class WriterTests
    {
        private Mod _mod;

        [SetUp]
        public void SetUp()
        {
            _mod = ScriptableObject.CreateInstance<Mod>();
            _mod.DisplayName = "Warm Gear";
            _mod.Id = "WarmGear";
            _mod.Author = "Deadman";
            _mod.Version = "1.2.0";
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_mod);

        [Test]
        public void BuildInfo_AlwaysHasRequiresArray()
        {
            var json = ModWriters.BuildInfo(_mod);

            Assert.That(json["Name"].Value<string>(), Is.EqualTo("Warm Gear"));
            Assert.That(json["Requires"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(json["RequiresDLC"].Value<bool>(), Is.False);
        }

        [Test]
        public void GearSpawns_UseInvariantNumbers_InAnyCulture()
        {
            var table = ScriptableObject.CreateInstance<SpawnTable>();
            table.Scenes.Add(new SpawnTable.SceneGroup
            {
                Scene = "CoastalRegion",
                Spawns =
                {
                    new SpawnTable.SceneSpawn { Item = "GEAR_WoolBalaclava", Position = new Vector3(412.3f, 18.75f, -120.4f), Chance = 35 },
                    new SpawnTable.SceneSpawn { Item = "GEAR_Thermos", Position = new Vector3(1e-7f, -0f, 2), Rotation = new Vector3(0, 90, 0) }
                }
            });
            table.LootTables.Add(new SpawnTable.LootTableGroup
            {
                LootTable = "LootTableVehicleGloveBox",
                Entries = { new SpawnTable.LootEntry { Item = "GEAR_PineTeaBag", Weight = 4 } }
            });

            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                var text = ModWriters.GearSpawns(table);
                Assert.That(text, Is.EqualTo(
                    "scene=CoastalRegion\n" +
                    "item=GEAR_WoolBalaclava p=412.3,18.75,-120.4 c=35\n" +
                    "item=GEAR_Thermos p=0,0,2 r=0,90,0 c=100\n" +
                    "\n" +
                    "loottable=LootTableVehicleGloveBox\n" +
                    "item=GEAR_PineTeaBag w=4\n" +
                    "\n"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void GearSpawns_TagCarriesOverUntilChanged()
        {
            var table = ScriptableObject.CreateInstance<SpawnTable>();
            table.Scenes.Add(new SpawnTable.SceneGroup { Scene = "A", Tag = "winter", Spawns = { new SpawnTable.SceneSpawn { Item = "GEAR_X" } } });
            table.Scenes.Add(new SpawnTable.SceneGroup { Scene = "B", Tag = "winter", Spawns = { new SpawnTable.SceneSpawn { Item = "GEAR_X" } } });
            table.Scenes.Add(new SpawnTable.SceneGroup { Scene = "C", Spawns = { new SpawnTable.SceneSpawn { Item = "GEAR_X" } } });

            var lines = ModWriters.GearSpawns(table).Split('\n');

            Assert.That(lines.Count(l => l == "tag=winter"), Is.EqualTo(1));
            Assert.That(Array.IndexOf(lines, "tag=none"), Is.GreaterThan(Array.IndexOf(lines, "scene=C")));
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void Localization_SkipsEmptyTextAndKeys()
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.Set("GAMEPLAY_Thermos", "English", "Thermos Flask");
            table.Set("GAMEPLAY_Thermos", "French (France)", "Bouteille isotherme");
            table.GetOrAdd("GAMEPLAY_Unused");

            var json = ModWriters.Localization(table);

            Assert.That(json.Properties().Select(p => p.Name), Is.EqualTo(new[] { "GAMEPLAY_Thermos" }));
            Assert.That(json["GAMEPLAY_Thermos"]["French (France)"].Value<string>(), Is.EqualTo("Bouteille isotherme"));
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void Blueprint_WritesOnlyTheQuantityForItsUnits()
        {
            var bp = ScriptableObject.CreateInstance<Blueprint>();
            bp.name = "BP_Thermos";
            bp.CraftedResult = "GEAR_Thermos";
            bp.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_ScrapMetal", Count = 2, Quantity = 5 });
            bp.RequiredGear.Add(new Blueprint.GearRequirement { Item = "GEAR_Salt", Count = 3, Quantity = 0.5f, Units = RequirementUnits.Kilograms });
            bp.RequiredLiquid.Add(new Blueprint.LiquidRequirement { Liquid = "LIQUID_Kerosene", VolumeInLitres = 0.25f });

            var json = ModWriters.Blueprint(bp);
            var gear = (JArray)json["RequiredGear"];

            Assert.That(json["Name"].Value<string>(), Is.EqualTo("BP_Thermos"));
            Assert.That(gear[0]["Count"].Value<int>(), Is.EqualTo(2));
            Assert.That(gear[0]["Quantity"].Value<float>(), Is.EqualTo(0f));
            Assert.That(gear[1]["Count"].Value<int>(), Is.EqualTo(0));
            Assert.That(gear[1]["Quantity"].Value<float>(), Is.EqualTo(0.5f));
            Assert.That(gear[1]["Units"].Value<string>(), Is.EqualTo("Kilograms"));
            Assert.That(json["RequiredLiquid"][0]["VolumeInLitres"].Value<float>(), Is.EqualTo(0.25f));
            Assert.That(json["AppliedSkill"].Value<string>(), Is.EqualTo("None"));
            Assert.That(json["RequiredTool"].Value<string>(), Is.EqualTo(""));
            UnityEngine.Object.DestroyImmediate(bp);
        }

        [Test]
        public void Recipe_NestsBlueprintData()
        {
            var bp = ScriptableObject.CreateInstance<Blueprint>();
            bp.IsCookingRecipe = true;
            bp.RecipeName = "GAMEPLAY_PineTea";
            bp.CraftedResult = "GEAR_PineTea";

            var json = ModWriters.Recipe(bp);

            Assert.That(json["RecipeName"].Value<string>(), Is.EqualTo("GAMEPLAY_PineTea"));
            Assert.That(json["BlueprintData"]["CraftedResult"].Value<string>(), Is.EqualTo("GEAR_PineTea"));
            Assert.That(json["BlueprintData"]["AppliedSkill"], Is.Null);
            UnityEngine.Object.DestroyImmediate(bp);
        }

        [Test]
        public void Number_IsFixedPointWithoutNegativeZero()
        {
            Assert.That(ModWriters.Number(-0f), Is.EqualTo("0"));
            Assert.That(ModWriters.Number(1e-7f), Is.EqualTo("0"));
            Assert.That(ModWriters.Number(123456.789f), Is.EqualTo("123456.7891"));
            Assert.That(ModWriters.Number(-2.5f), Is.EqualTo("-2.5"));
        }
    }

    public class PackagerTests
    {
        [TestCase("buildinfo.json", "buildinfo.json")]
        [TestCase("auto-mapped\\ThermosFlask.json", "auto-mapped/thermosflask.json")]
        [TestCase("bundle/WarmGear_assets_all.bundle", "bundle/warmgear_assets_all.bundle")]
        [TestCase("gear-spawns/warmgear.txt", "gear-spawns/warmgear.txt")]
        [TestCase("extras/Sounds.bnk", "extras/sounds.bnk")]
        public void AcceptedPaths(string staged, string expected)
        {
            Assert.That(ModPackager.ToEntryName(staged, out var rejection), Is.EqualTo(expected));
            Assert.That(rejection, Is.Null);
        }

        [TestCase("notes.json")]
        [TestCase("auto-mapped/readme.txt")]
        [TestCase("gear-spawns/spawns.json")]
        [TestCase("icon.png")]
        [TestCase("rarities.ir")]
        public void RejectedPaths(string staged)
        {
            Assert.That(ModPackager.ToEntryName(staged, out var rejection), Is.Null);
            Assert.That(rejection, Is.Not.Empty);
        }

        [Test]
        public void DeclaredRootExtensions_AreAllowedAtTheRootOnly()
        {
            var declared = new[] { ".ir" };
            Assert.That(ModPackager.ToEntryName("Rarities.ir", out var rejection, declared), Is.EqualTo("rarities.ir"));
            Assert.That(rejection, Is.Null);
            Assert.That(ModPackager.ToEntryName("extras/rarities.ir", out _, declared), Is.Null);
        }

        [TestCase(".ir", true)]
        [TestCase(".isk", true)]
        [TestCase(".json", false)]
        [TestCase(".DLL", false)]
        [TestCase(".bnk", false)]
        [TestCase("ir", false)]
        [TestCase(".i/r", false)]
        public void RootExtensions_MustBeOnesTheLoaderSkips(string extension, bool allowed)
        {
            Assert.That(ModPackager.IsAllowedRootExtension(extension, out var reason), Is.EqualTo(allowed));
            Assert.That(reason, allowed ? Is.Null : Is.Not.Empty);
        }

        [Test]
        public void Package_RefusesARootExtensionTheLoaderReads()
        {
            var staging = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcsdk-pack-" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(staging);
                System.IO.File.WriteAllText(System.IO.Path.Combine(staging, "buildinfo.json"), "{}");
                var archive = System.IO.Path.Combine(staging, "out", "Test.modcomponent");

                var e = Assert.Throws<InvalidOperationException>(() => ModPackager.Package(staging, archive, new[] { ".json" }));
                Assert.That(e.Message, Does.Contain(".json"));
            }
            finally
            {
                if (System.IO.Directory.Exists(staging)) System.IO.Directory.Delete(staging, true);
            }
        }

        [Test]
        public void Package_PutsBuildInfoAndBundlesFirst()
        {
            var staging = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcsdk-pack-" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (var file in new[] { "localizations/a.json", "auto-mapped/b.json", "bundle/c.bundle", "buildinfo.json", "bundle/catalog.json" })
                {
                    var path = System.IO.Path.Combine(staging, file);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                    System.IO.File.WriteAllText(path, "{}");
                }

                var archive = System.IO.Path.Combine(staging, "out", "Test.modcomponent");
                var entries = ModPackager.Package(staging, archive);

                Assert.That(entries, Is.EqualTo(new[] { "buildinfo.json", "bundle/c.bundle", "bundle/catalog.json", "auto-mapped/b.json", "localizations/a.json" }));
                using var stream = System.IO.File.OpenRead(archive);
                using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
                Assert.That(zip.Entries.Select(e => e.FullName), Is.EqualTo(entries));
            }
            finally
            {
                if (System.IO.Directory.Exists(staging)) System.IO.Directory.Delete(staging, true);
            }
        }
    }
}
