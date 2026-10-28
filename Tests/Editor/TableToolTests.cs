using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using NUnit.Framework;
using UnityEngine;

namespace ModComponent.SDK.Editor.Tests
{
    public class LocalizationCsvTests
    {
        private static readonly string[] Languages = { "English", "German", "French (France)" };

        [Test]
        public void Export_ThenImport_RoundTripsTrickyText()
        {
            var source = ScriptableObject.CreateInstance<LocalizationTable>();
            source.Set("GAMEPLAY_Note", "English", "Line one,\n\"quoted\" line two");
            source.Set("GAMEPLAY_Note", "French (France)", "Note, avec virgule");
            source.Set("GAMEPLAY_Tea", "English", "Pine Tea");

            var csv = LocalizationCsv.Export(source, Languages);
            var target = ScriptableObject.CreateInstance<LocalizationTable>();
            var result = LocalizationCsv.Import(target, csv, Languages);

            Assert.That(result.Added, Is.EqualTo(2));
            Assert.That(target.Get("GAMEPLAY_Note"), Is.EqualTo("Line one,\n\"quoted\" line two"));
            Assert.That(target.Get("GAMEPLAY_Note", "French (France)"), Is.EqualTo("Note, avec virgule"));
            Assert.That(target.Get("GAMEPLAY_Tea", "German"), Is.Null);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Import_MergesAndSkipsUnknownColumns()
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.Set("GAMEPLAY_Tea", "English", "Tea");
            table.Set("GAMEPLAY_Tea", "German", "Tee");

            var result = LocalizationCsv.Import(table, "﻿Key,English,German,Klingon\r\nGAMEPLAY_Tea,Pine Tea,,x\r\nGAMEPLAY_New,New,,\r\n", Languages);

            Assert.That(result.Added, Is.EqualTo(1));
            Assert.That(result.Updated, Is.EqualTo(1));
            Assert.That(result.UnknownLanguages, Is.EqualTo(new[] { "Klingon" }));
            Assert.That(table.Get("GAMEPLAY_Tea"), Is.EqualTo("Pine Tea"));
            Assert.That(table.Get("GAMEPLAY_Tea", "German"), Is.EqualTo("Tee"), "empty cells keep existing text");
            Object.DestroyImmediate(table);
        }

        [Test]
        public void Import_RequiresKeyColumn()
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            Assert.Throws<System.FormatException>(() => LocalizationCsv.Import(table, "English,German\nTea,Tee\n", Languages));
            Object.DestroyImmediate(table);
        }
    }

    public class LocalizationRefactorTests
    {
        [Test]
        public void RenameKey_UpdatesItemsAndRecipes()
        {
            var mod = ScriptableObject.CreateInstance<Mod>();
            mod.Localization = ScriptableObject.CreateInstance<LocalizationTable>();
            mod.Localization.Set("GAMEPLAY_Hat", "English", "Hat");
            var item = ScriptableObject.CreateInstance<GearItem>();
            item.AddModule(new SDK.Modules.GenericComponent { DisplayNameLocalizationId = "GAMEPLAY_Hat" });
            mod.Items.Add(item);
            var recipe = ScriptableObject.CreateInstance<Blueprint>();
            recipe.RecipeName = "GAMEPLAY_Hat";
            mod.Blueprints.Add(recipe);

            var updated = LocalizationRefactor.RenameKey(mod, "GAMEPLAY_Hat", "GAMEPLAY_WoolHat");

            Assert.That(updated, Is.EqualTo(2));
            Assert.That(mod.Localization.Get("GAMEPLAY_WoolHat"), Is.EqualTo("Hat"));
            Assert.That(item.Component.DisplayNameLocalizationId.Id, Is.EqualTo("GAMEPLAY_WoolHat"));
            Assert.That(recipe.RecipeName.Id, Is.EqualTo("GAMEPLAY_WoolHat"));
            foreach (var o in new Object[] { mod.Localization, item, recipe, mod }) Object.DestroyImmediate(o);
        }
    }

    public class CoordinateParserTests
    {
        [TestCase("p=412.3,18.75,-120.4 r=0,90,0", 412.3f, 18.75f, -120.4f, true)]
        [TestCase("item=GEAR_X p=1,2,3 c=50", 1f, 2f, 3f, false)]
        [TestCase("(412.3, 18.8, -120.4)", 412.3f, 18.8f, -120.4f, false)]
        [TestCase("412,3 18,75 -120,4", 412.3f, 18.75f, -120.4f, false)]
        [TestCase("Position: 1.5 2.5 3.5", 1.5f, 2.5f, 3.5f, false)]
        [TestCase("1 2 3 0 90 0", 1f, 2f, 3f, true)]
        public void Parses(string text, float x, float y, float z, bool hasRotation)
        {
            Assert.That(CoordinateParser.TryParse(text, out var position, out var rotation), Is.True);
            Assert.That(Vector3.Distance(position, new Vector3(x, y, z)), Is.LessThan(0.001f), position.ToString("F3"));
            Assert.That(rotation.HasValue, Is.EqualTo(hasRotation));
        }

        [TestCase("")]
        [TestCase("1, 2")]
        [TestCase("no numbers here")]
        public void Rejects(string text) => Assert.That(CoordinateParser.TryParse(text, out _, out _), Is.False);
    }
}
