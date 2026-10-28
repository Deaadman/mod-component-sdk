<div align="center">

<img src="https://raw.githubusercontent.com/Deaadman/mod-component-sdk/main/Documentation~/images/TitleCardGitHub.png" alt="ModComponent SDK Banner">

![Latest Release](https://img.shields.io/github/v/release/Deaadman/mod-component-sdk?style=for-the-badge&label=Latest%20Release&include_prereleases)

</div>

## General Information

The ModComponent SDK is a Unity package for making `.modcomponent` item mods for The Long Dark. You describe items, blueprints, localization and spawns in the editor, the SDK checks them against what the ModComponent loader actually accepts, and builds a ready-to-install `.modcomponent` file.

| | Version |
| - | - |
| The Long Dark | 2.58 |
| ModComponent | 7.0.0 |
| Unity | **6000.0.60f1** exactly (the version the game is built with) |

### Features
- **Mod Hub**: one window listing every mod in the project with its items, blueprints and tables, issue counts on each row, and the selected asset's inspector alongside it.
- **Items as assets**: each item is a `GearItem` asset with one component and any behaviours, shown as cards with tabs, units and tooltips. Prefabs stay pure visuals, and the build never modifies them.
- **Pickers for game names**: gear, sounds, scenes, loot tables, liquids, powders, localization keys and prefab children all come from searchable lists, and typing a name from another mod still works.
- **Validation with fixes**: dozens of checks based on the ModComponent source, such as prefab names, missing icons, loader traps and invalid spawn lines, most with a one-click fix. A build won't start while there are errors.
- **Safe builds**: builds are staged in `Library/`, use a temporary Addressables group, and restore your project's Addressables settings afterwards. A build can also copy the result straight into the game's `Mods` folder.
- **Localization table** with a spreadsheet view and CSV import/export for translators.
- **Spawns editor** with a coordinate paste box and a preview of the exact file the build writes.
- **Inspect preview and icon renderer**: line up the inspect view and render an inventory icon from the model.
- **New Mod / New Item wizards** with 14 item templates.
- **1.x importer**: brings mods made with SDK 1.x across without touching the originals.
- **Update banner**: shows new releases (including nightlies) in the Hub and installs a specific tag.

## Getting Started

1. Install Unity **6000.0.60f1** from Unity Hub and create a project.
2. In **Window › Package Manager**, choose **+ › Install package from git URL…** and enter:
   ```
   https://github.com/Deaadman/mod-component-sdk.git#v2.0.0
   ```
   For the latest nightly, use its tag instead, e.g. `#v2.0.0-nightly.1`.
3. Switch the build target to **Windows** in **File › Build Profiles**.
4. Open **ModComponent SDK › Mod Hub**, then **New › Mod…**, or **ModComponent SDK › Create Example Mod** to start from a working example.
5. Build with **Build…** in the Hub. The first build offers to turn on JSON catalogs, which ModComponent needs; Unity recompiles once.

See [the guide](Documentation~/index.md) for a walkthrough and the [Wiki](https://github.com/Deaadman/mod-component-sdk/wiki) for reference pages.

### Coming from 1.x?
Open **ModComponent SDK › Import 1.x Mods…**, run a dry run, then import. Your 1.x assets are left as they are. After checking the result, remove the old scripts from your prefabs with the button in that window. A validation rule also points out any you missed.

## Other Resources
Older guides written for 1.x and for hand-made ModComponent mods are still useful background, but field names and workflows differ:

| Type | Author | Last Updated |
| - | - | - |
| [ModComponent Basic Guide](https://github.com/TLD-Mods/Guides/blob/main/ModComponent%20Basic%20Guide.md) | [Jods-Its](https://github.com/Jods-Its) | 26th June 2023 |
| [ModComponent Documentation](https://github.com/dommrogers/ModComponent/blob/master/docs/index.md) | [STBlade](https://github.com/dommrogers) | 22nd January 2024 |

## Support
Questions and feedback are welcome on the [**Discord**](https://discord.gg/2mnXAZfGXQ). Bugs and feature requests go in [**Issues**](https://github.com/Deaadman/mod-component-sdk/issues). To work on the SDK itself, see [CONTRIBUTING.md](CONTRIBUTING.md).
