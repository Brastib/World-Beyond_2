using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Öffentliche Bauten der KI (nur Siedlungen, die der Spieler nicht steuert) und tägliche Wachstumsbegründung.
    /// Haushaltsbauten laufen getrennt in <see cref="DailySystems"/>.
    /// </summary>
    public static class SettlementPlanner
    {
        public const int FullPercent = 90;

        public static void Run(WorldSimulation sim)
        {
            var w = sim.World;
            foreach (var s in w.Settlements)
            {
                if (s.CivId != w.PlayerCivId) { PlanStorage(sim, s); PlanField(sim, s); }
                string status = Evaluate(sim, s);
                if (status != s.Status)
                {
                    s.Status = status;
                    w.AddLog("Siedlung", $"{s.Name}: {status}", s.Id);
                }
            }
        }

        static void PlanStorage(WorldSimulation sim, Settlement s)
        {
            var w = sim.World; var c = sim.Content;
            int cap = 0, used = 0;
            foreach (var b in Economy.Storages(w, c, s.Id)) { cap += c.Building(b.DefId).StorageCapacity; used += Res.Sum(b.Stock); }
            if (cap == 0 || used * 100 < cap * FullPercent) return;
            foreach (var b in w.Buildings) if (b.SettlementId == s.Id && !b.Complete && b.DefId == DefaultContent.Storage) return;
            var def = c.Building(DefaultContent.Storage);
            for (int r = 0; r < Res.Count; r++)
                if (def.Cost[r] > 0 && Economy.SettlementAvailable(w, c, s.Id, (ResourceType)r) - Economy.OpenSiteDemand(w, c, s.Id, (ResourceType)r) < def.Cost[r]) return;
            int cell = DailySystems.FindFreeCell(w, s, 3, BuildRules.BuildRadius);
            if (cell < 0) return;
            var site = Construction.PlaceSite(w, c, DefaultContent.Storage, s, cell, 0);
            w.AddLog("Bau", $"{s.Name} beauftragt ein neues Lager #{site.Id}, weil das Lager zu {used * 100 / cap}% voll ist.", site.Id);
        }

        /// <summary>KI legt Felder an: Ziel 1 Feld je 5 Einwohner (mindestens 1).</summary>
        static void PlanField(WorldSimulation sim, Settlement s)
        {
            var w = sim.World; var c = sim.Content;
            int pop = 0, fields = 0;
            foreach (var p in w.People) if (p.SettlementId == s.Id) pop++;
            foreach (var b in w.Buildings)
            {
                if (b.SettlementId != s.Id || b.DefId != DefaultContent.Field) continue;
                if (!b.Complete) return;
                fields++;
            }
            if (fields >= System.Math.Max(1, pop / 5)) return;
            var def = c.Building(DefaultContent.Field);
            for (int r = 0; r < Res.Count; r++)
                if (def.Cost[r] > 0 && Economy.SettlementAvailable(w, c, s.Id, (ResourceType)r) - Economy.OpenSiteDemand(w, c, s.Id, (ResourceType)r) < def.Cost[r]) return;
            int cell = DailySystems.FindFreeCell(w, s, 3, BuildRules.BuildRadius);
            if (cell < 0) return;
            var site = Construction.PlaceSite(w, c, DefaultContent.Field, s, cell, 0);
            w.AddLog("Bau", $"{s.Name} legt ein neues Feld #{site.Id} an ({fields} Felder für {pop} Einwohner).", site.Id);
        }

        /// <summary>Erste zutreffende Ursache, in Prioritätsreihenfolge. Text ist für Inspektor und Protokoll gedacht.</summary>
        public static string Evaluate(WorldSimulation sim, Settlement s)
        {
            var w = sim.World; var c = sim.Content;
            int pop = 0, homeless = 0, roomy = 0;
            foreach (var p in w.People) if (p.SettlementId == s.Id) pop++;
            int capacity = c.Building(DefaultContent.House).HousingCapacity;
            foreach (var h in w.Households)
            {
                if (h.SettlementId != s.Id) continue;
                if (h.HomeBuildingId == 0) homeless++;
                else if (h.MemberIds.Count < capacity) roomy++;
            }
            int food = Economy.SettlementAvailable(w, c, s.Id, ResourceType.Nahrung);
            int cap = 0, used = 0;
            foreach (var b in Economy.Storages(w, c, s.Id)) { cap += c.Building(b.DefId).StorageCapacity; used += Res.Sum(b.Stock); }
            if (pop == 0) return "Verlassen.";
            if (food < pop * Lifecycle.FoodReservePerPerson) return $"Stagniert: Nahrung knapp ({food} für {pop} Personen, nötig {pop * Lifecycle.FoodReservePerPerson}).";
            if (cap > 0 && used * 100 >= cap * 95) return $"Stagniert: Lager voll ({used}/{cap}).";
            if (homeless > 0 && Economy.SettlementAvailable(w, c, s.Id, ResourceType.Holz) < c.Building(DefaultContent.House).Cost[(int)ResourceType.Holz])
                return $"Wartet: {homeless} Haushalt(e) ohne Haus, Holz fehlt.";
            if (homeless > 0) return $"Baut: {homeless} Haushalt(e) ohne Haus.";
            if (roomy == 0) return "Wächst langsam: Häuser voll, Auszüge gründen neue Haushalte.";
            return "Wächst: genug Nahrung und Platz in Häusern.";
        }
    }
}
