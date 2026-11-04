using System.Linq;
using NUnit.Framework;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Tests
{
    /// <summary>M0-Abnahme auf Simulationsebene.</summary>
    public class GameplayTests
    {
        [Test]
        public void Start_HasThreeSettlementsWithHouseholdsAndStock()
        {
            var w = TestUtil.NewSim().World;
            Assert.AreEqual(3, w.Civilizations.Count);
            Assert.AreEqual(3, w.Settlements.Count);
            Assert.AreEqual(6, w.Households.Count);
            Assert.AreEqual(18, w.People.Count);
            foreach (var s in w.Settlements)
            {
                var tc = w.Buildings.Single(b => b.SettlementId == s.Id);
                Assert.AreEqual(DefaultContent.TownCenter, tc.DefId);
                Assert.Greater(tc.Stock[0], 0); Assert.Greater(tc.Stock[1], 0); Assert.Greater(tc.Stock[2], 0);
            }
        }

        [Test]
        public void Household_GathersStoresBuildsAndMovesIntoHouse()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            sim.Run(Calendar.TicksPerDay * 20);
            Assert.That(w.People.Sum(p => p.TotalGathered), Is.GreaterThan(0), "Niemand hat gesammelt");
            var moved = w.Households.Where(h => h.HomeBuildingId != 0).ToList();
            Assert.That(moved, Is.Not.Empty, "Kein Haushalt hat ein Haus bezogen");
            var house = w.BuildingById(moved[0].HomeBuildingId);
            Assert.IsTrue(house.Complete);
            Assert.AreEqual(DefaultContent.House, house.DefId);
            Assert.That(w.Log.Any(l => l.Text.Contains("bezieht sein Haus")));
        }

        [Test]
        public void Player_BuildsStorage_WhichGetsCompletedAndUsed()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            int cell = TestUtil.FindPlayerBuildCell(sim);
            sim.Submit(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, cell));
            sim.Step();
            Assert.IsTrue(w.CommandHistory[0].Succeeded, w.CommandHistory[0].Result);
            var storage = w.Buildings.Single(b => b.DefId == DefaultContent.Storage);
            sim.Run(Calendar.TicksPerDay * 30);
            Assert.IsTrue(storage.Complete, "Lager nach 30 Tagen nicht fertig. Arbeit übrig: " + storage.LaborRemaining);
            CollectionAssert.AreEqual(new[] { 0, 15, 5 }, storage.Delivered);
        }

        [Test]
        public void InvalidCommands_AreRejectedWithReason()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            int water = Enumerable.Range(0, w.Map.CellCount).First(i => w.Map.Biomes[i] == Biome.Wasser);
            StringAssert.Contains("Wiese", sim.Preview(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, water)));
            int center = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId).CenterCell;
            StringAssert.Contains("Gebäude", sim.Preview(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, center)));
            StringAssert.Contains("nicht vom Spieler", sim.Preview(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.House, TestUtil.FindPlayerBuildCell(sim))));
            sim.Submit(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, water));
            sim.Step();
            Assert.IsFalse(w.CommandHistory[0].Succeeded);
            Assert.That(w.Buildings.Count, Is.EqualTo(3));
        }

        [Test]
        public void Road_CostsStone_AndIsFasterToWalk()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            var s = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId);
            int before = Economy.SettlementAvailable(w, sim.Content, s.Id, ResourceType.Stein);
            int x = w.Map.X(s.CenterCell), y = w.Map.Y(s.CenterCell);
            int to = -1;
            for (int dx = 5; dx >= 2 && to < 0; dx--)
                foreach (int sx in new[] { dx, -dx })
                    if (w.Map.InBounds(x + sx, y) && sim.Preview(GameCommand.PlaceRoad(w.PlayerCivId, s.CenterCell, w.Map.Index(x + sx, y))) == null)
                    { to = w.Map.Index(x + sx, y); break; }
            Assert.GreaterOrEqual(to, 0, "Keine gültige Teststraße");
            int oldCost = w.Map.MoveCost(to);
            sim.Submit(GameCommand.PlaceRoad(w.PlayerCivId, s.CenterCell, to));
            sim.Step();
            Assert.IsTrue(w.CommandHistory[0].Succeeded, w.CommandHistory[0].Result);
            var cells = BuildRules.RoadCells(w.Map, s.CenterCell, to);
            int planned = cells.Count(c => w.Map.RoadWork[c] > 0);
            Assert.Greater(planned, 0);
            Assert.AreEqual(before - planned, Economy.SettlementAvailable(w, sim.Content, s.Id, ResourceType.Stein) + Reserved(sim, s.Id), "Stein wird bei Auftrag bezahlt");
            Assert.AreEqual(oldCost, w.Map.MoveCost(to), "Straße entsteht nicht sofort");
            sim.Run(24 * 10);
            Assert.That(cells.All(c => w.Map.RoadWork[c] == 0), "Nach 10 Tagen sind alle Felder gebaut");
            Assert.AreEqual(1, w.Map.MoveCost(to));
            Assert.That(w.Log.Any(l => l.Text.Contains("Straßenfelder sind fertig")));
        }

        static int Reserved(WorldSimulation sim, int sid) =>
            Economy.Storages(sim.World, sim.Content, sid).Sum(b => b.Reserved[(int)ResourceType.Stein]);

        [Test]
        public void ObserverMode_BlocksBuildCommands_AndCivCanBeChosenLater()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            int civ = w.PlayerCivId, cell = TestUtil.FindPlayerBuildCell(sim);
            sim.Submit(GameCommand.ChoosePlayerCiv(0)); sim.Step();
            Assert.AreEqual(0, w.PlayerCivId);
            StringAssert.Contains("gespielte", sim.Preview(GameCommand.PlaceBuilding(civ, DefaultContent.Storage, cell)));
            sim.Submit(GameCommand.ChoosePlayerCiv(civ)); sim.Step();
            Assert.IsNull(sim.Preview(GameCommand.PlaceBuilding(civ, DefaultContent.Storage, cell)));
            var b = TestUtil.Reload(sim);
            Assert.AreEqual(civ, b.World.PlayerCivId);
        }

        [Test]
        public void TooExpensiveRoad_IsRejected()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            var s = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId);
            foreach (var b in w.Buildings) b.Stock[(int)ResourceType.Stein] = 0;
            int x = w.Map.X(s.CenterCell), y = w.Map.Y(s.CenterCell);
            string reason = sim.Preview(GameCommand.PlaceRoad(w.PlayerCivId, s.CenterCell, w.Map.Index(x, y + 1)));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void LongRun_NoNegativeStocks_NoBrokenReservations()
        {
            var sim = TestUtil.NewSim(777);
            sim.Submit(GameCommand.PlaceBuilding(sim.World.PlayerCivId, DefaultContent.Storage, TestUtil.FindPlayerBuildCell(sim)));
            for (int k = 0; k < 40; k++) { sim.Run(Calendar.TicksPerDay * 3); TestUtil.AssertValid(sim.World); }
        }

        [Test]
        public void Calendar_FormatsSeasons()
        {
            Assert.AreEqual("Jahr 1, Monat 1, Tag 1, 00:00 (Frühling)", Calendar.Format(0));
            Assert.AreEqual(Season.Winter, Calendar.SeasonOf(Calendar.TicksPerDay * 300));
            Assert.AreEqual(2, Calendar.Year(Calendar.TicksPerDay * Calendar.DaysPerYear));
        }

        [Test]
        public void Content_IsValid_AndOnlyFirstEpochEnabled()
        {
            var c = DefaultContent.Create();
            Assert.That(c.Validate(), Is.Empty);
            Assert.AreEqual(9, c.Epochs.Count);
            Assert.AreEqual(1, c.Epochs.Count(e => e.Enabled));
        }
    }
}
