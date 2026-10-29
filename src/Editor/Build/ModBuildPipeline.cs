using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ModComponent.SDK.Editor.Build.Export;
using ModComponent.SDK.Editor.Integrations;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ModComponent.SDK.Editor.Build
{
    public enum BuildMessageSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct BuildMessage
    {
        public BuildMessage(BuildMessageSeverity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public BuildMessageSeverity Severity { get; }
        public string Text { get; }
        public override string ToString() => $"{Severity}: {Text}";
    }

    public sealed class ModBuildOptions
    {
        /// <summary>Folder the .modcomponent is written to.</summary>
        public string OutputDirectory;

        /// <summary>Optional folder to copy the finished file into, such as the game's Mods folder.</summary>
        public string CopyToDirectory;

        /// <summary>Skip the Addressables step. Used by tests that only check the data files.</summary>
        public bool SkipBundles;
    }

    /// <summary>The pipeline's steps, in order.</summary>
    public enum BuildStep
    {
        Check,
        Stage,
        Write,
        Bundle,
        Package,
        Copy
    }

    /// <summary>What a build did, for the build window and logs.</summary>
    public sealed class BuildReport
    {
        public Mod Mod;
        public bool Succeeded;
        public string ArchivePath;
        public string StagingDirectory;
        public readonly List<string> Entries = new();
        public readonly List<BuildMessage> Messages = new();
        public TimeSpan Duration;

        /// <summary>The step the build was on when it finished or failed.</summary>
        public BuildStep Step;

        public IEnumerable<BuildMessage> Errors => Messages.Where(m => m.Severity == BuildMessageSeverity.Error);
        public IEnumerable<BuildMessage> Warnings => Messages.Where(m => m.Severity == BuildMessageSeverity.Warning);

        internal void Info(string text) => Messages.Add(new BuildMessage(BuildMessageSeverity.Info, text));
        internal void Warn(string text) => Messages.Add(new BuildMessage(BuildMessageSeverity.Warning, text));
        internal void Fail(string text) => Messages.Add(new BuildMessage(BuildMessageSeverity.Error, text));
    }

    /// <summary>
    /// Builds a .modcomponent: check → stage → write → bundle → package → copy. Staging happens under
    /// Library/, never inside Assets/, and nothing in the project is left changed.
    /// </summary>
    public static class ModBuildPipeline
    {
        /// <summary>
        /// Checks run before anything is built (by default every validation rule). Errors stop the build.
        /// </summary>
        public static Func<Mod, IEnumerable<BuildMessage>> Validator =
            mod => Validation.ModValidator.Validate(mod, forBuild: true).Issues.Select(Validation.ModValidator.ToBuildMessage);

        public static string StagingRoot => Path.GetFullPath(Path.Combine("Library", "ModComponentSDK"));

        public static string ArchiveFileName(Mod mod) => mod.Id + ".modcomponent";

        public static BuildReport Build(Mod mod, ModBuildOptions options)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            options ??= new ModBuildOptions();

            var report = new BuildReport { Mod = mod };
            var clock = Stopwatch.StartNew();
            try
            {
                Run(mod, options, report);
            }
            catch (Exception e)
            {
                report.Fail(e.Message);
                Debug.LogException(e);
            }
            finally
            {
                report.Duration = clock.Elapsed;
                report.Succeeded = !report.Errors.Any();
                EditorUtility.ClearProgressBar();
            }

            return report;
        }

        private static void Run(Mod mod, ModBuildOptions options, BuildReport report)
        {
            // 1. Check
            report.Step = BuildStep.Check;
            Progress("Checking", 0.05f);
            foreach (var message in Validator(mod) ?? Enumerable.Empty<BuildMessage>())
                report.Messages.Add(message);
            if (report.Errors.Any()) return;

            report.Step = BuildStep.Stage;
            // 2. Stage. The id names folders that are deleted recursively, so it is checked here as well as in validation.
            if (!Mod.IsValidId(mod.Id))
            {
                report.Fail($"Mod id \"{mod.Id}\" may only use letters, digits, '-' and '_'.");
                return;
            }

            var id = mod.Id;
            var archiveName = ArchiveFileName(mod);
            var staging = Path.Combine(StagingRoot, id);
            report.StagingDirectory = staging;
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            report.Info($"Staging in {staging}");

            if (!options.SkipBundles && !ProjectSetup.IsJsonCatalogEnabled)
            {
                report.Fail(ProjectSetup.JsonCatalogMessage);
                return;
            }

            // 3. Write. Runs before the bundle step: an Addressables build can unload the mod's assets,
            // after which item and table references read as null.
            report.Step = BuildStep.Write;
            Progress("Writing data files", 0.15f);
            WriteDataFiles(mod, staging, report);
            if (report.Errors.Any()) return;

            // 4. Bundle
            if (!options.SkipBundles)
            {
                report.Step = BuildStep.Bundle;
                Progress("Building bundles", 0.3f);
                var bundles = AddressablesModBuilder.Build(mod, Path.Combine(StagingRoot, id + "-addressables"));
                if (!bundles.Succeeded)
                {
                    report.Fail("Bundle build failed: " + bundles.Error);
                    return;
                }

                var bundleDir = Path.Combine(staging, "bundle");
                Directory.CreateDirectory(bundleDir);
                foreach (var path in bundles.BundlePaths.Append(bundles.CatalogPath))
                {
                    var name = Path.GetFileName(path).ToLowerInvariant();
                    File.Copy(path, Path.Combine(bundleDir, name), true);
                    report.Info($"bundle/{name}  {FormatSize(new FileInfo(path).Length)}");
                }
            }

            // 5. Package
            report.Step = BuildStep.Package;
            Progress("Packaging", 0.85f);
            var outputDirectory = string.IsNullOrEmpty(options.OutputDirectory)
                ? Path.Combine(StagingRoot, "Output")
                : options.OutputDirectory;
            var archive = Path.Combine(outputDirectory, archiveName);
            report.Entries.AddRange(ModPackager.Package(staging, archive, IntegrationRegistry.RootExtensions(mod)));
            report.ArchivePath = archive;
            report.Info($"Wrote {archive} ({FormatSize(new FileInfo(archive).Length)}, {report.Entries.Count} files)");

            // 6. Copy
            if (!string.IsNullOrEmpty(options.CopyToDirectory))
            {
                report.Step = BuildStep.Copy;
                Directory.CreateDirectory(options.CopyToDirectory);
                var copy = Path.Combine(options.CopyToDirectory, archiveName);
                File.Copy(archive, copy, true);
                report.Info($"Copied to {copy}");
            }
        }

        /// <summary>Writes buildinfo, item JSON, blueprints, recipes, localization, spawns, integrations and extras.</summary>
        public static void WriteDataFiles(Mod mod, string staging, BuildReport report)
        {
            Write(staging, ModPackager.BuildInfoFile, ModWriters.ToJson(ModWriters.BuildInfo(mod)));

            var notes = new List<string>();
            foreach (var item in mod.Items.Where(i => i != null))
            {
                var file = "auto-mapped/" + ModWriters.ItemFileName(item);
                Write(staging, file, ModWriters.ToJson(ModWriters.Item(item, notes)));
                report.Info(file);
            }

            foreach (var note in notes) report.Info("Loader fix: " + note);

            foreach (var bp in mod.Blueprints.Where(b => b != null))
            {
                var folder = bp.IsCookingRecipe ? "recipes/" : "blueprints/";
                var json = bp.IsCookingRecipe ? ModWriters.Recipe(bp) : ModWriters.Blueprint(bp);
                Write(staging, folder + ModWriters.BlueprintFileName(bp), ModWriters.ToJson(json));
                report.Info(folder + ModWriters.BlueprintFileName(bp));
            }

            var localization = ModWriters.Localization(mod.Localization);
            if (localization.Count > 0)
            {
                var file = "localizations/" + mod.Id.ToLowerInvariant() + ".json";
                Write(staging, file, ModWriters.ToJson(localization));
                report.Info($"{file} ({localization.Count} keys)");
            }

            var spawns = ModWriters.GearSpawns(mod.Spawns);
            if (!string.IsNullOrWhiteSpace(spawns))
            {
                var file = "gear-spawns/" + mod.Id.ToLowerInvariant() + ".txt";
                Write(staging, file, spawns);
                report.Info(file);
            }

            foreach (var integration in mod.Integrations.Where(i => i != null))
            {
                var exporter = IntegrationRegistry.Get(integration)?.Exporter;
                if (exporter == null)
                {
                    report.Warn($"No exporter for the {integration.DisplayName} integration; its data was not written.");
                    continue;
                }

                foreach (var (path, contents) in exporter.Export(mod, integration))
                {
                    Write(staging, path, contents);
                    report.Info(path);
                }
            }

            var extraNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var extra in mod.ExtraFiles.Where(f => f != null))
            {
                var source = Path.GetFullPath(AssetDatabase.GetAssetPath(extra));
                var name = Path.GetFileName(source);
                if (!extraNames.Add(name))
                {
                    report.Fail($"Two extra files are named {name}; rename one so both reach the archive.");
                    continue;
                }

                var target = Path.Combine(staging, "extras", name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, false);
                report.Info("extras/" + name);
            }
        }

        private static void Write(string staging, string relativePath, string contents)
        {
            var path = Path.Combine(staging, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static void Progress(string step, float value)
        {
            if (!UnityEngine.Application.isBatchMode)
                EditorUtility.DisplayProgressBar("Building mod", step, value);
        }

        private static string FormatSize(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024f / 1024f:0.0} MB" : $"{bytes / 1024f:0.#} KB";
    }
}
