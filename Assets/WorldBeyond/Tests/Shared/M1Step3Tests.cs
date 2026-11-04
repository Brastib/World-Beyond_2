using System;
using System.Collections.Generic;
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
    /// <summary>M1 Schritt 3: Zufriedenheit, Baupriorität, monatlicher Verlauf, Save v4.</summary>
    public class M1Step3Tests
    {
        static byte[] Unzip(string b64)
        {
            using (var input = new MemoryStream(Convert.FromBase64String(b64)))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream()) { gz.CopyTo(output); return output.ToArray(); }
        }

        [Test]
        public void V3Save_IsMigrated_AndKeepsRunning()
        {
            var w = SaveCodec.Decode(Unzip(SaveFixtures.V3), out var header);
            Assert.AreEqual(3, header.FormatVersion);
            Assert.That(w.People.All(p => p.Satisfaction == 60), "v3 → v4: Zufriedenheit startet bei 60");
            Assert.That(w.Buildings.All(b => !b.HighPriority), "v3 → v4: keine Vorrang-Baustellen");
            Assert.That(w.Settlements.All(s => s.History.Count == 0), "v3 → v4: leerer Verlauf");
            var sim = new WorldSimulation(w, DefaultContent.Create());
            for (int k = 0; k < 10; k++) { sim.Run(100); TestUtil.AssertValid(w); }
            Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(TestUtil.Reload(sim).World));
        }

        [Test]
        public void Target_ListsReasons_AndDailyStepIsLimited()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            var p = w.People.First();
            p.Hunger = 100; p.Health = 30;
            var reasons = new List<string>();
            int t = Satisfaction.Target(w, sim.Content, p, reasons);
            Assert.That(reasons, Does.Contain("-25 hungert"));
            Assert.That(reasons, Does.Contain("-10 krank"));
            Assert.That(t, Is.LessThan(Satisfaction.LowThreshold));
            Assert.AreEqual(Math.Max(0, Math.Min(100, Satisfaction.Base + reasons.Sum(r => int.Parse(r.Split(' ')[0])))), t);

            foreach (var q in w.People) q.Satisfaction = 0;
            sim.Run(Calendar.TicksPerDay);
            Assert.That(w.People.All(q => q.Satisfaction <= Satisfaction.DailyStep), "höchstens 5 Punkte pro Tag");
            TestUtil.AssertValid(w);
        }

        [Test]
        public void Discontent_BlocksBirths()
        {
            var normal = TestUtil.NewSim();
            var unhappy = TestUtil.NewSim();
            for (int d = 0; d < Calendar.DaysPerYear * 2; d++)
            {
                foreach (var p in unhappy.World.People) p.Satisfaction = 0;
                normal.Run(Calendar.TicksPerDay);
                unhappy.Run(Calendar.TicksPerDay);
            }
            Assert.Greater(normal.World.Settlements.Sum(s => s.Births), 0, "Vergleichslauf hat Geburten");
            Assert.AreEqual(0, unhappy.World.Settlements.Sum(s => s.Births), "unzufrieden: keine Geburten");
            Assert.That(unhappy.World.Settlements.Any(s => s.Status.Contains("unzufrieden")), "Status nennt die Ursache");
            TestUtil.AssertValid(unhappy.World);
        }

        [Test]
        public void Discontent_SlowsWork()
        {
            var normal = TestUtil.NewSim();
            var unhappy = TestUtil.NewSim();
            for (int d = 0; d < 20; d++)
            {
                foreach (var p in unhappy.World.People) p.Satisfaction = 0;
                normal.Run(Calendar.TicksPerDay);
                unhappy.Run(Calendar.TicksPerDay);
            }
            int a = normal.World.People.Sum(p => p.TotalGathered), b = unhappy.World.People.Sum(p => p.TotalGathered);
            Assert.Less(b, a, $"unzufrieden gesammelt {b}, normal {a}");
        }

        [Test]
        public void Priority_SiteIsServedFirst_AndReplays()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            int civ = w.PlayerCivId;
            sim.Submit(GameCommand.PlaceBuilding(civ, DefaultContent.Field, TestUtil.FindPlayerBuildCell(sim, 3)));
            sim.Step();
            sim.Submit(GameCommand.PlaceBuilding(civ, DefaultContent.Field, TestUtil.FindPlayerBuildCell(sim, 4)));
            sim.Step();
            var fields = w.Buildings.Where(b => b.DefId == DefaultContent.Field && b.CivId == civ).OrderBy(b => b.Id).ToList();
            Assert.AreEqual(2, fields.Count, string.Join("; ", w.CommandHistory.Select(c => c.Result)));
            var first = fields[0]; var second = fields[1];
            sim.Submit(GameCommand.SetPriority(civ, second.Id, true));
            sim.Step();
            Assert.IsTrue(w.CommandHistory.Last().Succeeded, w.CommandHistory.Last().Result);
            Assert.IsTrue(second.HighPriority);

            int guard = 0;
            while (!second.Complete && guard++ < 24 * 120) sim.Step();
            Assert.IsTrue(second.Complete, "Vorrang-Feld fertig");
            Assert.IsFalse(first.Complete, "das normale Feld ist noch nicht fertig, wenn das Vorrang-Feld fertig wird");
            TestUtil.AssertValid(w);

            Assert.IsNotNull(sim.Preview(GameCommand.SetPriority(civ, second.Id, false)), "fertiges Gebäude abgelehnt");
            var foreign = w.Buildings.First(b => b.CivId != civ);
            Assert.IsNotNull(sim.Preview(GameCommand.SetPriority(civ, foreign.Id, true)), "fremdes Gebäude abgelehnt");
            Assert.IsNotNull(sim.Preview(GameCommand.SetPriority(civ, 999999, true)), "unbekanntes Gebäude abgelehnt");

            var fresh = TestUtil.NewSim();
            WorldSimulation.Replay(fresh, w.CommandHistory, w.Tick);
            Assert.AreEqual(SaveCodec.StateHash(w), SaveCodec.StateHash(fresh.World), "Replay mit Prioritätsbefehl identisch");
        }

        [Test]
        public void History_RecordsMonthly_IsCapped_AndSurvivesSaveLoad()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Run(Calendar.TicksPerDay * Calendar.DaysPerMonth * 3);
            foreach (var s in w.Settlements)
            {
                Assert.AreEqual(3, s.History.Count, s.Name);
                Assert.That(s.History.Last().Population, Is.EqualTo(w.People.Count(p => p.SettlementId == s.Id)));
                Assert.That(s.History.Select(h => h.Tick), Is.Ordered);
            }
            var cap = w.Settlements[0];
            while (cap.History.Count < HistoryPoint.MaxPoints) cap.History.Insert(0, new HistoryPoint { Tick = 0 });
            sim.Run(Calendar.TicksPerDay * Calendar.DaysPerMonth);
            Assert.AreEqual(HistoryPoint.MaxPoints, cap.History.Count, "Obergrenze");
            Assert.AreEqual(w.Tick, cap.History.Last().Tick, "neuester Punkt hinten");

            var again = TestUtil.Reload(sim);
            Assert.AreEqual(SaveCodec.StateHash(w), SaveCodec.StateHash(again.World));
            Assert.AreEqual(HistoryPoint.MaxPoints, again.World.Settlements[0].History.Count);
            Assert.AreEqual(w.People.Select(p => p.Satisfaction), again.World.People.Select(p => p.Satisfaction));
        }
    }
}
