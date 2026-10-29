# ModComponent SDK 2.0 guide

## How a mod is put together

| Asset | What it is |
| - | - |
| **Mod** | One `.modcomponent` file: name, id, author, version, required mods, and the lists below. |
| **GearItem** | One item. Points at a model prefab and holds the item's data as modules. |
| **Blueprint** | A crafting blueprint, or with *Cooking recipe* on, a cooking recipe. |
| **Localization table** | Keys and their text per language. English is the fallback. |
| **Spawn table** | Fixed spawn points in scenes, and entries added to vanilla loot tables. |

An item has exactly **one component** (what the item is: Food, Tool, Clothing…), any number of **behaviours** (extra abilities: Stackable, Burnable, Repairable…), and optional **modifications** (Change Layer). Accelerant, Burnable, Fire Starter and Tinder can't be combined, because ModComponent only loads one of them.

## Your first item

1. **ModComponent SDK › Mod Hub › New › Mod…**. Give it a name; everything can be changed later.
2. **New › Item…**. Pick a model (FBX or prefab), a display name and a template. The SDK creates:
   - `GEAR_<Name>.prefab`: an empty root with a box collider, and your model as a child.
   - `GEAR_<Name>.asset`: the item, with the template's modules.
   - `GAMEPLAY_<Name>` and `GAMEPLAY_<Name>Description` keys in the mod's localization table.
3. Select the item in the Hub. Set values on each card's tabs; units are shown in the labels. Use **Render icon** for an inventory icon, and drag the **Inspect preview** to line up the inspect view, then **Use current view**.
4. Check the **Issues** list at the bottom. Errors stop the build, and most have a **Fix** button.
5. **Build…**. Choose an output folder and, optionally, your game folder to copy the file into `Mods`.

## Names and references

Fields that name something in the game (gear, sounds, scenes, loot tables, liquids, powders) are text with a **▾** picker. The picker lists vanilla names and your project's items. A yellow dot means the name isn't in either list, which is fine for items from another mod: add that mod to **Required mods**.

The bundled list of vanilla names was taken from The Long Dark 2.31, so things added since then won't appear in the picker yet. You can still type them.

## What the build produces

```
<Id>.modcomponent
├─ buildinfo.json
├─ bundle/          AssetBundles + JSON catalog
├─ auto-mapped/     one JSON per item, named in lower case without GEAR_
├─ blueprints/      recipes/
├─ localizations/   gear-spawns/
└─ rarities.ir      when Item Rarities is on
```

Builds need, once per project: the Windows build target, and JSON Addressables catalogs (the build offers to turn them on).

## Things ModComponent does that the SDK handles for you
- A missing JSON key becomes zero or empty in game, not the default, so every field is always written.
- One bad entry stops the rest of the archive loading, so only paths the loader accepts are packaged, and the most important ones go first.
- Gear spawn names may only use letters, digits and `_`, and numbers are always written with `.`.
- Clothing's first-person male and female prefabs are swapped by the loader; the SDK swaps them back on export.
- Cookable and Food items always load their cooking result, even with cooking off; an empty one is filled with a safe vanilla item.

## Preferences
**Edit › Preferences › ModComponent SDK**: game folder, default author, update checks (including nightlies), and the project's status.
