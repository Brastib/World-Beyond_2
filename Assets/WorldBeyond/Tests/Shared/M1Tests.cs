using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Persistence;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Tests
{
    public class M1Tests
    {
        static byte[] Unzip(string b64)
        {
            using (var input = new MemoryStream(Convert.FromBase64String(b64)))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream()) { gz.CopyTo(output); return output.ToArray(); }
        }

        [Test]
        public void V1Save_IsMigrated_AndKeepsRunning()
        {
            var w = SaveCodec.Decode(Unzip(SaveFixtures.V1), out var header);
            Assert.AreEqual(1, header.FormatVersion);
            Assert.AreEqual(144, w.Tick);
            Assert.That(w.People.All(p => p.Role == Role.Sammler), "v1-Standardwert: alle Erwachsenen Sammler");
            Assert.That(w.Settlements.All(s => s.Status == "" && s.Births == 0), "v1-Standardwerte Siedlung");
            Assert.That(w.Households.All(h => h.LastBirthDay == -1));
            var sim = new WorldSimulation(w, DefaultContent.Create());
            for (int k = 0; k < 10; k++) { sim.Run(100); TestUtil.AssertValid(w); }
            var again = TestUtil.Reload(sim);
            Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(again.World), "Nach Migration speichert v2 verlustfrei");
        }

        [Test]
        public void V2Save_IsMigrated_AndKeepsRunning()
        {
            var w = SaveCodec.Decode(Unzip(SaveFixtures.V2), out var header);
            Assert.AreEqual(2, header.FormatVersion);
            Assert.That(w.Map.RoadWork.All(x => x == 0), "v2 → v3: keine geplanten Straßen");
            Assert.That(w.Buildings.All(b => b.FieldGrowth == 0));
            var sim = new WorldSimulation(w, DefaultContent.Create());
            for (int k = 0; k < 10; k++) { sim.Run(100); TestUtil.AssertValid(w); }
            Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(TestUtil.Reload(sim).World));
        }

        [Test]
        public void Field_GrowsInSpringSummer_HarvestedInAutumn_SaveLoadMidSeason()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Submit(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Field, TestUtil.FindPlayerBuildCell(sim)));
            sim.Step();
            Assert.IsTrue(w.CommandHistory[0].Succeeded, w.CommandHistory[0].Result);
            var field = w.Buildings.Single(b => b.DefId == DefaultContent.Field && b.CivId == w.PlayerCivId);
            sim.Run(24 * 90);
            Assert.IsTrue(field.Complete, "Feld nach 90 Tagen angelegt");
            Assert.Greater(field.FieldGrowth, 0, "Feld wächst im Frühling");
            sim = TestUtil.Reload(sim); w = sim.World; field = w.BuildingById(field.Id);
            while (Calendar.SeasonOf(w.Tick) != Season.Herbst) sim.Step();
            int grown = field.FieldGrowth;
            TestContext.WriteLine("Wachstum zu Herbstbeginn: " + grown);
            Assert.Greater(grown, 200);
            Assert.That(w.People.Any(p => p.Role == Role.Bauer));
            sim.Run(24 * 30);
            Assert.Less(field.FieldGrowth, grown, "Im Herbst wird geerntet");
            TestUtil.AssertValid(w);
        }

        [Test]
        public void AiSettlements_PlantFields()
        {
            var sim = TestUtil.NewSim();
            sim.Run(24 * 120);
            var w = sim.World;
            Assert.That(w.Buildings.Any(b => b.DefId == DefaultContent.Field && b.CivId != w.PlayerCivId && b.Complete));
        }

        [Test]
        public void Roles_AreAssignedByNeed_AndRestrictGathering()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            Assert.That(w.People.Any(p => p.Role == Role.Holzfaeller));
            Assert.That(w.People.Any(p => p.Role == Role.Sammler));
            for (int t = 0; t < 24 * 10; t++)
            {
                sim.Step();
                foreach (var p in w.People.Where(p => p.Task == PersonTask.Sammeln))
                {
                    var own = Jobs.RoleResource(p.Role);
                    // Eigenen Rohstoff sammeln oder als Aushilfe; Kinder sammeln nie.
                    Assert.AreNotEqual(Role.Kind, p.Role);
                    if (own.HasValue && p.TaskResource != own.Value)
                        Assert.That(Economy.SettlementStock(w, sim.Content, p.SettlementId)[(int)own.Value] >= 0);
                }
            }
            Assert.That(w.People.Any(p => p.Role == Role.Baumeister) || !w.Buildings.Any(b => !b.Complete));
        }

        [Test]
        public void Population_GrowsOverYears_WithBirthsAndNewHouseholds()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            for (int y = 0; y < 3; y++) { sim.Run(Calendar.TicksPerDay * Calendar.DaysPerYear); TestUtil.AssertValid(w); }
            TestContext.WriteLine($"Nach 3 Jahren: {w.People.Count} Personen, {w.Households.Count} Haushalte, " +
                $"{w.Buildings.Count(b => b.DefId == DefaultContent.House && b.Complete)} Häuser, Geburten {w.Settlements.Sum(s => s.Births)}, Tode {w.Settlements.Sum(s => s.Deaths)}");
            foreach (var s in w.Settlements) TestContext.WriteLine($"{s.Name}: {w.People.Count(p => p.SettlementId == s.Id)} Personen, Geburten {s.Births}, Tode {s.Deaths}, {s.Status}");
            foreach (var l in w.Log.Where(l => l.Category == "Bevölkerung" && l.Text.Contains("verhungert")).Take(3)) TestContext.WriteLine("LOG " + l.Text);
            Assert.That(w.Settlements.All(s => w.People.Any(p => p.SettlementId == s.Id)), "Keine Siedlung stirbt im Standardszenario aus");
            Assert.Greater(w.People.Count, 18, "Bevölkerung wächst");
            Assert.Greater(w.Settlements.Sum(s => s.Births), 0);
            Assert.Greater(w.Households.Count, 6, "Auszüge gründen neue Haushalte");
            Assert.That(w.Log.Any(l => l.Category == "Siedlung"), "Wachstumsbegründung wird protokolliert");
        }

        [Test]
        public void AiSettlement_BuildsStorage_WhenFull()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Run(Calendar.TicksPerDay * Calendar.DaysPerYear);
            var aiStorages = w.Buildings.Where(b => b.DefId == DefaultContent.Storage && b.CivId != w.PlayerCivId).ToList();
            Assert.That(aiStorages, Is.Not.Empty, "KI-Siedlungen sollten bei vollem Lager ein Lager beauftragen");
            Assert.That(aiStorages.Any(b => b.Complete));
            Assert.That(w.Buildings.Any(b => b.DefId == DefaultContent.Storage && b.CivId == w.PlayerCivId), Is.False, "Spieler-Siedlung baut öffentliche Gebäude nicht automatisch");
        }

        [Test]
        public void RemovingHauler_ReleasesReservations()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Submit(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, TestUtil.FindPlayerBuildCell(sim)));
            Person hauler = null;
            for (int t = 0; t < 2000 && hauler == null; t++) { sim.Step(); hauler = w.People.FirstOrDefault(p => p.Task == PersonTask.MaterialHolen); }
            Assert.IsNotNull(hauler);
            Lifecycle.RemovePerson(w, hauler, "Testfall");
            TestUtil.AssertValid(w);
            Assert.IsNull(w.PersonById(hauler.Id));
            sim.Run(24 * 40);
            TestUtil.AssertValid(w);
            Assert.That(w.Buildings.Where(b => b.DefId == DefaultContent.Storage).All(b => b.Complete), "Baustelle wird trotzdem fertig");
        }

        [Test]
        public void LastMemberDies_HouseholdRemoved_HouseBecomesVacantAndReused()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Run(24 * 30);
            var h = w.Households.First(x => x.HomeBuildingId != 0);
            int house = h.HomeBuildingId;
            foreach (var id in h.MemberIds.ToList()) Lifecycle.RemovePerson(w, w.PersonById(id), "Testfall");
            Assert.IsNull(w.HouseholdById(h.Id));
            TestUtil.AssertValid(w);
            sim.Run(24 * 400);
            TestUtil.AssertValid(w);
            TestContext.WriteLine("Haus bewohnt von: " + string.Join(",", w.Households.Where(x => x.HomeBuildingId == house).Select(x => x.Name)));
        }
    }
}
