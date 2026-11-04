using System.Collections.Generic;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Zufriedenheit 0..100 je Person (Save v4). Täglich wird ein Zielwert aus nachvollziehbaren Gründen berechnet;
    /// der gespeicherte Wert bewegt sich höchstens <see cref="DailyStep"/> Punkte pro Tag darauf zu.
    /// Wirkung: Geburten nur bei Siedlungsdurchschnitt ≥ <see cref="BirthMinimum"/>; unter <see cref="LowThreshold"/>
    /// legt eine Person nach jeder Aufgabe <see cref="DiscontentPauseHours"/> Stunden Pause ein.
    /// </summary>
    public static class Satisfaction
    {
        public const int Base = 50;
        public const int DailyStep = 5;
        public const int LowThreshold = 25;
        public const int BirthMinimum = 50;
        public const int DiscontentPauseHours = 2;
        public const int RoadRadius = 2;

        /// <summary>Zielwert mit Gründen (für den Inspektor). <paramref name="reasons"/> darf null sein.</summary>
        public static int Target(WorldState w, ContentCatalog c, Person p, List<string> reasons)
        {
            int t = Base;
            if (p.Hunger == 0) Add(ref t, 15, "satt", reasons);
            else if (p.Hunger >= 50) Add(ref t, -25, "hungert", reasons);
            else Add(ref t, -10, "schlecht versorgt", reasons);

            var h = w.HouseholdById(p.HouseholdId);
            var home = h != null && h.HomeBuildingId != 0 ? w.BuildingById(h.HomeBuildingId) : null;
            if (home == null) Add(ref t, -15, "ohne Haus", reasons);
            else
            {
                Add(ref t, 15, "eigenes Haus", reasons);
                if (h.MemberIds.Count >= c.Building(home.DefId).HousingCapacity) Add(ref t, -5, "beengt", reasons);
                if (NearRoad(w.Map, home.Cell)) Add(ref t, 5, "Straßenanschluss", reasons);
            }
            if (p.Health < 50) Add(ref t, -10, "krank", reasons);
            return System.Math.Max(0, System.Math.Min(100, t));
        }

        static void Add(ref int t, int delta, string why, List<string> reasons)
        {
            t += delta;
            reasons?.Add((delta > 0 ? "+" : "") + delta + " " + why);
        }

        public static bool NearRoad(WorldMap m, int cell)
        {
            int cx = m.X(cell), cy = m.Y(cell);
            for (int y = cy - RoadRadius; y <= cy + RoadRadius; y++)
                for (int x = cx - RoadRadius; x <= cx + RoadRadius; x++)
                    if (m.InBounds(x, y) && System.Math.Abs(x - cx) + System.Math.Abs(y - cy) <= RoadRadius && m.Road[m.Index(x, y)]) return true;
            return false;
        }

        /// <summary>Täglich nach der Versorgung.</summary>
        public static void Run(WorldSimulation sim)
        {
            var w = sim.World;
            foreach (var p in w.People)
            {
                int target = Target(w, sim.Content, p, null);
                int before = p.Satisfaction;
                if (p.Satisfaction < target) p.Satisfaction = System.Math.Min(target, p.Satisfaction + DailyStep);
                else if (p.Satisfaction > target) p.Satisfaction = System.Math.Max(target, p.Satisfaction - DailyStep);
                if (before >= LowThreshold && p.Satisfaction < LowThreshold)
                    w.AddLog("Stimmung", $"{p.Name} ist unzufrieden und arbeitet langsamer.", p.Id);
            }
        }

        /// <summary>Durchschnitt der Siedlung (ganzzahlig); 0 ohne Einwohner.</summary>
        public static int Average(WorldState w, int settlementId)
        {
            int sum = 0, n = 0;
            foreach (var p in w.People) if (p.SettlementId == settlementId) { sum += p.Satisfaction; n++; }
            return n == 0 ? 0 : sum / n;
        }

        /// <summary>Monatlicher Verlaufspunkt (Tag 0 jedes Monats), höchstens <see cref="HistoryPoint.MaxPoints"/>.</summary>
        public static void RecordHistory(WorldSimulation sim)
        {
            var w = sim.World;
            if (Calendar.Day(w.Tick) % Calendar.DaysPerMonth != 0) return;
            foreach (var s in w.Settlements)
            {
                int pop = 0;
                foreach (var p in w.People) if (p.SettlementId == s.Id) pop++;
                s.History.Add(new HistoryPoint
                {
                    Tick = w.Tick, Population = pop,
                    Food = Economy.SettlementAvailable(w, sim.Content, s.Id, ResourceType.Nahrung),
                    Wood = Economy.SettlementAvailable(w, sim.Content, s.Id, ResourceType.Holz),
                    Stone = Economy.SettlementAvailable(w, sim.Content, s.Id, ResourceType.Stein),
                    Satisfaction = Average(w, s.Id)
                });
                while (s.History.Count > HistoryPoint.MaxPoints) s.History.RemoveAt(0);
            }
        }
    }
}
