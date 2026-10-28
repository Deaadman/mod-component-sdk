using System;
using System.Collections.Generic;
using System.Linq;
using ModComponent.SDK.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI
{
    /// <summary>Small shared building blocks for the SDK's UI Toolkit views.</summary>
    public static class SdkUI
    {
        private const string StylePath = "Packages/" + SdkInfo.PackageName + "/Editor/UI/Styles.uss";
        private static StyleSheet _styles;

        public static StyleSheet Styles => _styles != null ? _styles : _styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);

        public static T WithStyles<T>(this T element) where T : VisualElement
        {
            if (Styles != null && !element.styleSheets.Contains(Styles)) element.styleSheets.Add(Styles);
            return element;
        }

        public static VisualElement Section(string title, out VisualElement content)
        {
            var section = new VisualElement();
            section.AddToClassList("mcsdk-section");
            if (!string.IsNullOrEmpty(title))
            {
                var label = new Label(title);
                label.AddToClassList("mcsdk-section__title");
                section.Add(label);
            }

            content = section;
            return section;
        }

        public static VisualElement Row(params VisualElement[] children)
        {
            var row = new VisualElement();
            row.AddToClassList("mcsdk-buttons");
            foreach (var child in children) row.Add(child);
            return row;
        }

        public static Texture SeverityIcon(Severity severity) => EditorGUIUtility.IconContent(severity switch
        {
            Severity.Error => "console.erroricon.sml",
            Severity.Warning => "console.warnicon.sml",
            _ => "console.infoicon.sml"
        }).image;

        /// <summary>
        /// A list of validation issues with Select and Fix buttons. <paramref name="onFixed"/> runs after a fix,
        /// typically to revalidate.
        /// </summary>
        public static VisualElement IssueList(IEnumerable<ValidationIssue> issues, Action onFixed, bool showTarget = true)
        {
            var list = new VisualElement();
            var any = false;
            foreach (var issue in issues)
            {
                any = true;
                var row = new VisualElement();
                row.AddToClassList("mcsdk-issue");

                var icon = new Image { image = SeverityIcon(issue.Severity) };
                icon.AddToClassList("mcsdk-issue__icon");
                row.Add(icon);

                var text = new VisualElement();
                text.AddToClassList("mcsdk-issue__text");
                var message = new Label(issue.Message) { tooltip = issue.RuleId };
                message.style.whiteSpace = WhiteSpace.Normal;
                text.Add(message);
                var where = string.Join(" · ", new[] { showTarget && issue.Target != null ? issue.Target.name : null, issue.Field }
                    .Where(s => !string.IsNullOrEmpty(s)));
                if (where.Length > 0)
                {
                    var whereLabel = new Label(where);
                    whereLabel.AddToClassList("mcsdk-issue__where");
                    text.Add(whereLabel);
                }

                row.Add(text);

                if (showTarget && issue.Target != null)
                {
                    var select = new Button(() =>
                    {
                        Selection.activeObject = issue.Target;
                        EditorGUIUtility.PingObject(issue.Target);
                    }) { text = "Select" };
                    select.AddToClassList("mcsdk-issue__button");
                    row.Add(select);
                }

                if (issue.HasFix)
                {
                    var fix = new Button(() =>
                    {
                        issue.Fix();
                        AssetDatabase.SaveAssets();
                        onFixed?.Invoke();
                    }) { text = issue.FixLabel ?? "Fix" };
                    fix.AddToClassList("mcsdk-issue__button");
                    row.Add(fix);
                }

                list.Add(row);
            }

            if (!any)
            {
                var empty = new Label("No problems found.");
                empty.AddToClassList("mcsdk-empty");
                list.Add(empty);
            }

            return list;
        }

        /// <summary>Mods whose item list contains the item. Usually one.</summary>
        public static IEnumerable<Mod> OwningMods(GearItem item) =>
            ModValidator.FindAllMods().Where(m => m.Items.Contains(item));

        public static IEnumerable<Mod> OwningMods(Blueprint blueprint) =>
            ModValidator.FindAllMods().Where(m => m.Blueprints.Contains(blueprint));
    }
}
