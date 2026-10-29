using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

namespace ModComponent.SDK.Editor.Updates
{
    /// <summary>A semantic version, including pre-release tags such as 2.0.0-nightly.3.</summary>
    public readonly struct SemVersion : IComparable<SemVersion>
    {
        private static readonly Regex Pattern = new(@"^v?(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$");

        public SemVersion(int major, int minor, int patch, string prerelease = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease ?? string.Empty;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string Prerelease { get; }
        public bool IsPrerelease => Prerelease.Length > 0;

        public static bool TryParse(string text, out SemVersion version)
        {
            version = default;
            var m = Pattern.Match(text?.Trim() ?? string.Empty);
            if (!m.Success) return false;
            version = new SemVersion(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                m.Groups[4].Success ? m.Groups[4].Value : null);
            return true;
        }

        /// <summary>SemVer 2.0 precedence: a release outranks its pre-releases; numeric identifiers compare as numbers.</summary>
        public int CompareTo(SemVersion other)
        {
            var c = Major.CompareTo(other.Major);
            if (c == 0) c = Minor.CompareTo(other.Minor);
            if (c == 0) c = Patch.CompareTo(other.Patch);
            if (c != 0) return c;
            if (!IsPrerelease) return other.IsPrerelease ? 1 : 0;
            if (!other.IsPrerelease) return -1;

            var a = Prerelease.Split('.');
            var b = other.Prerelease.Split('.');
            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                var aNum = int.TryParse(a[i], out var an);
                var bNum = int.TryParse(b[i], out var bn);
                c = aNum && bNum ? an.CompareTo(bn) : aNum ? -1 : bNum ? 1 : string.CompareOrdinal(a[i], b[i]);
                if (c != 0) return c;
            }

            return a.Length.CompareTo(b.Length);
        }

        public override string ToString() => IsPrerelease ? $"{Major}.{Minor}.{Patch}-{Prerelease}" : $"{Major}.{Minor}.{Patch}";
    }

    /// <summary>A release on GitHub that is newer than the installed SDK.</summary>
    public sealed class AvailableUpdate
    {
        public string Tag;
        public SemVersion Version;
        public string Notes;
        public string Url;
    }

    /// <summary>
    /// Checks GitHub releases once per editor session and installs a chosen release by its tag. Never installs
    /// from a moving branch, and never shows a popup: the Mod Hub shows a dismissible banner instead.
    /// </summary>
    [InitializeOnLoad]
    public static class UpdateService
    {
        private const string ReleasesApi = "https://api.github.com/repos/Deaadman/mod-component-sdk/releases";
        private const string CheckedKey = "ModComponentSDK.UpdateChecked";
        private const string DismissedKey = "ModComponentSDK.DismissedUpdate";
        public const string CheckOnStartupKey = "ModComponentSDK.CheckForUpdates";
        public const string IncludePrereleasesKey = "ModComponentSDK.IncludePrereleases";

        public static AvailableUpdate Available { get; private set; }
        public static event Action Changed;

        static UpdateService()
        {
            if (Application.isBatchMode || SessionState.GetBool(CheckedKey, false)) return;
            if (!EditorPrefs.GetBool(CheckOnStartupKey, true)) return;
            SessionState.SetBool(CheckedKey, true);
            EditorApplication.delayCall += Check;
        }

        /// <summary>Whether pre-releases (nightlies) count. Defaults to on when the installed version is itself a nightly.</summary>
        public static bool IncludePrereleases
        {
            get => EditorPrefs.GetBool(IncludePrereleasesKey, CurrentVersion.IsPrerelease);
            set => EditorPrefs.SetBool(IncludePrereleasesKey, value);
        }

        public static SemVersion CurrentVersion => SemVersion.TryParse(SdkInfo.Version, out var v) ? v : default;

        // By version, not tag: the nightly's tag stays "nightly" from one build to the next.
        public static bool IsDismissed(AvailableUpdate update) => update != null && EditorPrefs.GetString(DismissedKey, string.Empty) == update.Version.ToString();

        public static void Dismiss(AvailableUpdate update)
        {
            EditorPrefs.SetString(DismissedKey, update?.Version.ToString() ?? string.Empty);
            Changed?.Invoke();
        }

        public static void Check()
        {
            var request = UnityWebRequest.Get(ReleasesApi);
            request.SetRequestHeader("User-Agent", "ModComponent-SDK");
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SendWebRequest().completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success) return;
                    Available = PickUpdate(request.downloadHandler.text, CurrentVersion, IncludePrereleases);
                    Changed?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{SdkInfo.DisplayName}: couldn't read the release list ({e.Message}).");
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        /// <summary>
        /// The newest release above <paramref name="current"/>, from GitHub's releases JSON. Drafts are ignored. A release's
        /// version is its tag (1.0.5, v1.0.5), or its title for the "nightly" release, titled e.g. 2.0.0-nightly.20261029.42.
        /// </summary>
        public static AvailableUpdate PickUpdate(string releasesJson, SemVersion current, bool includePrereleases)
        {
            var candidates = new List<AvailableUpdate>();
            foreach (var release in JArray.Parse(releasesJson).OfType<JObject>())
            {
                if (release.Value<bool?>("draft") == true) continue;
                var tag = release.Value<string>("tag_name");
                if (!SemVersion.TryParse(tag, out var version) && !SemVersion.TryParse(release.Value<string>("name"), out version)) continue;
                if (version.IsPrerelease && !includePrereleases) continue;
                if (version.CompareTo(current) <= 0) continue;
                candidates.Add(new AvailableUpdate
                {
                    Tag = tag, Version = version, Notes = release.Value<string>("body") ?? string.Empty,
                    Url = release.Value<string>("html_url")
                });
            }

            return candidates.OrderByDescending(c => c.Version).FirstOrDefault();
        }

        /// <summary>
        /// Installs the release's tag through Package Manager, e.g. …mod-component-sdk.git?path=src#2.0.1, along with
        /// every add-on installed from this repository, so they stay on the same release.
        /// </summary>
        public static void Install(AvailableUpdate update)
        {
            var installed = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Select(p => p.packageId);
            var request = Client.AddAndRemove(InstallUrls(update.Tag, installed).ToArray());
            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= poll;
                if (request.Status == StatusCode.Success)
                    Debug.Log($"{SdkInfo.DisplayName} updated to {update.Version}.");
                else
                    Debug.LogError($"{SdkInfo.DisplayName} update to {update.Version} failed: {request.Error?.message}");
            };
            EditorApplication.update += poll;
        }

        /// <summary>
        /// Git URLs to install for <paramref name="tag"/>: the SDK, then each add-on from this repository found in
        /// <paramref name="installedPackageIds"/> (name@url), such as …mod-component-sdk.git?path=addons/item-rarities.
        /// </summary>
        public static IEnumerable<string> InstallUrls(string tag, IEnumerable<string> installedPackageIds)
        {
            var repository = SdkInfo.RepositoryUrl + ".git?path=";
            yield return $"{repository}src#{tag}";

            var addOns = installedPackageIds
                .Select(id =>
                {
                    var start = id.IndexOf(repository, StringComparison.OrdinalIgnoreCase);
                    return start < 0 ? null : id.Substring(start + repository.Length).Split('#')[0].Trim('/');
                })
                .Where(path => path != null && path.StartsWith("addons/", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal);
            foreach (var path in addOns) yield return $"{repository}{path}#{tag}";
        }
    }
}
