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
            Nature(sim.World);
            Households(sim);
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
                int cell = FindHouseCell(w, s);
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

        /// <summary>Nächster gültiger Bauplatz um das Zentrum: Abstand 2..Radius, gleicher Abstand nach Zellindex.</summary>
        static int FindHouseCell(WorldState w, Settlement s)
        {
            var m = w.Map;
            int cx = m.X(s.CenterCell), cy = m.Y(s.CenterCell);
            for (int d = 2; d <= HouseSearchRadius; d++)
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
