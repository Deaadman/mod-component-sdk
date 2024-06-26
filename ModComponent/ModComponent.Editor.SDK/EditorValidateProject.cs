#if UNITY_EDITOR
using ModComponent.SDK;
using UnityEditor;
using UnityEngine;

namespace ModComponent.Editor.SDK
{
    internal class EditorValidateProject : EditorValidateBase
    {
        private CheckStatus assetsGeneratedStatus = CheckStatus.Pending;

        [MenuItem("ModComponent SDK/Validate Project", false, 40)]
        internal static void ShowWindow()
        {
            var window = GetWindow<EditorValidateProject>("Project Validator");
            window.minSize = new Vector2(300, 300);
            window.StartProjectValidation();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            DrawStatus("Data Assets", ref assetsGeneratedStatus);

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private async void StartProjectValidation()
        {
            assetsGeneratedStatus = CheckStatus.Checking;
            bool assetsGenerationResult = DataGenerator.CheckAndPromptForAssetGeneration();
            assetsGeneratedStatus = assetsGenerationResult ? CheckStatus.Success : CheckStatus.Failed;
        }

        protected override string GetStatusMessage(string baseLabel, CheckStatus status)
        {
            return status switch
            {
                CheckStatus.Checking => $"Checking {baseLabel}...",
                CheckStatus.Success => $"{baseLabel} is installed.",
                CheckStatus.Failed => $"Installation for {baseLabel} failed.",
                CheckStatus.Pending => $"{baseLabel} status not checked.",
                CheckStatus.Waiting => $"Installation available for {baseLabel}.",
                _ => baseLabel,
            };
        }
    }
}
#endif