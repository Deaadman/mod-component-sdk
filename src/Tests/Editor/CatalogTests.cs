using System.Linq;
using ModComponent.SDK.Editor.Catalog;
using NUnit.Framework;

namespace ModComponent.SDK.Editor.Tests
{
    public class CatalogTests
    {
        [Test]
        public void DefaultCatalog_LoadsEveryList()
        {
            var catalog = GameDataCatalog.Default;

            Assert.That(catalog.Gear.Count, Is.GreaterThan(600));
            Assert.That(catalog.Sounds.Count, Is.GreaterThan(1000));
            Assert.That(catalog.Scenes, Is.Not.Empty);
            Assert.That(catalog.LootTables, Is.Not.Empty);
            Assert.That(catalog.Liquids, Does.Contain("LIQUID_WaterPotable"));
            Assert.That(catalog.Powders, Does.Contain("POWDER_Gunpowder"));
            Assert.That(catalog.GameVersion, Is.Not.Empty);
        }

        [Test]
        public void LootTables_MatchGearSpawnerNormalization()
        {
            var catalog = GameDataCatalog.Parse("{\"lootTables\":[\"LootTableVehicleGloveBox\",\"CargoContainer\"]}");

            Assert.That(catalog.Contains(CatalogKind.LootTable, "VehicleGloveBox"), Is.True);
            Assert.That(catalog.Contains(CatalogKind.LootTable, "loottablevehicleglovebox"), Is.True);
            Assert.That(catalog.Contains(CatalogKind.LootTable, "LootCargoContainer"), Is.True);
            Assert.That(catalog.Contains(CatalogKind.LootTable, "GloveBox"), Is.False);
        }

        [Test]
        public void DefaultCatalog_LanguagesStartWithEnglish()
        {
            var catalog = GameDataCatalog.Default;

            Assert.That(catalog.Languages.First(), Is.EqualTo(LocalizationTable.DefaultLanguage));
            Assert.That(catalog.Languages, Does.Contain("French (France)"));
            Assert.That(catalog.Languages, Is.Unique);
        }

        [Test]
        public void DefaultCatalog_GearIdsAreUniqueAndPrefixed()
        {
            var ids = GameDataCatalog.Default.Gear.Select(g => g.Id).ToList();

            Assert.That(ids, Is.Unique);
            Assert.That(ids, Has.All.StartWith(GearItem.GearPrefix));
        }

        [Test]
        public void Contains_MatchesScenesCaseInsensitively_AndGearExactly()
        {
            var catalog = GameDataCatalog.Parse(
                "{\"game\":\"2.58\",\"gear\":[{\"id\":\"GEAR_Knife\",\"source\":\"vanilla\"}],\"scenes\":[\"CoastalRegion\"]}");

            Assert.That(catalog.Contains(CatalogKind.Scene, "coastalregion"), Is.True);
            Assert.That(catalog.Contains(CatalogKind.Gear, "GEAR_Knife"), Is.True);
            Assert.That(catalog.Contains(CatalogKind.Gear, "gear_knife"), Is.False);
            Assert.That(catalog.Contains(CatalogKind.Gear, ""), Is.False);
            Assert.That(catalog.Gear[0].IsVanilla, Is.True);
        }
    }
}
