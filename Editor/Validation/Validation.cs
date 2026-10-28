using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Build;
using ModComponent.SDK.Editor.Catalog;
using UnityEditor;

namespace ModComponent.SDK.Editor.Validation
{
    public enum Severity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>One problem found by a rule, pointing at the asset (and field) it is about.</summary>
    public sealed class ValidationIssue
    {
        public ValidationIssue(string ruleId, Severity severity, string message, UnityEngine.Object target = null,
            string field = null, string fixLabel = null, Action fix = null)
        {
            RuleId = ruleId;
            Severity = severity;
            Message = message;
            Target = target;
            Field = field;
            FixLabel = fixLabel;
            Fix = fix;
        }

        public string RuleId { get; }
        public Severity Severity { get; }
        public string Message { get; }

        /// <summary>The asset the issue is on: the mod, an item, a blueprint, a table or a prefab.</summary>
        public UnityEngine.Object Target { get; }

        /// <summary>Where on the target, e.g. "Repairable › Materials". Optional.</summary>
        public string Field { get; }

        public string FixLabel { get; }
        public Action Fix { get; }
        public bool HasFix => Fix != null;

        public override string ToString() =>
            $"{Severity}: {(Target != null ? Target.name + ": " : string.Empty)}{(Field != null ? Field + ": " : string.Empty)}{Message}";
    }

    /// <summary>What a rule can see while checking a mod.</summary>
    public sealed class ValidationContext
    {
        public ValidationContext(Mod mod, GameDataCatalog catalog, IEnumerable<Mod> otherMods = null, bool forBuild = false)
        {
            Mod = mod;
            Catalog = catalog;
            ForBuild = forBuild;
            OtherMods = (otherMods ?? Enumerable.Empty<Mod>()).Where(m => m != null && m != mod).ToList();
            ModItems = mod.Items.Where(i => i != null).GroupBy(i => i.GearName).ToDictionary(g => g.Key ?? string.Empty, g => g.First());
            OtherModItems = new HashSet<string>(OtherMods.SelectMany(m => m.Items).Where(i => i != null).Select(i => i.GearName));
        }

        public Mod Mod { get; }
        public GameDataCatalog Catalog { get; }

        /// <summary>True when checking right before a build; project-environment rules only report then.</summary>
        public bool ForBuild { get; }

        public IReadOnlyList<Mod> OtherMods { get; }
        public IReadOnlyDictionary<string, GearItem> ModItems { get; }
        public ISet<string> OtherModItems { get; }

        public bool IsKnownGear(string id) =>
            ModItems.ContainsKey(id) || OtherModItems.Contains(id) || Catalog.Contains(CatalogKind.Gear, id);
    }

    /// <summary>
    /// A check over a mod. Subclasses are found automatically, so other packages can add rules.
    /// Yield an issue per problem; return nothing when all is well.
    /// </summary>
    public abstract class ValidationRule
    {
        /// <summary>Stable id shown with each issue, e.g. "item.prefab-name".</summary>
        public abstract string Id { get; }

        public abstract IEnumerable<ValidationIssue> Check(ValidationContext context);

        protected ValidationIssue Error(string message, UnityEngine.Object target = null, string field = null, string fixLabel = null, Action fix = null) =>
            new(Id, Severity.Error, message, target, field, fixLabel, fix);

        protected ValidationIssue Warning(string message, UnityEngine.Object target = null, string field = null, string fixLabel = null, Action fix = null) =>
            new(Id, Severity.Warning, message, target, field, fixLabel, fix);

        protected ValidationIssue Info(string message, UnityEngine.Object target = null, string field = null, string fixLabel = null, Action fix = null) =>
            new(Id, Severity.Info, message, target, field, fixLabel, fix);
    }

    public sealed class ValidationResult
    {
        public ValidationResult(IReadOnlyList<ValidationIssue> issues) => Issues = issues;

        public IReadOnlyList<ValidationIssue> Issues { get; }
        public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == Severity.Error);
        public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == Severity.Warning);
        public bool HasErrors => Errors.Any();

        /// <summary>Issues about one asset, e.g. for an item's issue panel.</summary>
        public IEnumerable<ValidationIssue> For(UnityEngine.Object target) => Issues.Where(i => i.Target == target);
    }

    /// <summary>Runs every <see cref="ValidationRule"/>. The build pipeline calls it before building.</summary>
    public static class ModValidator
    {
        private static IReadOnlyList<ValidationRule> _rules;

        public static IReadOnlyList<ValidationRule> Rules => _rules ??= TypeCache.GetTypesDerivedFrom<ValidationRule>()
            .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (ValidationRule)Activator.CreateInstance(t))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToArray();

        public static ValidationResult Validate(Mod mod, bool forBuild = false, GameDataCatalog catalog = null, IEnumerable<Mod> otherMods = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            var context = new ValidationContext(mod, catalog ?? GameDataCatalog.Default, otherMods ?? FindAllMods(), forBuild);
            var issues = new List<ValidationIssue>();
            foreach (var rule in Rules)
            {
                try
                {
                    issues.AddRange(rule.Check(context).Where(i => i != null));
                }
                catch (Exception e)
                {
                    issues.Add(new ValidationIssue(rule.Id, Severity.Error, $"The {rule.Id} check crashed: {e.Message}", mod));
                }
            }

            return new ValidationResult(issues.OrderByDescending(i => i.Severity).ToList());
        }

        private static List<Mod> _allMods;
        private static bool _watchingProject;

        /// <summary>Every Mod asset in the project. Cached until the project changes.</summary>
        public static IReadOnlyList<Mod> FindAllMods()
        {
            if (_allMods != null && _allMods.All(m => m != null)) return _allMods;
            if (!_watchingProject)
            {
                EditorApplication.projectChanged += () => _allMods = null;
                _watchingProject = true;
            }

            _allMods = AssetDatabase.FindAssets("t:" + nameof(Mod))
                .Select(g => AssetDatabase.LoadAssetAtPath<Mod>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null)
                .ToList();
            return _allMods;
        }

        public static BuildMessage ToBuildMessage(ValidationIssue issue) => new(issue.Severity switch
        {
            Severity.Error => BuildMessageSeverity.Error,
            Severity.Warning => BuildMessageSeverity.Warning,
            _ => BuildMessageSeverity.Info
        }, $"[{issue.RuleId}] " + issue);
    }
}
