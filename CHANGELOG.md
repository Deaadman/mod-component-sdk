# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
A version's section is its GitHub release description, and the Mod Hub's
update banner links to it, so write it for modders: one plain sentence
each, about what changed for them.

- Add entries under "## [Unreleased]" as you go, grouped under Added,
  Changed, Deprecated, Removed, Fixed or Security.
- To release, rename it to "## [2.0.0] - YYYY-MM-DD" (no v), add a fresh
  "## [Unreleased]" above it, and update the links at the bottom.

See docs/contributing.md, "Releasing". CI checks this file's format.
-->

## [Unreleased]

A rewrite of the SDK. To bring a mod made with 1.x across, install 2.0 over 1.x, open **ModComponent SDK › Import 1.x Mods…**, run a **Dry run**, check its notes, then **Import**. Review the new mod in the Mod Hub, then **Remove 1.x scripts** from its prefabs.

### Added

- The **Mod Hub** lists every mod in the project with its items, blueprints and tables, their issue counts, search, and **New**, **Validate** and **Build**.
- Each item is a `GearItem` asset holding its component, behaviours and modifications as cards, so prefabs stay pure visuals.
- Searchable pickers for gear, sounds, scenes, loot tables, liquids, powders, localization keys and prefab children, filled from a bundled list of the game's names.
- Validation rules, most with a one-click fix, run before every build.
- A **Localization** window with a spreadsheet view, CSV import and export, and key renames that update every use.
- A **Spawns** window with a coordinate paste box and a preview of the file the build writes.
- A **Build** window with each step, its log, and copying the result to the game's `Mods` folder.
- An inspect preview and an inventory icon renderer.
- **New Mod** and **New Item** wizards with 14 item templates, and **Create Example Mod**.
- An **Import 1.x Mods** window with a dry run.
- An update banner in the Mod Hub that installs a release by its tag, along with any add-ons, and **Preferences › ModComponent SDK**.
- Item Rarities support, in the optional Item Rarities add-on. Install it with `https://github.com/Deaadman/mod-component-sdk.git?path=addons/item-rarities#2.0.0`, the same way as the SDK.
- Add-on packages can extend the SDK: a setting on each mod, data on each item, files in the build, validation rules and steps for **Import 1.x Mods…**.
- A warning on mods and items that use data from an add-on that isn't installed, with a button to remove it.

### Changed

- **Breaking:** The SDK needs Unity **6000.0.60f1** exactly, and makes mods for ModComponent 7.0.0 and The Long Dark 2.58.
- **Breaking:** Install the SDK with `https://github.com/Deaadman/mod-component-sdk.git?path=src#2.0.0`: the package now lives in the repository's `src` folder.
- Builds are staged in `Library/`, never change prefabs, use a temporary Addressables group, and put the project's Addressables settings back afterwards.
- Item files have every field the loader reads, under its exact names, and work around the loader's known bugs with clothing's first-person prefabs and empty cooking results.
- The vanilla item icons are in the optional **Vanilla Icons** sample.
- The SDK reads and writes JSON with `com.unity.nuget.newtonsoft-json`.

### Removed

- The 1.x editors, Mod Manager, validators and data asset generator. The 1.x types stay so old projects still load, until 3.0.

### Fixed

- Mods with spaces in their names are included in their own Addressables build.
- Gear spawn coordinates are written with `.` whatever the PC's region.
- The Bed component exports its packed and usable meshes, instead of the normal model for both.
- The prefab validator reports problems, instead of always passing.
- An interrupted build no longer leaves prefabs without their components.
- Deleting a mod definition deletes only that asset, not its whole folder.
- The update checker installs the tag you chose, and no longer offers older versions as updates.

## [1.0.5] - 2024-08-23

### Changed

- `QuantityInKilograms` and `VolumeInLitres` take decimals.

### Fixed

- Exporting one of several mods in a project no longer builds every mod's Addressables groups into it.

## [1.0.4] - 2024-08-05

### Added

- `GutLabelOverride`, `HideLabelOverride` and `MeatLabelOverride` on the Mod Body Harvest Component.
- `PreventAllDamageFromSource`, `FirstPersonPrefabMale` and `FirstPersonPrefabFemale` on the Mod Clothing Component.

### Fixed

- Exported loot tables no longer have an extra `LootTable` in front of their names.
- The Hinterland data assets are no longer duplicated.
- The auto updater updates the SDK package properly.

## [1.0.3] - 2024-07-13

### Added

- Item Rarities support: a Mod Item Rarities asset, under **Misc** on a Mod Definition, assigns your items to the mod's rarities.

### Fixed

- Some exported fields were `null` instead of `""`.

## [1.0.2] - 2024-06-27

### Changed

- The data assets cover the items, regions, sounds and loot tables of The Long Dark 2.31. Delete `_ModComponent/DataAssets/Hinterland` before generating them again.
- The supported versions in the About window are easier to read.

### Deprecated

- The auto updater, since the SDK checks for updates when the editor starts.
- The [ExampleMod SDK](https://github.com/Deaadman/ExampleModSDK), since the wiki covers the same ground.

### Removed

- The contributors window and its button in the About window.

## [1.0.1] - 2024-04-09

### Added

- **Automatically Check for Updates** and **Check for Preview Updates** options in the auto updater, both off by default.
- A `Units` choice on Mod Blueprints, to measure in a count or in kilograms.

### Changed

- **Breaking:** The Harvestable, Millable and Repairable behaviours take their yields, requirements and materials as single arrays, so prefabs using them need setting up again.
- Mod Blueprints take kerosene and gunpowder as `LiquidRequirement` and `PowderRequirement`.

### Fixed

- Items no longer lose their components with "The referenced script on this Behaviour is missing!".
- Blank Inlet (`CanneryRegion`) is in the scene data assets.
- The auto updater no longer takes a `v` in front of a version as a new update.
- Components no longer throw when their **Normal Model** or **Inspect Model** is empty, and export both models.
- Components export empty audio fields as `""`.

## [1.0.0] - 2024-02-24

### Added

- The first release.

[Unreleased]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.5...HEAD
[1.0.5]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.4...1.0.5
[1.0.4]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.3...1.0.4
[1.0.3]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.2...1.0.3
[1.0.2]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.1...1.0.2
[1.0.1]: https://github.com/Deaadman/mod-component-sdk/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/Deaadman/mod-component-sdk/releases/tag/1.0.0
