#if UNITY_EDITOR
using ModComponent.Editor.API;
using ModComponent.SDK.Components;
using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEngine;
using UnityEditor;

namespace ModComponent.Editor.SDK
{
    [CustomEditor(typeof(ModDefinition))]
    internal class EditorModDefinition : EditorBase
    {
        SerializedProperty requiredMods, items, icons, modItemRarities;

        protected override Tab[] GetTabs() => new[] { Tab.Common };

        protected override void OnEnable()
        {
            base.OnEnable();
            requiredMods = serializedObject.FindProperty("RequiredMods");
            items = serializedObject.FindProperty("Items");
            icons = serializedObject.FindProperty("Icons");
            modItemRarities = serializedObject.FindProperty("modItemRarities");
        }

        void DrawCustomFields()
        {
            ModDefinition modDefinition = (ModDefinition)target;

            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawCustomHeading("General Properties");

            EditorGUI.BeginDisabledGroup(true);
            modDefinition.Name = EditorGUILayout.TextField(new GUIContent("Name", "ModComponent name."), modDefinition.Name);
            modDefinition.Author = EditorGUILayout.TextField(new GUIContent("Author", "Creator of this ModComponent."), modDefinition.Author);
            EditorGUI.EndDisabledGroup();

            modDefinition.Version = EditorGUILayout.TextField(new GUIContent("Version", "Version of this ModComponent."), modDefinition.Version);

            EditorGUILayout.Space(10);
            DrawCustomHeading("Required Dependencies (Optional)");
            EditorGUILayout.PropertyField(requiredMods, new GUIContent("Required Mods", "Required mods for functionality."), true);
            modDefinition.RequiresDLC = EditorGUILayout.Toggle(new GUIContent("Requires DLC", "TFTFT DLC requirement."), modDefinition.RequiresDLC);

            EditorGUILayout.Space(10);
            DrawCustomHeading("Content Properties");
            EditorGUILayout.PropertyField(items, new GUIContent("GEAR Prefabs", "GEAR_ Prefabs associated with this ModComponent."), true);
            EditorGUILayout.PropertyField(icons, new GUIContent("Inventory, Crafting & Paperdoll Textures", "Icons and textures."), true);

            EditorGUILayout.Space(10);
            DrawCustomHeading("Misc Properties");
            EditorGUILayout.PropertyField(modItemRarities, new GUIContent("Item Rarities", "This object allows your items to have assigned rarities."), false);
            
            GUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCustomFields();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif