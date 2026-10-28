using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Build.Export;
using UnityEditor;

namespace ModComponent.SDK.Editor.Validation.Rules
{
    /// <summary>Project settings a build depends on. Reported only when building, so editing stays quiet.</summary>
    public sealed class ProjectSetupRule : ValidationRule
    {
        public override string Id => "project.setup";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            if (!context.ForBuild) yield break;

            if (!UnityVersionGuard.IsSupported)
                yield return Error(UnityVersionGuard.Message);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                yield return Error("The Long Dark is a 64-bit Windows game, and this project builds for " +
                                   $"{EditorUserBuildSettings.activeBuildTarget}.", fixLabel: "Switch to Windows",
                    fix: () => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64));

            if (!ProjectSetup.IsJsonCatalogEnabled)
                yield return Error(ProjectSetup.JsonCatalogMessage, fixLabel: "Turn on JSON catalogs", fix: ProjectSetup.EnableJsonCatalog);
        }
    }

    public sealed class ModIdentityRule : ValidationRule
    {
        private static readonly Regex SemVer = new(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$");

        public override string Id => "mod.identity";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            var mod = context.Mod;

            if (string.IsNullOrWhiteSpace(mod.DisplayName))
                yield return Error("The mod has no name.", mod, "Name");

            if (!Mod.IsValidId(mod.Id))
            {
                var suggested = Mod.ToId(mod.DisplayName);
                yield return Error($"Id \"{mod.Id}\" may only use letters, digits, '-' and '_'. It names the .modcomponent file.",
                    mod, "Id", Mod.IsValidId(suggested) ? $"Use \"{suggested}\"" : null,
                    Mod.IsValidId(suggested) ? () => Edit(mod, m => m.Id = suggested) : null);
            }

            if (string.IsNullOrWhiteSpace(mod.Author))
                yield return Warning("No author is set. It is shown in ModComponent's log when the mod loads.", mod, "Author");

            if (!SemVer.IsMatch(mod.Version ?? string.Empty))
                yield return Warning($"Version \"{mod.Version}\" isn't in the form 1.2.3.", mod, "Version");

            foreach (var other in context.OtherMods.Where(o => string.Equals(o.Id, mod.Id, System.StringComparison.OrdinalIgnoreCase)))
                yield return Error($"{other.name} uses the same id. Both would build to {mod.Id}.modcomponent.", mod, "Id");

            foreach (var required in mod.RequiredMods.Where(r => !string.IsNullOrWhiteSpace(r)))
                if (string.Equals(required.Replace(".modcomponent", string.Empty).Trim(), mod.Id, System.StringComparison.OrdinalIgnoreCase))
                    yield return Error("The mod lists itself as a required mod.", mod, "Required mods");
        }

        internal static void Edit<T>(T asset, System.Action<T> change) where T : UnityEngine.Object
        {
            Undo.RecordObject(asset, "Fix " + asset.name);
            change(asset);
            EditorUtility.SetDirty(asset);
        }
    }

    public sealed class ModContentRule : ValidationRule
    {
        public override string Id => "mod.content";

        public override IEnumerable<ValidationIssue> Check(ValidationContext context)
        {
            var mod = context.Mod;

            if (mod.Items.Any(i => i == null))
                yield return Error("The item list has empty slots.", mod, "Items", "Remove empty slots",
                    () => ModIdentityRule.Edit(mod, m => m.Items.RemoveAll(i => i == null)));

            if (mod.Blueprints.Any(b => b == null))
                yield return Error("The blueprint list has empty slots.", mod, "Blueprints", "Remove empty slots",
                    () => ModIdentityRule.Edit(mod, m => m.Blueprints.RemoveAll(b => b == null)));

            foreach (var group in mod.Items.Where(i => i != null).GroupBy(ModWriters.ItemFileName, System.StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                yield return Error($"{string.Join(", ", group.Select(i => i.name))} share the gear name {group.First().GearName}. " +
                                   "Gear names must be unique.", group.First(), "Gear name");

            foreach (var item in mod.Items.Where(i => i != null && context.OtherModItems.Contains(i.GearName)))
                yield return Error($"{item.GearName} is also an item in another mod in this project; the mod loaded last would replace it.", item, "Gear name");

            foreach (var item in mod.Items.Where(i => i != null).GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key))
                yield return Error($"{item.name} is listed more than once.", mod, "Items", "Remove duplicates",
                    () => ModIdentityRule.Edit(mod, m =>
                    {
                        var distinct = m.Items.Distinct().ToList();
                        m.Items.Clear();
                        m.Items.AddRange(distinct);
                    }));

            if (mod.Items.All(i => i == null) && mod.Blueprints.All(b => b == null) && (mod.Spawns == null || mod.Spawns.IsEmpty))
                yield return Warning("The mod has no items, blueprints or spawns, so the build would be empty.", mod);

            var archiveSafeExtensions = new[] { ".bnk" };
            foreach (var extra in mod.ExtraFiles.Where(f => f != null))
            {
                var extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(extra)).ToLowerInvariant();
                if (!archiveSafeExtensions.Contains(extension))
                    yield return Error($"{extra.name}{extension}: only Wwise .bnk sound banks can be added as extra files.", mod, "Extra files");
            }

            foreach (var integration in mod.Integrations.Where(i => i != null))
                if (Integrations.IntegrationExporters.For(integration.GetType()) == null)
                    yield return Warning($"No exporter is installed for {integration.DisplayName}, so its data won't be written.", mod, "Integrations");
        }
    }
}
