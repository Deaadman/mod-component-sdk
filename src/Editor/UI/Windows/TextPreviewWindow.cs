using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI.Windows
{
    /// <summary>A read-only text popup.</summary>
    public sealed class TextPreviewWindow : EditorWindow
    {
        public void ShowText(string text)
        {
            rootVisualElement.Clear();
            var field = new TextField { multiline = true, isReadOnly = true, value = text, style = { flexGrow = 1 } };
            var scroll = new ScrollView { style = { flexGrow = 1 } };
            scroll.Add(field);
            rootVisualElement.Add(scroll);
            minSize = new Vector2(420, 300);
            Show();
        }
    }
}
