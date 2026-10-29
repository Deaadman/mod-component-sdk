using System.Collections.Generic;
using ModComponent.SDK.Editor.Validation;
using ModComponent.SDK.Editor.Validation.Rules;

namespace ModComponent.SDK.Editor.Migration
{
    /// <summary>
    /// 1.x Mod*Component scripts left on an item prefab aren't in the game, so they would load as missing
    /// scripts. 2.0 keeps item data on the GearItem asset instead.
    /// </summary>
    public sealed class LegacyComponentsRule : ItemRule
    {
        public override string Id => "item.legacy-components";

        protected override IEnumerable<ValidationIssue> CheckItem(GearItem item, ValidationContext context)
        {
            if (!V1Importer.HasLegacyComponents(item.Prefab)) yield break;
            yield return Error("The prefab still has SDK 1.x scripts. They would load as missing scripts in game; " +
                               "the item's data now lives on this asset.", item, "Model prefab", "Remove 1.x scripts",
                () => V1Importer.RemoveLegacyComponents(item.Prefab));
        }
    }
}
