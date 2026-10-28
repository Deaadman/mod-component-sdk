using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Modules;

namespace ModComponent.SDK.Editor.Validation.Rules
{
    /// <summary>Values the loader accepts but that break or silently do nothing in game.</summary>
    public sealed class ModuleValuesRule : ItemRule
    {
        public override string Id => "item.module-values";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            foreach (var module in item.AllModules)
            {
                switch (module)
                {
                    case CookableComponent cookable when cookable.Cooking && cookable.CookingResult.IsEmpty:
                        yield return Error("Cooking is on, so a cooking result is needed.", item, "Cooking › Cooking result");
                        break;

                    case CookingPotComponent pot when pot.Template.IsEmpty:
                        yield return Error("A cooking pot needs a vanilla template pot, such as GEAR_CookingPot.", item, "Cooking Pot › Template");
                        break;

                    case ClothingComponent clothing:
                        if (clothing.HoursToDryNearFire <= 0 || clothing.HoursToDryWithoutFire <= 0 || clothing.HoursToFreeze <= 0)
                            yield return Error("Drying and freezing hours must be above 0; the game divides by them.", item, "Clothing");
                        if (clothing.MinLayer > clothing.MaxLayer)
                            yield return Error("Min layer is outside max layer.", item, "Clothing › Layers");
                        break;

                    case LiquidComponent liquid when liquid.LiquidLiters > liquid.LiquidCapacityLiters:
                        yield return Warning("The starting amount is more than the capacity; the game clamps it.", item, "Liquid › Liquid liters");
                        break;

                    case FoodComponent food when food.EatingTime > 10:
                        yield return Info("Eating time above 10 seconds isn't clamped by ModComponent 7.0.0 the way vanilla food is.", item, "Food › Eating time");
                        break;

                    case RandomItemComponent random when random.ItemNames.Count(r => !r.IsEmpty) == 0:
                        yield return Error("Add at least one item to choose from.", item, "Random Item › Item names");
                        break;

                    case RandomWeightedItemComponent weighted when weighted.Items.Count(p => p != null && !p.Item.IsEmpty) == 0:
                        yield return Error("Add at least one item to choose from.", item, "Random Weighted Item › Items");
                        break;

                    case EvolveBehaviour evolve when evolve.TargetItemName.IsEmpty:
                        yield return Error("Set the item this evolves into.", item, "Evolve › Target item");
                        break;

                    case StackableBehaviour stack when stack.ShareStackWithGear.Any(r => r.IsEmpty):
                        yield return Error("\"Share stack with gear\" has an empty entry; ModComponent fails on it.", item, "Stackable › Share stack with gear");
                        break;

                    case SharpenableBehaviour sharpen when sharpen.MinutesMin > sharpen.MinutesMax || sharpen.ConditionMin > sharpen.ConditionMax:
                        yield return Warning("A minimum is larger than its maximum.", item, "Sharpenable");
                        break;
                }

                foreach (var issue in CheckPairs(item, module))
                    yield return issue;
            }
        }

        /// <summary>Entries of [SplitArrays] lists export as two arrays; both halves must be filled in.</summary>
        private IEnumerable<ValidationIssue> CheckPairs(GearItem item, ModuleData module)
        {
            foreach (var found in ReferenceWalker.Walk(item).Where(r => r.Module == module && r.Field.FieldType == typeof(List<GearCount>)))
            {
                if (found.Reference.IsEmpty)
                    yield return Error("An entry has no item.", item, found.Path);
            }

            foreach (var field in Build.Export.ModuleSerializer.GetExportedFields(module.GetType()).Where(f => f.FieldType == typeof(List<GearCount>)))
            {
                var list = (List<GearCount>)field.GetValue(module);
                if (list == null) continue;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) yield return Error("An entry is empty.", item, $"{field.Name}[{i}]");
                    else if (list[i].Count < 1) yield return Error("Amounts must be at least 1.", item, $"{field.Name}[{i}]");
                }
            }
        }
    }
}
