using System;

namespace ModComponent.SDK.Modules
{
    /// <summary>What part of a ModComponent item a module describes.</summary>
    public enum ModuleKind
    {
        Component,
        Behaviour,
        Modification
    }

    /// <summary>
    /// Marks a concrete module class and names the JSON key ModComponent reads it from,
    /// e.g. <c>[Module("ModFoodComponent")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ModuleAttribute : Attribute
    {
        public ModuleAttribute(string exportName) => ExportName = exportName;

        /// <summary>The top-level key in the item's auto-mapped JSON.</summary>
        public string ExportName { get; }

        /// <summary>Name shown in the editor. Defaults to the class name without its suffix.</summary>
        public string DisplayName { get; set; }

        /// <summary>Group used in the "Add module" menu.</summary>
        public string Category { get; set; }

        /// <summary>One-line summary shown in menus and docs.</summary>
        public string Description { get; set; }
    }

    /// <summary>Writes the field under a different JSON key than its C# name.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ExportNameAttribute : Attribute
    {
        public ExportNameAttribute(string name) => Name = name;
        public string Name { get; }
    }

    /// <summary>Unit label drawn next to a numeric field, e.g. "kg" or "min".</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnitAttribute : Attribute
    {
        public UnitAttribute(string unit) => Unit = unit;
        public string Unit { get; }
    }

    /// <summary>Inspector tab the field is drawn on. Fields without one go on the module's own tab.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TabAttribute : Attribute
    {
        public TabAttribute(string name) => Name = name;
        public string Name { get; }
    }

    /// <summary>
    /// Exports a list of two-field entries as two parallel JSON arrays,
    /// e.g. a list of (item, count) as <c>YieldNames</c> and <c>YieldCounts</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SplitArraysAttribute : Attribute
    {
        public SplitArraysAttribute(string firstKey, string secondKey)
        {
            FirstKey = firstKey;
            SecondKey = secondKey;
        }

        public string FirstKey { get; }
        public string SecondKey { get; }
    }

    /// <summary>Modules ModComponent will not load alongside this one; only the first in its order is used.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ConflictsWithAttribute : Attribute
    {
        public ConflictsWithAttribute(params Type[] modules) => Modules = modules;
        public Type[] Modules { get; }
    }
}
