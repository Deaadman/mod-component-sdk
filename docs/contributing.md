# Contributing

## Setting up

You need Unity **6000.0.60f1** exactly, the version The Long Dark is built with. In any other version the SDK won't build mods.

1. Fork the repository and clone your fork.
2. Create an empty Unity project to work in.
3. In **Window › Package Manager**, choose **+ › Install package from disk…** and pick `src/package.json` from your clone. Changes you make in the clone show up in the project straight away. Install the add-ons in `addons/` the same way if you're working on them.
4. To run the tests, add the packages to `testables` in the project's `Packages/manifest.json`:

   ```json
   "testables": ["com.deadman.modcomponent.sdk", "com.deadman.modcomponent.sdk.item-rarities"]
   ```

   Or open `test-project` in Unity Hub instead: it installs the SDK and every add-on from your clone, with their tests on.

## Making a change

1. Pick an [issue](https://github.com/Deaadman/mod-component-sdk/issues), or open a [bug report](https://github.com/Deaadman/mod-component-sdk/issues/new?template=bug_report.yml) or [feature request](https://github.com/Deaadman/mod-component-sdk/issues/new?template=feature_request.yml) for it first.
2. Make a branch from `develop` for that one change.
3. Add or update [tests](#testing), and add a line to [`CHANGELOG.md`](../CHANGELOG.md) under `## [Unreleased]` if modders will notice the change (see [Writing the changelog](#writing-the-changelog)).
4. Open a pull request against `develop`, not `main`, and link the issue.

To suggest a change to the [wiki](https://github.com/Deaadman/mod-component-sdk/wiki), which only maintainers can edit, use the [wiki change form](https://github.com/Deaadman/mod-component-sdk/issues/new?template=wiki.yml).

## How it's laid out

- `src`: the Unity package. Package Manager installs this folder, so everything the SDK ships lives in it.
- `src/Runtime`: the assets and data model: `Mod`, `GearItem`, `Blueprint`, the tables, the item modules and the references the pickers fill in.
- `src/Editor/Build`: turns a mod into a `.modcomponent` file: the exporter, the workarounds for the loader's bugs, the Addressables build and the packager.
- `src/Editor/Validation`: the rule engine and its rules. A new rule is picked up automatically.
- `src/Editor/UI`: the Mod Hub, inspectors, pickers and windows, in UI Toolkit, styled in `Styles.uss`.
- `src/Editor/Authoring`: the wizards, item templates, example mod, and localization CSV and renames.
- `src/Editor/Migration`: the 1.x importer.
- `src/Editor/Updates`: the update banner, which reads the repository's releases and installs one by its tag.
- `src/Legacy`: the 1.x types, kept so old projects still load. Don't add to them; they go in 3.0.
- `src/Data/catalog.json`: the game's names the pickers offer.
- `src/Samples~/VanillaIcons`: the vanilla item icons, an optional sample.
- `src/Documentation~/index.md`: the guide linked from the README.
- `src/Tests/Editor`: the EditMode tests.
- `addons`: add-on packages, one folder each, such as `addons/item-rarities`. See [Add-ons](#add-ons).
- `test-project`: the Unity project CI runs the tests in. Only its `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt` are committed.
- `docs`: this page.
- `assets`: the banner. Edit `banner.svg` (its layers open in Inkscape), then export it as `assets/images/banner.webp` at its own size, 2400×480.
- `scripts/release-notes.ps1`: reads `CHANGELOG.md` for CI and the release workflows.

The JSON keys the exporter writes are ModComponent's own names, so don't rename them. `LoaderContractTests` in `ExportTests.cs` pins every key, so a new or renamed module field has to be added there too.

## Add-ons

An add-on is a Unity package in `addons/` that extends the SDK through its own assemblies, which reference the SDK's. The SDK finds what it defines by type, so installing the package is all a modder does. It can add:

- an integration: a `ModIntegration` subclass, shown as a toggle in **New Mod** and the Mod inspector, with its fields drawn under it
- data on items: an `ItemIntegrationData` subclass marked `[ItemIntegrationData(typeof(TheIntegration), Label = "…")]`, shown as a section on items of mods that turn the integration on
- files in the build: an `IntegrationExporter<TheIntegration>`. Files at the archive root need their extension in `RootExtensions`, and the packager refuses any extension ModComponent reads itself
- validation rules: `ValidationRule` subclasses, as in the SDK
- 1.x import steps: `V1ImportStep` subclasses, run by **Import 1.x Mods…**

[`addons/item-rarities`](../addons/item-rarities) does all of these. To make another, copy its layout: a `package.json` named `com.deadman.modcomponent.sdk.<name>` that depends on the SDK, `Runtime`, `Editor` and `Tests/Editor` assemblies, a `.meta` file for every file and folder (Package Manager ignores files without one when installing from git), and a guide in `Documentation~/index.md`. Add it to `test-project/Packages/manifest.json` and to the README's add-on table.

A type that moves from the SDK into an add-on keeps saved data loading with `[MovedFrom]`, naming its old namespace and assembly.

## Versions

The version is `version` in [`src/package.json`](../src/package.json). It's the next release's version, so a release changes nothing but the changelog. Every add-on has the same version, and depends on that version of the SDK, so one tag releases them all.

Nightly builds add a suffix with the date and the run, like `2.0.0-nightly.20261029.42`. Package Manager and the update banner show it, so a bug report says exactly which build it came from.

## Testing

Open **Window › General › Test Runner** and run the EditMode tests. CI runs them in `test-project`, with the SDK and every add-on. The tests that build a real mod need JSON catalogs turned on once, in **Preferences › ModComponent SDK**, and skip themselves otherwise.

Before a release, also build the example mod from **ModComponent SDK › Create Example Mod**, copy it into the game's `Mods` folder, and check its items load in the game.

## Branches

- `develop` is where work happens. Every push to it publishes a [nightly build](#nightly-builds).
- `main` has the newest release. Merge `develop` into it to release.

## CI

[CI](../.github/workflows/ci.yml) runs for every push to `main` and every pull request. It checks:

- that `CHANGELOG.md` follows the format below
- that the EditMode tests pass, run by [game.ci](https://game.ci) in a fresh project. It needs the repository secrets `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD`.

Once a week, [Dependabot](../.github/dependabot.yml) opens a pull request against `develop` for actions with a newer version, with the minor and patch updates together. Merge it when CI passes.

## Nightly builds

The [nightly workflow](../.github/workflows/nightly.yml) runs on every push to `develop`. It runs CI, then replaces the [`nightly`](https://github.com/Deaadman/mod-component-sdk/releases/tag/nightly) pre-release and its tag. The tag is one commit on top of `develop` that only sets the nightly version in `src/package.json` and the add-ons'. Its notes are the `## [Unreleased]` section of `CHANGELOG.md`, then a link to every commit since the latest release. To preview them, run `.\scripts\release-notes.ps1 -Version Unreleased -OutDir notes`.

## Releasing

The [release workflow](../.github/workflows/release.yml) runs when a version tag is pushed:

1. On `develop`, in [`CHANGELOG.md`](../CHANGELOG.md), rename `## [Unreleased]` to the new version and date, for example `## [2.0.0] - 2026-11-02`. Add a new empty `## [Unreleased]` above it, and update the links at the bottom.
2. Check `version` in `src/package.json` and in each `addons/*/package.json`, and each add-on's SDK dependency, are the same version. Commit, and merge `develop` into `main`.
3. Tag the commit on `main` and push the tag: `git tag 2.0.0`, then `git push origin 2.0.0`. Tags don't start with `v`.
4. Set `version` in `src/package.json` and the add-ons on `develop` to the next version, so nightlies after the release are numbered after it.

The workflow then runs CI, checks the tag matches `src/package.json` and every add-on, and publishes the release. It stops if `CHANGELOG.md` has no section for that version.

### Writing the changelog

`CHANGELOG.md` follows [Keep a Changelog 2.0.0](https://keepachangelog.com/en/2.0.0/), but without a `v` in front of versions. CI checks the format and says what to fix; run `.\scripts\release-notes.ps1 -Check` to check it yourself.

Each version's section is the release's description on GitHub, and the update banner links to it, so write for modders:

- Add each change under `## [Unreleased]` as you make it.
- One plain sentence per change, about what's different for them. "Gear spawn coordinates are written with `.` whatever the PC's region", not "Use InvariantCulture in SpawnWriter".
- Leave out changes modders can't see, like refactors and CI.
- Use full `https://` links. Links like `docs/contributing.md` break on the release page.

The format rules:

- Use only these headings, in this order, and skip empty ones: `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed`, `### Security`.
- Every line under a heading starts with `- `. A long entry can continue on the next line if it's indented.
- A version can start with a short summary before its first heading.
- Start a breaking change with `**Breaking:**`.
- Add ` [YANKED]` after the date of a pulled release. The workflow won't publish it.

The links at the bottom make each heading clickable. `[Unreleased]` compares the newest release with `HEAD`, each version compares with the one before, and the first links to its tag.
