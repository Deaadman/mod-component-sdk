#if UNITY_EDITOR
using ModComponent.Components;
using ModComponent.Editor.SDK;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor.API
{
    [CustomEditor(typeof(ModGenericComponent), true)]
    internal class EditorComponents : EditorBase
    {
        protected override Tab[] GetTabs()
        {
            return new[] { Tab.Common, Tab.Audio, Tab.Inspect, Tab.About };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            DefinePropertyUnits();
            DefinePropertyDisplayNames();
            DefineTabDrawMethods();
        }

        private void DefinePropertyUnits()
        {
            propertyUnits = new Dictionary<string, string>()
            {
                { "WeightKG", "KG" },
                { "DaysToDecay", "DAYS" },
                { "MaxHP", "HP" },

                { "ConditionGainPerHour", "% / HR" },
                { "AdditionalConditionGainPerHour", "% / HR" },
                { "WarmthBonusCelsius", "°C" },
                { "DegradePerHour", "% / HR" },

                { "GutWeightKgPerUnit", "KG" },
                { "HideWeightKgPerUnit", "KG" },
                { "MeatAvailableMinKG", "KG" },
                { "MeatAvailableMaxKG", "KG" },

                { "SurveyGameMinutes", "MINS" },
                { "SurveyRealSeconds", "SECS" },
                { "SurveySkillExtendedHours", "HRS" },

                { "DaysToDecayWornOutside", "DAYS" },
                { "DaysToDecayWornInside", "DAYS" },
                { "Warmth", "°C" },
                { "WarmthWhenWet", "°C" },
                { "Windproof", "°C" },
                { "Waterproofness", "%" },
                { "Toughness", "%" },
                { "SprintBarReduction", "%" },
                { "DecreaseAttackChance", "%" },
                { "IncreaseFleeChance", "%" },
                { "HoursToDryNearFire", "HRS" },
                { "HoursToDryWithoutFire", "HRS" },
                { "HoursToFreeze", "HRS" },

                { "CookingMinutes", "MINS" },
                { "CookingUnitsRequired", "UNITS" },
                { "CookingWaterRequired", "L" },
                { "BurntMinutes", "MINS" },

                { "Capacity", "L" },

                { "InstantHealing", "%" },
                { "TimeToUseSeconds", "SECS" },
                { "UnitsPerUse", "UNITS" },

                { "DaysToDecayOutdoors", "DAYS" },
                { "DaysToDecayIndoors", "DAYS" },
                { "Calories", "CAL" },
                { "EatingTime", "SECS" },
                { "ThirstEffect", "%" },
                { "FoodPoisoning", "%" },
                { "FoodPoisoningLowCondition", "%" },
                { "ParasiteRiskIncrements", "%" },
                { "ConditionRestBonus", "% / HR" },
                { "ConditionRestMinutes", "MINS" },
                { "InstantRestChange", "%" },
                { "RestFactorMinutes", "MINS" },
                { "InstantColdChange", "%" },
                { "ColdFactorMinutes", "MINS" },
                { "AlcoholPercentage", "%" },
                { "AlcoholUptakeMinutes", "MINS" },

                { "LiquidCapacityLiters", "L" },
                { "LiquidLiters", "L" },

                { "CapacityKG", "KG" },
                { "ChanceFull", "%" },

                { "LitersPurify", "L" },
                { "ProgressBarDurationSeconds", "SECS" },

                { "TimeRequirementHours", "HRS" },

                { "DegradeOnUse", "%" },
                { "CraftingTimeMultiplier", "%" },
                { "DegradePerHourCrafting", "% / HR" },
                { "BreakDownTimeMultiplier", "%" },
                { "ForceLocks", "" },
                { "IceFishingHoleDegradeOnUse", "%" },
                { "IceFishingHoleMinutes", "MINS" },
                { "MinutesPerKgMeat", "MINS" },
                { "MinutesPerKgFrozenMeat", "MINS" },
                { "MinutesPerHide", "MINS" },
                { "MinutesPerGut", "MINS" },
                { "DegradePerHourHarvesting", "% / HR" },
                { "DamageMultiplier", "%" },
                { "FleeChanceMultiplier", "%" },
                { "TapMultiplier", "%" },
                { "BleedoutMultiplier", "%" }
            };
        }

        private void DefinePropertyDisplayNames()
        {
            propertyDisplayNames = new Dictionary<string, string>()
            {
                { "DisplayNameLocalizationId", "Display Name Localization Key"},
                { "DescriptionLocalizatonId", "Description Localization Key"},
                { "InventoryActionLocalizationId", "Inventory Action Localization Key"},
                { "WeightKG", "Weight" },
                { "DaysToDecay", "Decay" },
                { "MaxHP", "Max Health" },

                { "ConditionGainPerHour", "Condition Gain" },
                { "AdditionalConditionGainPerHour", "Additional Condition Gain" },
                { "WarmthBonusCelsius", "Warmth Bonus" },
                { "DegradePerHour", "Degrade" },

                { "GutWeightKgPerUnit", "Gut Weight Unit" },
                { "HideWeightKgPerUnit", "Hide Weight Unit" },
                { "MeatAvailableMinKG", "Meat Available Minimum" },
                { "MeatAvailableMaxKG", "Meat Available Maximum" },

                { "SurveyGameMinutes", "Survey Time" },
                { "SurveyRealSeconds", "Survey Real Time" },
                { "SurveySkillExtendedHours", "Survey Skill Extended" },

                { "MinLayer", "Minimum Layer" },
                { "MaxLayer", "Maximum Layer" },
                { "DaysToDecayWornOutside", "Decay Worn Outside" },
                { "DaysToDecayWornInside", "Decay Worn Inside" },
                { "HoursToDryNearFire", "Dry Near Fire" },
                { "HoursToDryWithoutFire", "Dry Without Fire" },
                { "HoursToFreeze", "Freeze" },

                { "HudMessageLocalizationId", "HUD Message Localization Key"},
                { "NarrativeTextLocalizationId", "Narrative Text Localization Key"},

                { "Cooking", "Can Be Cooked" },
                { "CookingMinutes", "Cooking Time" },
                { "CookingUnitsRequired", "Amount Required" },
                { "CookingWaterRequired", "Water Amount Required" },
                { "CookingResult", "Result" },
                { "BurntMinutes", "Time Until Burnt" },

                { "ProgressBarMessage", "Progress Bar Localization Key" },
                { "RemedyText", "Remedy Localization Key" },
                { "InstantHealing", "Restored Condition" },
                { "TimeToUseSeconds", "Use Time" },
                { "UnitsPerUse", "Amount Per Use" },

                { "DaysToDecayOutdoors", "Decay Indoors" },
                { "DaysToDecayIndoors", "Decay Outdoors" },
                { "ConditionRestMinutes", "Condition Rest" },
                { "RestFactorMinutes", "Rest Factor" },
                { "ColdFactorMinutes", "Cold Factor" },
                { "AlcoholUptakeMinutes", "Alcohol Uptake" },

                { "LiquidCapacityLiters", "Liquid Capacity" },
                { "LiquidLiters", "Initial Amount" },

                { "CapacityKG", "Capacity" },

                { "LitersPurify", "Purify" },
                { "ProgressBarDurationSeconds", "Progress Bar Duration" },
                { "ProgressBarLocalizationID", "Progress Bar Localization Key" },

                { "TimeRequirementHours", "Time Requirement" },

                { "DegradePerHourCrafting", "Degrade Crafting" },
                { "IceFishingHoleMinutes", "Ice Fishing Hole Time" },
                { "MinutesPerKgMeat", "Per Kilogram Meat" },
                { "MinutesPerKgFrozenMeat", "Per Kilogram Frozen Meat" },
                { "MinutesPerHide", "Per Hide" },
                { "MinutesPerGut", "Per Gut" },
                { "DegradePerHourHarvesting", "Degrade Harvesting" }
            };
        }

        private void DefineTabDrawMethods()
        {
            tabDrawMethods = new Dictionary<Tab, Action>
            {
                { Tab.Common, () => DrawCommonFields() },
                { Tab.Audio, () => DrawAudioFields() },
                { Tab.Inspect, () => DrawInspectFields() },
                { Tab.About, () => DrawAboutFields() }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            foreach (Tab tab in GetTabs())
            {
                DrawTabButton(tab, tab.ToString());
            }
            GUILayout.EndHorizontal();

            if (tabDrawMethods.TryGetValue(selectedTab, out Action drawMethod))
            {
                drawMethod();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommonFields()
        {
            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawCustomHeading("Generic Component Properties");

            string[] baseClassProperties = new string[]
            {
                "DisplayNameLocalizationId",
                "DescriptionLocalizatonId",
                "InventoryActionLocalizationId",
                "WeightKG",
                "DaysToDecay",
                "MaxHP",
                "InitialCondition",
                "InventoryCategory"
            };
            DrawFields(baseClassProperties);

            if (target.GetType() == typeof(ModAmmoComponent) || target.GetType().IsSubclassOf(typeof(ModAmmoComponent)))
            {
                DrawCustomHeading("Ammo Component Properties");
                DrawFields(new string[] { "AmmoForGunType" });
            }

            if (target.GetType() == typeof(ModBedComponent) || target.GetType().IsSubclassOf(typeof(ModBedComponent)))
            {
                DrawCustomHeading("Bed Component Properties");
                DrawFields(new string[] {
                    "ConditionGainPerHour",
                    "AdditionalConditionGainPerHour",
                    "WarmthBonusCelsius",
                    "DegradePerHour",
                    "BearAttackModifier",
                    "WolfAttackModifier",
                    "PackedMesh",
                    "UsableMesh"
                });
            }

            if (target.GetType() == typeof(ModBodyHarvestComponent) || target.GetType().IsSubclassOf(typeof(ModBodyHarvestComponent)))
            {
                DrawCustomHeading("Body Harvest Component Properties");
                DrawFields(new string[] {
                    "CanCarry",
                    "GutPrefab",
                    "GutQuantity",
                    "GutWeightKgPerUnit",
                    "HidePrefab",
                    "HideQuantity",
                    "HideWeightKgPerUnit",
                    "MeatPrefab",
                    "MeatAvailableMinKG",
                    "MeatAvailableMaxKG",
                    "GutLabelOverride",
                    "HideLabelOverride",
                    "MeatLabelOverride"
                });
            }

            if (target.GetType() == typeof(ModCharcoalComponent) || target.GetType().IsSubclassOf(typeof(ModCharcoalComponent)))
            {
                DrawCustomHeading("Charcoal Component Properties");
                DrawFields(new string[] {
                    "SurveyGameMinutes",
                    "SurveyRealSeconds",
                    "SurveySkillExtendedHours"
                });
            }

            if (target.GetType() == typeof(ModClothingComponent) || target.GetType().IsSubclassOf(typeof(ModClothingComponent)))
            {
                DrawCustomHeading("Clothing Component Properties");
                DrawFields(new string[] {
                    "Region",
                    "MinLayer",
                    "MaxLayer",
                    "Footwear",
                    "DaysToDecayWornOutside",
                    "DaysToDecayWornInside",
                    "Warmth",
                    "WarmthWhenWet",
                    "Windproof",
                    "Waterproofness",
                    "Toughness",
                    "SprintBarReduction",
                    "DecreaseAttackChance",
                    "IncreaseFleeChance",
                    "HoursToDryNearFire",
                    "HoursToDryWithoutFire",
                    "HoursToFreeze",
                    "MainTexture",
                    "BlendTexture",
                    "DrawLayer",
                    "ImplementationType",
                    "PreventAllDamageFromSource",
                    "FirstPersonPrefabMale",
                    "FirstPersonPrefabFemale"
                });
            }

            if (target.GetType() == typeof(ModCollectibleComponent) || target.GetType().IsSubclassOf(typeof(ModCollectibleComponent)))
            {
                DrawCustomHeading("Collectible Component Properties");
                DrawFields(new string[] {
                    "HudMessageLocalizationId",
                    "NarrativeTextLocalizationId",
                    "TextAlignment"
                });
            }

            if (target.GetType() == typeof(ModCookableComponent) || target.GetType().IsSubclassOf(typeof(ModCookableComponent)))
            {
                DrawCustomHeading("Cookable Component Properties");
                DrawFields(new string[] {
                    "Cooking",
                    "CookingMinutes",
                    "CookingUnitsRequired",
                    "CookingWaterRequired",
                    "CookingResult",
                    "BurntMinutes",
                    "Type"
                });
            }

            if (target.GetType() == typeof(ModCookingPotComponent) || target.GetType().IsSubclassOf(typeof(ModCookingPotComponent)))
            {
                DrawCustomHeading("Cooking Pot Component Properties");
                DrawFields(new string[] {
                    "CanCookLiquid",
                    "CanCookGrub",
                    "CanCookMeat",
                    "Capacity",
                    "Template"
                });
            }

            if (target.GetType() == typeof(ModFirstAidComponent) || target.GetType().IsSubclassOf(typeof(ModFirstAidComponent)))
            {
                DrawCustomHeading("First Aid Component Properties");
                DrawFields(new string[] {
                    "ProgressBarMessage",
                    "RemedyText",
                    "InstantHealing",
                    "FirstAidType",
                    "TimeToUseSeconds",
                    "UnitsPerUse"
                });
            }

            if (target.GetType() == typeof(ModFoodComponent) || target.GetType().IsSubclassOf(typeof(ModFoodComponent)))
            {
                DrawCustomHeading("Food Component Properties");
                DrawFields(new string[] {
                    "DaysToDecayOutdoors",
                    "DaysToDecayIndoors",
                    "Calories",
                    "Servings",
                    "EatingTime",
                    "ThirstEffect",
                    "FoodPoisoning",
                    "FoodPoisoningLowCondition",
                    "ParasiteRiskIncrements",
                    "Natural",
                    "Raw",
                    "Drink",
                    "Meat",
                    "Fish",
                    "Canned",
                    "Opening",
                    "OpeningWithCanOpener",
                    "OpeningWithKnife",
                    "OpeningWithHatchet",
                    "OpeningWithSmashing",
                    "AffectCondition",
                    "ConditionRestBonus",
                    "ConditionRestMinutes",
                    "AffectRest",
                    "InstantRestChange",
                    "RestFactorMinutes",
                    "AffectCold",
                    "InstantColdChange",
                    "ColdFactorMinutes",
                    "ContainsAlcohol",
                    "AlcoholPercentage",
                    "AlcoholUptakeMinutes",
                    "VitaminC"
                });
            }

            if (target.GetType() == typeof(ModGenericEquippableComponent) || target.GetType().IsSubclassOf(typeof(ModGenericEquippableComponent)))
            {
                DrawCustomHeading("Equippable Component Properties");
                DrawFields(new string[] {
                    "EquippedModelPrefab",
                    "ImplementationType"
                });
            }

            if (target.GetType() == typeof(ModLiquidComponent) || target.GetType().IsSubclassOf(typeof(ModLiquidComponent)))
            {
                DrawCustomHeading("Liquid Component Properties");
                DrawFields(new string[] {
                    "LiquidType",
                    "LiquidCapacityLiters",
                    "RandomizedQuantity",
                    "LiquidLiters"
                });
            }

            if (target.GetType() == typeof(ModPowderComponent) || target.GetType().IsSubclassOf(typeof(ModPowderComponent)))
            {
                DrawCustomHeading("Powder Component Properties");
                DrawFields(new string[] {
                    "PowderType",
                    "CapacityKG",
                    "ChanceFull"
                });
            }

            if (target.GetType() == typeof(ModPurificationComponent) || target.GetType().IsSubclassOf(typeof(ModPurificationComponent)))
            {
                DrawCustomHeading("Purification Component Properties");
                DrawFields(new string[] {
                    "LitersPurify",
                    "ProgressBarDurationSeconds",
                    "ProgressBarLocalizationID"
                });
            }

            if (target.GetType() == typeof(ModRandomItemComponent) || target.GetType().IsSubclassOf(typeof(ModRandomItemComponent)))
            {
                DrawCustomHeading("Random Item Component Properties");
                DrawFields(new string[] {
                    "ItemNames"
                });
            }

            if (target.GetType() == typeof(ModRandomWeightedItemComponent) || target.GetType().IsSubclassOf(typeof(ModRandomWeightedItemComponent)))
            {
                DrawCustomHeading("Random Weighted Item Component Properties");
                DrawFields(new string[] {
                    "ItemNames",
                    "ItemWeights"
                });
            }

            if (target.GetType() == typeof(ModResearchComponent) || target.GetType().IsSubclassOf(typeof(ModResearchComponent)))
            {
                DrawCustomHeading("Research Component Properties");
                DrawFields(new string[] {
                    "SkillType",
                    "TimeRequirementHours",
                    "SkillPoints",
                    "NoBenefitAtSkillLevel"
                });
            }

            if (target.GetType() == typeof(ModToolComponent) || target.GetType().IsSubclassOf(typeof(ModToolComponent)))
            {
                DrawCustomHeading("Tool Component Properties");
                DrawFields(new string[] {
                    "ToolType",
                    "ToolKind",
                    "DegradeOnUse",
                    "Usage",
                    "SkillBonus",
                    "CraftingTimeMultiplier",
                    "DegradePerHourCrafting",
                    "BreakDown",
                    "BreakDownTimeMultiplier",
                    "ForceLocks",
                    "IceFishingHole",
                    "IceFishingHoleDegradeOnUse",
                    "IceFishingHoleMinutes",
                    "CarcassHarvesting",
                    "MinutesPerKgMeat",
                    "MinutesPerKgFrozenMeat",
                    "MinutesPerHide",
                    "MinutesPerGut",
                    "DegradePerHourHarvesting",
                    "StruggleBonus",
                    "DamageMultiplier",
                    "FleeChanceMultiplier",
                    "TapMultiplier",
                    "CanPuncture",
                    "BleedoutMultiplier",
                });
            }

            GUILayout.EndVertical();
        }

        private void DrawAudioFields()
        {
            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawCustomHeading("Generic Audio Properties");

            string[] baseClassProperties = new string[]
            {
                "PickUpAudio",
                "PutBackAudio",
                "StowAudio",
                "WornOutAudio"
            };
            DrawFields(baseClassProperties);

            if (target.GetType() == typeof(ModBedComponent) || target.GetType().IsSubclassOf(typeof(ModBedComponent)))
            {
                DrawCustomHeading("Bed Audio Properties");
                DrawFields(new string[] {
                    "OpenAudio",
                    "CloseAudio"
                });
            }

            if (target.GetType() == typeof(ModBodyHarvestComponent) || target.GetType().IsSubclassOf(typeof(ModBodyHarvestComponent)))
            {
                DrawCustomHeading("Body Harvest Audio Properties");
                DrawFields(new string[] {
                    "HarvestAudio"
                });
            }

            if (target.GetType() == typeof(ModCharcoalComponent) || target.GetType().IsSubclassOf(typeof(ModCharcoalComponent)))
            {
                DrawCustomHeading("Charcoal Audio Properties");
                DrawFields(new string[] {
                    "SurveyLoopAudio"
                });
            }

            if (target.GetType() == typeof(ModClothingComponent) || target.GetType().IsSubclassOf(typeof(ModClothingComponent)))
            {
                DrawCustomHeading("Clothing Audio Properties");
                DrawFields(new string[] {
                    "MovementSound"
                });
            }

            if (target.GetType() == typeof(ModCookableComponent) || target.GetType().IsSubclassOf(typeof(ModCookableComponent)))
            {
                DrawCustomHeading("Cookable Audio Properties");
                DrawFields(new string[] {
                    "CookingAudio",
                    "StartCookingAudio"
                });
            }

            if (target.GetType() == typeof(ModFirstAidComponent) || target.GetType().IsSubclassOf(typeof(ModFirstAidComponent)))
            {
                DrawCustomHeading("First Aid Audio Properties");
                DrawFields(new string[] {
                    "UseAudio"
                });
            }

            if (target.GetType() == typeof(ModFoodComponent) || target.GetType().IsSubclassOf(typeof(ModFoodComponent)))
            {
                DrawCustomHeading("Food Audio Properties");
                DrawFields(new string[] {
                    "EatingAudio",
                    "EatingPackagedAudio"
                });
            }

            if (target.GetType() == typeof(ModGenericEquippableComponent) || target.GetType().IsSubclassOf(typeof(ModGenericEquippableComponent)))
            {
                DrawCustomHeading("Equippable Audio Properties");
                DrawFields(new string[] {
                    "EquippingAudio"
                });
            }

            if (target.GetType() == typeof(ModPurificationComponent) || target.GetType().IsSubclassOf(typeof(ModPurificationComponent)))
            {
                DrawCustomHeading("Purification Audio Properties");
                DrawFields(new string[] {
                    "PurifyAudio"
                });
            }

            if (target.GetType() == typeof(ModResearchComponent) || target.GetType().IsSubclassOf(typeof(ModResearchComponent)))
            {
                DrawCustomHeading("Research Audio Properties");
                DrawFields(new string[] {
                    "ReadAudio"
                });
            }

            if (target.GetType() == typeof(ModToolComponent) || target.GetType().IsSubclassOf(typeof(ModToolComponent)))
            {
                DrawCustomHeading("Tool Audio Properties");
                DrawFields(new string[] {
                    "ForceLockAudio",
                    "IceFishingHoleAudio"
                });
            }

            GUILayout.EndVertical();
        }

        private void DrawInspectFields()
        {
            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawCustomHeading("Generic Inspect Properties");

            DrawFields(new string[] {
                "InspectOnPickup",
                "InspectDistance",
                "InspectAngles",
                "InspectOffset",
                "InspectScale",
                "NormalModel",
                "InspectModel"
            });

            GUILayout.EndVertical();
        }

        private void DrawFields(string[] propertyNames)
        {
            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            foreach (string name in propertyNames)
            {
                if (serializedProperties.TryGetValue(name, out SerializedProperty property))
                {
                    string displayName = propertyDisplayNames.ContainsKey(name) ? propertyDisplayNames[name] : ObjectNames.NicifyVariableName(name);
                    if (propertyUnits.TryGetValue(name, out string unit))
                    {
                        ModComponentEditorStyles.DrawPropertyWithUnit(property, unit, new GUIContent(displayName));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property, new GUIContent(displayName));
                    }
                }
            }
            GUILayout.EndVertical();
        }
    }
}
#endif