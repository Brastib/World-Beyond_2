using System;
using System.Collections.Generic;

namespace WorldBeyond.Simulation.Core
{
    /// <summary>
    /// Gesamter autoritativer Simulationszustand. Kein Singleton: jede Simulation besitzt genau eine Instanz.
    /// Entitätslisten sind nach Id aufsteigend sortiert (Ids werden monoton vergeben), dadurch ist
    /// jede Iteration deterministisch.
    /// </summary>
    public sealed class WorldState
    {
        public const int MaxLogEntries = 300;

        public ulong Seed;
        public long Tick;
        public int NextId = 1;
        public string ContentVersion = "";
        public int PlayerCivId;
        public WorldMap Map;
        public DeterministicRandom Rng;

        public readonly List<Civilization> Civilizations = new List<Civilization>();
        public readonly List<Settlement> Settlements = new List<Settlement>();
        public readonly List<Household> Households = new List<Household>();
        public readonly List<Person> People = new List<Person>();
        public readonly List<Building> Buildings = new List<Building>();
        public readonly List<GameCommand> PendingCommands = new List<GameCommand>();
        public readonly List<GameCommand> CommandHistory = new List<GameCommand>();
        public readonly List<LogEntry> Log = new List<LogEntry>();

        public WorldState(ulong seed, WorldMap map)
        {
            Seed = seed;
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Rng = new DeterministicRandom(seed ^ 0xA5A5_5A5A_C3C3_3C3CUL);
        }

        public int NewId() => NextId++;

        public static T Find<T>(List<T> list, int id) where T : class, IEntity
        {
            int lo = 0, hi = list.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int mId = list[mid].Id;
                if (mId == id) return list[mid];
                if (mId < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public Civilization Civ(int id) => Find(Civilizations, id);
        public Settlement SettlementById(int id) => Find(Settlements, id);
        public Household HouseholdById(int id) => Find(Households, id);
        public Person PersonById(int id) => Find(People, id);
        public Building BuildingById(int id) => Find(Buildings, id);

        public void AddLog(string category, string text, int entityId = 0)
        {
            Log.Add(new LogEntry { Tick = Tick, Category = category, Text = text, EntityId = entityId });
            if (Log.Count > MaxLogEntries) Log.RemoveAt(0);
        }

        /// <summary>Leitet abgeleitete Daten (Gebäudebelegung der Karte) neu ab, z. B. nach dem Laden.</summary>
        public void RebuildDerived()
        {
            Array.Clear(Map.BuildingAt, 0, Map.BuildingAt.Length);
            foreach (var b in Buildings) Map.BuildingAt[b.Cell] = b.Id;
        }
    }
}
