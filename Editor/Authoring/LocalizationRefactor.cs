using System.Linq;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.References;
using UnityEditor;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>Renames a localization key everywhere a mod uses it, so renaming never breaks an item.</summary>
    public static class LocalizationRefactor
    {
        /// <returns>The number of fields that were updated outside the table.</returns>
        public static int RenameKey(Mod mod, string oldKey, string newKey)
        {
            var table = mod.Localization;
            if (table == null || string.IsNullOrWhiteSpace(newKey) || oldKey == newKey || table.Contains(newKey)) return 0;
            var entry = table.Find(oldKey);
            if (entry == null) return 0;

            Undo.RecordObject(table, "Rename localization key");
            entry.Key = newKey;
            EditorUtility.SetDirty(table);

            var updated = 0;
            foreach (var item in mod.Items.Where(i => i != null).Distinct())
            {
                var recorded = false;
                foreach (var module in item.AllModules)
                foreach (var field in ModuleSerializer.GetExportedFields(module.GetType()).Where(f => f.FieldType == typeof(LocalizationKey)))
                {
                    if (((LocalizationKey)field.GetValue(module)).Id != oldKey) continue;
                    if (!recorded)
                    {
                        Undo.RecordObject(item, "Rename localization key");
                        recorded = true;
                    }

                    field.SetValue(module, new LocalizationKey(newKey));
                    updated++;
                }

                if (recorded) EditorUtility.SetDirty(item);
            }

            foreach (var bp in mod.Blueprints.Where(b => b != null).Distinct())
            {
                if (bp.RecipeName.Id != oldKey && bp.RecipeDescription.Id != oldKey) continue;
                Undo.RecordObject(bp, "Rename localization key");
                if (bp.RecipeName.Id == oldKey) { bp.RecipeName = newKey; updated++; }
                if (bp.RecipeDescription.Id == oldKey) { bp.RecipeDescription = newKey; updated++; }
                EditorUtility.SetDirty(bp);
            }

            return updated;
        }
    }
}
