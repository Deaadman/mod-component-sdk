#if UNITY_EDITOR
using ModComponent.Editor.API;
using ModComponent.SDK.Components;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor.SDK
{
    [CustomEditor(typeof(ModItemRarities))]
    internal class EditorModItemRarities : EditorBase
    {
        private SerializedProperty commonGameObjects;
        private SerializedProperty uncommonGameObjects;
        private SerializedProperty rareGameObjects;
        private SerializedProperty epicGameObjects;
        private SerializedProperty legendaryGameObjects;
        private SerializedProperty mythicGameObjects;

        protected override Tab[] GetTabs() => new[] { Tab.Common };

        protected override void OnEnable()
        {
            commonGameObjects = serializedObject.FindProperty("Common");
            uncommonGameObjects = serializedObject.FindProperty("Uncommon");
            rareGameObjects = serializedObject.FindProperty("Rare");
            epicGameObjects = serializedObject.FindProperty("Epic");
            legendaryGameObjects = serializedObject.FindProperty("Legendary");
            mythicGameObjects = serializedObject.FindProperty("Mythic");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginVertical(ModComponentEditorStyles.BackgroundBox);
            DrawCustomHeading("Item Rarities");

            EditorGUILayout.PropertyField(commonGameObjects, new GUIContent("Common GearItem Prefabs", "All prefabs under here will be classified with a 'common' rarity."), true);
            EditorGUILayout.PropertyField(uncommonGameObjects, new GUIContent("Uncommon GearItem Prefabs", "All prefabs under here will be classified with a 'uncommon' rarity."), true);
            EditorGUILayout.PropertyField(rareGameObjects, new GUIContent("Rare GearItem Prefabs", "All prefabs under here will be classified with a 'rare' rarity."), true);
            EditorGUILayout.PropertyField(epicGameObjects, new GUIContent("Epic GearItem Prefabs", "All prefabs under here will be classified with a 'epic' rarity."), true);
            EditorGUILayout.PropertyField(legendaryGameObjects, new GUIContent("Legendary GearItem Prefabs", "All prefabs under here will be classified with a 'legendary' rarity."), true);
            EditorGUILayout.PropertyField(mythicGameObjects, new GUIContent("Mythic GearItem Prefabs", "All prefabs under here will be classified with a 'mythic' rarity."), true);

            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif