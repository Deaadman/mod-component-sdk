using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ModComponent.SDK.Editor.Build.Export
{
    /// <summary>
    /// Adjustments for known ModComponent loader bugs, applied to an item's JSON after serialization so the
    /// item behaves in game the way it reads in the editor. Each quirk names the loader version it applies
    /// to; when the loader is fixed, only this table changes.
    /// </summary>
    public static class LoaderQuirks
    {
        /// <summary>Vanilla item the loader can always load. Used where it must load *something*.</summary>
        public const string SafeCookingResult = "GEAR_PinnacleCanPeaches";

        private sealed class Quirk
        {
            public string ModComponentVersion;
            public string ExportName;
            public Func<JObject, string> Apply;
        }

        private static readonly List<Quirk> Quirks = new()
        {
            // ClothingMapper assigns FirstPersonPrefabMale to the female slot and vice versa.
            new Quirk
            {
                ModComponentVersion = "7.0.0",
                ExportName = "ModClothingComponent",
                Apply = json =>
                {
                    var male = json["FirstPersonPrefabMale"]?.ToString() ?? string.Empty;
                    var female = json["FirstPersonPrefabFemale"]?.ToString() ?? string.Empty;
                    if (male == female) return null;
                    json["FirstPersonPrefabMale"] = female;
                    json["FirstPersonPrefabFemale"] = male;
                    return "swapped FirstPersonPrefabMale/Female to counter the loader assigning them crossed over";
                }
            },
            // ModCookableComponent always loads CookingResult, even with Cooking off, and fails on an empty name.
            new Quirk { ModComponentVersion = "7.0.0", ExportName = "ModCookableComponent", Apply = FillCookingResult },
            new Quirk { ModComponentVersion = "7.0.0", ExportName = "ModFoodComponent", Apply = FillCookingResult },
        };

        /// <summary>Applies every quirk for the export name. Returns a note per change made, for the build log.</summary>
        public static IEnumerable<string> Apply(string exportName, JObject json, string modComponentVersion = SdkInfo.TargetModComponentVersion)
        {
            foreach (var quirk in Quirks)
            {
                if (quirk.ExportName != exportName || quirk.ModComponentVersion != modComponentVersion) continue;
                var note = quirk.Apply(json);
                if (note != null) yield return note;
            }
        }

        private static string FillCookingResult(JObject json)
        {
            if (!string.IsNullOrEmpty(json["CookingResult"]?.ToString())) return null;
            json["CookingResult"] = SafeCookingResult;
            return $"set the empty CookingResult to {SafeCookingResult}; the loader always loads it, and it is unused while Cooking is off";
        }
    }
}
