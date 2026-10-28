using System.Linq;
using ModComponent.SDK.Editor.Migration;
using ModComponent.SDK.Editor.UI.Windows;
using ModComponent.SDK.Editor.Updates;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModComponent.SDK.Editor.UI
{
    /// <summary>The Mod Hub's banners: an available update, and 1.x mods waiting to be imported.</summary>
    [InitializeOnLoad]
    internal static class HubBanners
    {
        static HubBanners()
        {
            ModHubWindow.BuildingBanners += Add;
        }

        private static void Add(VisualElement container)
        {
            var update = new VisualElement();
            container.Add(update);
            void RefreshUpdate()
            {
                update.Clear();
                var available = UpdateService.Available;
                if (available == null || UpdateService.IsDismissed(available)) return;
                var canInstall = SdkInfo.Package?.source == PackageSource.Git;
                update.Add(Banner($"{SdkInfo.DisplayName} {available.Version} is available (you have {SdkInfo.Version}).",
                    ("Release notes", () => Application.OpenURL(available.Url)),
                    canInstall ? ("Update", () => UpdateService.Install(available)) : default,
                    ("✕", () => UpdateService.Dismiss(available))));
            }

            RefreshUpdate();
            UpdateService.Changed += RefreshUpdate;
            update.RegisterCallback<DetachFromPanelEvent>(_ => UpdateService.Changed -= RefreshUpdate);

            var waiting = V1Importer.FindDefinitions().Count(d => !V1Importer.IsImported(d));
            if (waiting > 0)
                container.Add(Banner($"Found {waiting} mod(s) made with SDK 1.x.", ("Import…", MigrationWindow.Open)));
        }

        private static VisualElement Banner(string text, params (string label, System.Action action)[] buttons)
        {
            var banner = new VisualElement();
            banner.AddToClassList("mcsdk-banner");
            var label = new Label(text);
            label.AddToClassList("mcsdk-banner__text");
            banner.Add(label);
            foreach (var (caption, action) in buttons.Where(b => b.label != null))
                banner.Add(new Button(action) { text = caption });
            return banner;
        }
    }
}
