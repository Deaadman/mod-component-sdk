# Changelog

## [2.0.0] - unreleased

A rewrite of the SDK. Projects made with 1.x can be imported (see "Migrating" below).

### Added
- **Mod Hub** window: every mod with items, blueprints and tables, issue badges, search, New / Validate / Build.
- `GearItem` assets hold an item's data as module cards (one component, any behaviours, modifications), so prefabs stay pure visuals.
- Reference pickers for gear, sounds, scenes, loot tables, liquids, powders, localization keys and prefab children, backed by a bundled game-data catalog.
- Validation rules with one-click fixes, run before every build.
- Localization window (spreadsheet view, CSV import/export, key renames that update every use).
- Spawns window (coordinate paste, file preview) and Build window (steps, log, copy to the game's Mods folder).
- Inspect preview and inventory icon renderer.
- New Mod / New Item wizards with 14 item templates, and **Create Example Mod**.
- **Import 1.x Mods** window with a dry run.
- Update banner in the Hub that installs releases by tag, and Preferences › ModComponent SDK.
- Item Rarities support as an optional per-mod integration.

### Changed
- Targets ModComponent 7.0.0 and The Long Dark 2.58 and requires Unity **6000.0.60f1** exactly.
- Builds stage in `Library/`, never modify prefabs, use a temporary Addressables group, and restore the project's Addressables settings afterwards.
- Item JSON writes every field the loader reads, using its exact key names. Known loader bugs are compensated on export: Clothing's first-person prefabs are swapped back, and an empty `CookingResult` is filled.
- Vanilla item icons moved to the optional **Vanilla Icons** sample.
- JSON goes through `com.unity.nuget.newtonsoft-json`, not the Version Control package's internal copy.

### Fixed (from 1.x)
- Mods whose names contain spaces weren't included in their own Addressables build.
- Gear spawn coordinates were written with the PC's regional decimal separator.
- The Bed component exported the normal model's name for both its packed and usable meshes.
- The prefab validator always reported success.
- An interrupted build could leave prefabs without their components.
- Deleting a mod definition deleted its whole folder.
- The update checker never unsubscribed, installed from an old URL without a tag, and treated older tags as updates.

### Removed
- The 1.x editors, Mod Manager, validators and data-asset generator. The 1.x serialized types remain in `Legacy/` so old projects still load; they will be removed in 3.0.

### Migrating
1. Install 2.0 over 1.x.
2. **ModComponent SDK › Import 1.x Mods…** → **Dry run**, check the notes, then **Import**.
3. Review the new mod in the Hub, then **Remove 1.x scripts** from the prefabs.

## [1.0.6]
Last 1.x release. See the [releases page](https://github.com/Deaadman/mod-component-sdk/releases).
