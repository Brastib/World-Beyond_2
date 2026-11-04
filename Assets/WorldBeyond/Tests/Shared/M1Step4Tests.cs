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
    /// <summary>M1 Schritt 4: Produktionskette Holz + Stein → Werkstatt → Werkzeug → schnellere Arbeit, Save v5.</summary>
    public class M1Step4Tests
    {
        static byte[] Unzip(string b64)
        {
            using (var input = new MemoryStream(Convert.FromBase64String(b64)))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream()) { gz.CopyTo(output); return output.ToArray(); }
        }

        static int Tools(WorldSimulation sim, int sid) =>
            Economy.SettlementAvailable(sim.World, sim.Content, sid, ResourceType.Werkzeug);

        static Building PlaceWorkshop(WorldSimulation sim)
        {
            var w = sim.World;
            var s = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId);
            var tc = w.Buildings.First(b => b.SettlementId == s.Id && b.DefId == DefaultContent.TownCenter);
            tc.Stock[(int)ResourceType.Holz] += 40; tc.Stock[(int)ResourceType.Stein] += 30; // genug für Bau und erste Werkstücke
            var cmd = sim.Submit(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Workshop, TestUtil.FindPlayerBuildCell(sim)));
            sim.Step();
            Assert.IsTrue(w.CommandHistory.Last().Succeeded, w.CommandHistory.Last().Result);
            return w.Buildings.Last(b => b.DefId == DefaultContent.Workshop);
        }

        [Test]
        public void V4Save_IsMigrated_AndKeepsRunning()
        {
            var w = SaveCodec.Decode(Unzip(SaveFixtures.V4), out var header);
            Assert.AreEqual(4, header.FormatVersion);
            Assert.That(w.People.All(p => p.ToolUses == 0), "v4 → v5: niemand hat Werkzeug");
            Assert.That(w.Buildings.All(b => b.CraftProgress == 0 && b.Stock.Length == Res.Count && b.Stock[(int)ResourceType.Werkzeug] == 0), "v4 → v5: Werkzeug 0");
            var sim = new WorldSimulation(w, DefaultContent.Create());
            for (int k = 0; k < 10; k++) { sim.Run(100); TestUtil.AssertValid(w); }
            var again = TestUtil.Reload(sim);
            Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(again.World));
        }

        [Test]
        public void Workshop_TurnsWoodAndStoneIntoTools_WithoutCreatingGoods()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            var shop = PlaceWorkshop(sim);
            int sid = shop.SettlementId;
            for (int d = 0; d < 40 && !shop.Complete; d++) { sim.Run(24); TestUtil.AssertValid(w); }
            Assert.IsTrue(shop.Complete, "Werkstatt in 40 Tagen fertig");
            bool sawCrafter = false;
            for (int d = 0; d < 20; d++)
            {
                for (int h = 0; h < 24; h++)
                {
                    sim.Step();
                    sawCrafter |= w.People.Any(p => p.Role == Role.Handwerker && p.Task == PersonTask.Handwerk);
                }
                TestUtil.AssertValid(w);
            }
            Assert.IsTrue(sawCrafter, "Ein Handwerker arbeitet in der Werkstatt");
            int made = Tools(sim, sid) + w.People.Where(p => p.SettlementId == sid).Sum(p => p.ToolUses > 0 ? 1 : 0)
                       + w.People.Count(p => p.CarryType == ResourceType.Werkzeug && p.CarryAmount > 0);
            Assert.That(made, Is.GreaterThanOrEqualTo(2), "Werkzeug wurde hergestellt (im Lager, in Benutzung oder unterwegs)");
            // Werkzeug entsteht nur aus dem Rezept: kein Werkzeug ohne vorher verbrauchtes Holz/Stein in der Werkstatt.
            Assert.That(w.Buildings.Where(b => b.DefId != DefaultContent.Workshop && !Economy.IsStorage(b, sim.Content)).All(b => b.Stock.Sum() == 0));
            Assert.That(shop.Stock[(int)ResourceType.Werkzeug], Is.EqualTo(0));
        }

        [Test]
        public void Tools_DoubleWork_AndWearOut()
        {
            var a = TestUtil.NewSim();
            var b = TestUtil.NewSim();
            foreach (var tc in b.World.Buildings.Where(x => x.DefId == DefaultContent.TownCenter)) tc.Stock[(int)ResourceType.Werkzeug] = 12;
            a.Run(24 * 20); b.Run(24 * 20);
            TestUtil.AssertValid(a.World); TestUtil.AssertValid(b.World);
            Func<WorldSimulation, int> work = s => s.World.People.Where(p => p.Role == Role.Holzfaeller || p.Role == Role.Bergarbeiter).Sum(p => p.TotalGathered);
            Assert.That(b.World.People.Any(p => p.ToolUses > 0 && p.ToolUses < PersonSystem.ToolLife), "Werkzeug wird benutzt und nutzt sich ab");
            Assert.That(work(b), Is.GreaterThan(work(a)), $"Mit Werkzeug mehr Holz/Stein gesammelt ({work(b)} gegenüber {work(a)})");
        }

        [Test]
        public void SaveDuringCraft_LoadsAndContinuesIdentically()
        {
            var sim = TestUtil.NewSim();
            var shop = PlaceWorkshop(sim);
            for (int t = 0; t < 24 * 80 && shop.CraftProgress == 0; t++) sim.Step();
            Assert.That(shop.CraftProgress, Is.GreaterThan(0), "Werkstück in Arbeit");
            var copy = TestUtil.Reload(sim);
            Assert.AreEqual(shop.CraftProgress, copy.World.BuildingById(shop.Id).CraftProgress);
            sim.Run(200); copy.Run(200);
            TestUtil.AssertValid(copy.World);
            Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(copy.World));
        }

        [Test]
        public void AiSettlements_BuildWorkshop_AndProduceTools()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Run(24 * Calendar.DaysPerYear * 2);
            TestUtil.AssertValid(w);
            var ai = w.Settlements.Where(s => s.CivId != w.PlayerCivId).ToList();
            var report = string.Join("; ", ai.Select(s => $"{s.Name}: {w.People.Count(p => p.SettlementId == s.Id)} EW, Werkstatt {w.Buildings.Count(b => b.SettlementId == s.Id && b.Complete && b.DefId == DefaultContent.Workshop)}, Werkzeug {Tools(sim, s.Id)}, mit Werkzeug {w.People.Count(p => p.SettlementId == s.Id && p.ToolUses > 0)}"));
            TestContext.WriteLine(report);
            Assert.That(ai.Any(s => w.Buildings.Any(b => b.SettlementId == s.Id && b.Complete && b.DefId == DefaultContent.Workshop)), report);
            Assert.That(w.People.Any(p => p.ToolUses > 0) || ai.Any(s => Tools(sim, s.Id) > 0), report);
        }

        [Test]
        public void Singles_FormFamilies_AndStatusExplainsStagnation()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            bool family = false;
            for (int d = 0; d < 3 * Calendar.DaysPerYear; d++)
            {
                sim.Run(24);
                family |= w.Log.Any(l => l.Tick >= w.Tick - 24 && l.Text.Contains("gründen eine Familie"));
                if (d % 30 == 0) TestUtil.AssertValid(w);
            }
            Assert.That(family, "Alleinstehende bilden Familien");
            Assert.That(w.Households.All(h => h.MemberIds.Count > 0));
            // Seed 12345 nach 3 Jahren: Häuser voller Familien mit kleinen Kindern → Lagetext muss Stagnation nennen, nicht „Wächst“.
            var report = string.Join("; ", w.Settlements.Select(s => s.Name + ": " + s.Status));
            TestContext.WriteLine(report);
            foreach (var s in w.Settlements)
            {
                if (!s.Status.StartsWith("Wächst")) continue;
                Assert.That(Lifecycle.CanGrowSomehow(w, sim.Content, s.Id), s.Name + " meldet Wachstum ohne Möglichkeit: " + s.Status);
            }
            Assert.That(w.Settlements.Any(s => s.Status.StartsWith("Stagniert")), report);
        }
    }
}
