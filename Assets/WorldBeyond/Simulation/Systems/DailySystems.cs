using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>Tageswirtschaft: Bedürfnisse, Natur (jahreszeitlich), Haushaltsentscheidungen. Läuft bei Stunde 0.</summary>
    public static class DailySystems
    {
        public const int HouseSearchRadius = 10;

        public static void Run(WorldSimulation sim)
        {
            Needs(sim);
            Satisfaction.Run(sim);
            Lifecycle.Run(sim);
            Nature(sim.World);
            Jobs.Assign(sim.World, sim.Content);
            Households(sim);
            SettlementPlanner.Run(sim);
            Satisfaction.RecordHistory(sim);
        }

        static void Needs(WorldSimulation sim)
        {
            var w = sim.World;
            foreach (var p in w.People)
            {
                p.AgeDays++;
                if (Economy.TryTake(w, sim.Content, p.SettlementId, ResourceType.Nahrung, 1))
                {
                    p.Hunger = 0;
                    p.Health = System.Math.Min(100, p.Health + 5);
                }
                else
                {
                    int before = p.Hunger;
                    p.Hunger = System.Math.Min(100, p.Hunger + 25);
                    if (p.Hunger >= 100) p.Health = System.Math.Max(0, p.Health - 10);
                    if (before < 50 && p.Hunger >= 50) w.AddLog("Versorgung", $"{p.Name} hungert: keine Nahrung im Lager.", p.Id);
                }
            }
        }

        static void Nature(WorldState w)
        {
            var s = Calendar.SeasonOf(w.Tick);
            // Frühling/Sommer: täglich +1, Herbst: jeden zweiten Tag, Winter: kein Nachwuchs.
            if (s == Season.Winter) return;
            if (s == Season.Herbst && Calendar.Day(w.Tick) % 2 == 1) return;
            var m = w.Map;
            for (int i = 0; i < m.CellCount; i++)
                if (m.Resource[i] == NaturalResource.Beeren && m.ResourceAmount[i] < WorldMap.BerryMax) m.ResourceAmount[i]++;
        }

        static void Households(WorldSimulation sim)
        {
            var w = sim.World;
            var def = sim.Content.Building(DefaultContent.House);
            foreach (var h in w.Households)
            {
                if (h.HomeBuildingId != 0 || h.PendingHouseSiteId != 0) continue;
                var vacant = VacantHouse(w, sim.Content, h.SettlementId);
                if (vacant != null)
                {
                    h.HomeBuildingId = vacant.Id; vacant.OwnerHouseholdId = h.Id;
                    w.AddLog("Haushalt", $"{h.Name} zieht in das leerstehende Haus #{vacant.Id}.", h.Id);
                    continue;
                }
                bool affordable = true;
                for (int r = 0; r < Res.Count && affordable; r++)
                {
                    if (def.Cost[r] == 0) continue;
                    int free = Economy.SettlementAvailable(w, sim.Content, h.SettlementId, (ResourceType)r)
                               - Economy.OpenSiteDemand(w, sim.Content, h.SettlementId, (ResourceType)r);
                    if (free < def.Cost[r]) affordable = false;
                }
                if (!affordable) continue;
                var s = w.SettlementById(h.SettlementId);
                int cell = FindFreeCell(w, s, 2, HouseSearchRadius);
                if (cell < 0)
                {
                    w.AddLog("Haushalt", $"{h.Name}: kein freier Bauplatz im Umkreis von {HouseSearchRadius} Feldern.", h.Id);
                    continue;
                }
                var site = Construction.PlaceSite(w, sim.Content, DefaultContent.House, s, cell, h.Id);
                h.PendingHouseSiteId = site.Id;
                w.AddLog("Haushalt", $"{h.Name} beauftragt Hausbau #{site.Id} ({def.Cost[(int)ResourceType.Holz]} Holz verfügbar).", h.Id);
            }
        }

        static Building VacantHouse(WorldState w, ContentCatalog content, int settlementId)
        {
            foreach (var b in w.Buildings)
            {
                if (b.SettlementId != settlementId || !b.Complete || content.Building(b.DefId).HousingCapacity == 0) continue;
                bool used = false;
                foreach (var h in w.Households) if (h.HomeBuildingId == b.Id) { used = true; break; }
                if (!used) return b;
            }
            return null;
        }

        /// <summary>Nächster gültiger Bauplatz um das Zentrum: Abstand min..max, gleicher Abstand nach Zellindex.</summary>
        public static int FindFreeCell(WorldState w, Settlement s, int minDist, int maxDist)
        {
            var m = w.Map;
            int cx = m.X(s.CenterCell), cy = m.Y(s.CenterCell);
            for (int d = minDist; d <= maxDist; d++)
                for (int y = cy - d; y <= cy + d; y++)
                    for (int x = cx - d; x <= cx + d; x++)
                    {
                        if (!m.InBounds(x, y) || System.Math.Abs(x - cx) + System.Math.Abs(y - cy) != d) continue;
                        int i = m.Index(x, y);
                        if (BuildRules.CheckBuildingCell(w, s.CivId, i) == null) return i;
                    }
            return -1;
        }
    }
}
