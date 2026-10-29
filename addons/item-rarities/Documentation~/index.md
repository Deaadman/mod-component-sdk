# Item Rarities add-on

Gives items a rarity for the Item Rarities mod. With it on, a mod's build writes `rarities.ir`, which lists the mod's items under each rarity.

## Install

Install the ModComponent SDK first. Then, in **Window › Package Manager**, choose **+ › Install package from git URL…** and enter the same version as your SDK:

```
https://github.com/Deaadman/mod-component-sdk.git?path=addons/item-rarities#2.0.0
```

The Mod Hub's update banner updates the add-on along with the SDK.

## Use

1. Turn on **Item Rarities** under **Integrations** on the Mod, or tick **Item Rarities support** in **New Mod**.
2. Each item of that mod gets an **Item Rarities** section. Tick **Has a rarity** and pick one: Common, Uncommon, Rare, Epic, Legendary or Mythic.
3. Build. Items without a rarity aren't listed in `rarities.ir`, and the Issues list points them out.

Mods made with SDK 1.x keep their rarities when they're imported with **Import 1.x Mods…**, as long as this add-on is installed first.

## Coming from an earlier 2.0 nightly

Item Rarities used to be built into the SDK. Install this add-on and your mods' rarities load as before. Until it's installed, the Mod's Issues list warns that its rarity data can't be loaded, and the build leaves `rarities.ir` out.
