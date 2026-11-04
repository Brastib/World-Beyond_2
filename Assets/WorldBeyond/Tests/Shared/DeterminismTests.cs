using NUnit.Framework;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Persistence;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Tests
{
    public class DeterminismTests
    {
        [Test]
        public void SameSeedSameTicks_SameStateHash()
        {
            var a = TestUtil.NewSim(); var b = TestUtil.NewSim();
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World), "Startzustand");
            a.Run(2000); b.Run(2000);
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void DifferentSeed_DifferentMapAndState()
        {
            var a = TestUtil.NewSim(1); var b = TestUtil.NewSim(2);
            Assert.AreNotEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void KnownSeed_MapFingerprintIsStable()
        {
            // Fängt unbeabsichtigte Generator-Änderungen ab (Plattform/Version). Bei gewollter Änderung: Wert + ADR anpassen.
            var m = WorldGenerator.Generate(TestUtil.Seed);
            long sum = 0;
            for (int i = 0; i < m.CellCount; i++) sum = sum * 31 + m.Elevation[i] * 7 + (int)m.Biomes[i];
            Assert.AreEqual(ExpectedFingerprint, sum);
        }
        public const long ExpectedFingerprint = 5480965671066214387; // Seed 12345, 128x128, Generator m0-1

        [Test]
        public void CommandReplay_ReproducesState()
        {
            var a = TestUtil.NewSim();
            a.Run(10);
            int cell = TestUtil.FindPlayerBuildCell(a);
            a.Submit(GameCommand.PlaceBuilding(a.World.PlayerCivId, DefaultContent.Storage, cell));
            a.Run(400);
            var s = a.World.SettlementById(a.World.Civ(a.World.PlayerCivId).CapitalSettlementId);
            a.Submit(GameCommand.PlaceRoad(a.World.PlayerCivId, s.CenterCell, a.World.Map.Index(a.World.Map.X(s.CenterCell) + 4, a.World.Map.Y(s.CenterCell))));
            a.Run(1000);

            var b = TestUtil.NewSim();
            WorldSimulation.Replay(b, a.World.CommandHistory, a.World.Tick);
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void ManySeeds_ProduceValidStartAndStayValid()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = TestUtil.NewSim(seed);
                Assert.AreEqual(3, sim.World.Settlements.Count);
                TestUtil.AssertValid(sim.World);
                for (int k = 0; k < 6; k++) { sim.Run(100); TestUtil.AssertValid(sim.World); }
            }
        }
    }
}
