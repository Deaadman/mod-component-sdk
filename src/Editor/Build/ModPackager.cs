using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ModComponent.SDK.Editor.Build
{
    /// <summary>
    /// Zips a staged mod folder into a .modcomponent. Only paths the loader accepts are allowed, with '/'
    /// separators and lower-case names, in an order that puts buildinfo and bundles first: the loader stops
    /// reading an archive at its first bad entry.
    /// </summary>
    public static class ModPackager
    {
        public const string BuildInfoFile = "buildinfo.json";

        // Folder prefix → extensions allowed inside it. Order here is the order inside the archive.
        private static readonly (string folder, string[] extensions)[] Layout =
        {
            ("bundle/", new[] { ".bundle", ".unity3d", ".json" }),
            ("auto-mapped/", new[] { ".json" }),
            ("blueprints/", new[] { ".json" }),
            ("recipes/", new[] { ".json" }),
            ("localizations/", new[] { ".json" }),
            ("gear-spawns/", new[] { ".txt" }),
        };

        // Files the loader reads from anywhere.
        private static readonly string[] LooseExtensions = { ".bnk" };

        // Every extension the loader reads (ZipFileLoader.GetFileType). It skips any other, which is what makes
        // an integration's own root files, such as rarities.ir, safe.
        private static readonly string[] LoaderExtensions = { ".unity3d", ".bundle", ".json", ".txt", ".dll", ".bnk" };

        /// <summary>
        /// Whether integrations may write files with this extension at the archive root: a plain extension the
        /// loader skips. Anything it reads would be loaded, or stop the archive loading.
        /// </summary>
        public static bool IsAllowedRootExtension(string extension, out string reason)
        {
            reason = null;
            var ext = extension?.ToLowerInvariant() ?? string.Empty;
            if (ext.Length < 2 || ext[0] != '.' || !ext.Skip(1).All(char.IsLetterOrDigit))
                reason = $"\"{extension}\" isn't a file extension like \".ir\"";
            else if (LoaderExtensions.Contains(ext))
                reason = $"ModComponent loads {ext} files itself";
            return reason == null;
        }

        /// <summary>
        /// The archive path for a staged file, or null with a reason if the loader would reject it.
        /// <paramref name="rootExtensions"/> are extra extensions allowed at the root, declared by integrations.
        /// </summary>
        public static string ToEntryName(string relativePath, out string rejection, IEnumerable<string> rootExtensions = null)
        {
            rejection = null;
            var path = relativePath.Replace('\\', '/').TrimStart('/').ToLowerInvariant();
            var extension = Path.GetExtension(path);

            if (path == BuildInfoFile) return path;

            foreach (var (folder, extensions) in Layout)
            {
                if (!path.StartsWith(folder, StringComparison.Ordinal)) continue;
                if (extensions.Contains(extension)) return path;
                rejection = $"{relativePath}: {extension} files are not allowed in {folder}";
                return null;
            }

            if (LooseExtensions.Contains(extension)) return path;
            if (!path.Contains('/') && rootExtensions != null &&
                rootExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase))) return path;

            rejection = $"{relativePath}: ModComponent would reject this file";
            return null;
        }

        /// <summary>
        /// Writes the archive. Throws if any staged file would be rejected by the loader, or a root extension isn't
        /// one the loader skips.
        /// </summary>
        public static IReadOnlyList<string> Package(string stagingDirectory, string archivePath, IEnumerable<string> rootExtensions = null)
        {
            var allowedRoot = (rootExtensions ?? Enumerable.Empty<string>()).ToList();
            var badExtensions = allowedRoot
                .Select(e => IsAllowedRootExtension(e, out var reason) ? null : $"An integration declares the root extension {e}, but {reason}.")
                .Where(r => r != null)
                .ToList();
            if (badExtensions.Count > 0)
                throw new InvalidOperationException("Cannot package the mod:\n" + string.Join("\n", badExtensions));

            var files = Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories)
                .Select(f => (full: f, rel: Path.GetRelativePath(stagingDirectory, f)))
                .ToList();

            var entries = new List<(string full, string entry)>();
            var problems = new List<string>();
            foreach (var (full, rel) in files)
            {
                var entry = ToEntryName(rel, out var rejection, allowedRoot);
                if (entry == null) problems.Add(rejection);
                else entries.Add((full, entry));
            }

            if (problems.Count > 0)
                throw new InvalidOperationException("Cannot package the mod:\n" + string.Join("\n", problems));

            var duplicates = entries.GroupBy(e => e.entry).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
                throw new InvalidOperationException("Two files map to the same archive entry: " + string.Join(", ", duplicates));

            entries = entries.OrderBy(e => Rank(e.entry)).ThenBy(e => e.entry, StringComparer.Ordinal).ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivePath)) ?? ".");
            var temp = archivePath + ".tmp";
            if (File.Exists(temp)) File.Delete(temp);
            using (var stream = new FileStream(temp, FileMode.CreateNew))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (var (full, entry) in entries)
                {
                    var zipEntry = zip.CreateEntry(entry, CompressionLevel.Optimal);
                    using var source = File.OpenRead(full);
                    using var target = zipEntry.Open();
                    source.CopyTo(target);
                }
            }

            if (File.Exists(archivePath)) File.Delete(archivePath);
            File.Move(temp, archivePath);
            return entries.Select(e => e.entry).ToList();
        }

        private static int Rank(string entry)
        {
            if (entry == BuildInfoFile) return 0;
            for (var i = 0; i < Layout.Length; i++)
                if (entry.StartsWith(Layout[i].folder, StringComparison.Ordinal)) return i + 1;
            return Layout.Length + 1;
        }
    }
}
