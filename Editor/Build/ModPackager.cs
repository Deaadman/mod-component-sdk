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

        // Files the loader reads from anywhere, or skips harmlessly (integration data such as rarities.ir).
        private static readonly string[] LooseExtensions = { ".bnk", ".ir" };

        /// <summary>The archive path for a staged file, or null with a reason if the loader would reject it.</summary>
        public static string ToEntryName(string relativePath, out string rejection)
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

            rejection = $"{relativePath}: ModComponent would reject this file";
            return null;
        }

        /// <summary>Writes the archive. Throws if any staged file would be rejected by the loader.</summary>
        public static IReadOnlyList<string> Package(string stagingDirectory, string archivePath)
        {
            var files = Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories)
                .Select(f => (full: f, rel: Path.GetRelativePath(stagingDirectory, f)))
                .ToList();

            var entries = new List<(string full, string entry)>();
            var problems = new List<string>();
            foreach (var (full, rel) in files)
            {
                var entry = ToEntryName(rel, out var rejection);
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
