using System.Linq;
using NUnit.Framework;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Persistence;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Tests
{
    public static class TestUtil
    {
        public const ulong Seed = 12345;
        public static WorldSimulation NewSim(ulong seed = Seed)
        {
            var content = DefaultContent.Create();
            return new WorldSimulation(ScenarioFactory.CreateDefault(seed, content), content);
        }

        public static WorldSimulation Reload(WorldSimulation sim)
        {
            var bytes = SaveCodec.Encode(sim.World, "test", new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
            return new WorldSimulation(SaveCodec.Decode(bytes, out _), sim.Content);
        }

        public static void AssertValid(WorldState w)
        {
            var errors = WorldValidator.Validate(w);
            Assert.That(errors, Is.Empty, "Tick " + w.Tick + ": " + string.Join("\n", errors.Take(10)));
        }

        /// <summary>Sucht eine gültige Bauzelle für ein Spielergebäude nahe der Spieler-Hauptstadt.</summary>
        public static int FindPlayerBuildCell(WorldSimulation sim, int minDist = 3)
        {
            var w = sim.World;
            var s = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId);
            for (int d = minDist; d <= BuildRules.BuildRadius; d++)
                for (int i = 0; i < w.Map.CellCount; i++)
                    if (w.Map.Manhattan(i, s.CenterCell) == d && BuildRules.CheckBuildingCell(w, w.PlayerCivId, i) == null) return i;
            Assert.Fail("Keine Bauzelle gefunden");
            return -1;
        }
    }
}
