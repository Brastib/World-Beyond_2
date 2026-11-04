using System.Collections.Generic;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Tägliche Berufszuteilung je Siedlung (Id-Reihenfolge, ganzzahlig):
    /// Baumeister für offene Baustellen (max. 2), ein Träger bei Baustellen, Rest nach Bedarf auf Sammler/Holzfäller/Bergarbeiter.
    /// </summary>
    public static class Jobs
    {
        public static void Assign(WorldState w, ContentCatalog content)
        {
            var adults = new List<Person>();
            foreach (var s in w.Settlements)
            {
                adults.Clear();
                foreach (var p in w.People)
                {
                    if (p.SettlementId != s.Id) continue;
                    if (!p.IsAdult) { p.Role = Role.Kind; continue; }
                    adults.Add(p);
                }
                int sites = 0, farmSlots = 0;
                bool growing = PersonSystem.IsGrowingSeason(w);
                foreach (var b in w.Buildings)
                {
                    if (b.SettlementId != s.Id) continue;
                    if (!b.Complete) { sites++; continue; }
                    var def = content.Building(b.DefId);
                    if (def.FieldCapacity > 0 && (growing || b.FieldGrowth >= 2)) farmSlots += def.Workers;
                }
                int roadJobs = PersonSystem.OpenRoadWork(w.Map, s) > 0 ? 1 : 0;
                int builders = System.Math.Min(System.Math.Min(2, sites + roadJobs), adults.Count);
                int carriers = sites > 0 && adults.Count - builders > 2 ? 1 : 0;
                int farmers = System.Math.Min(farmSlots, (adults.Count - builders - carriers) / 2);
                var stock = Economy.SettlementStock(w, content, s.Id);
                var assigned = new int[Res.Count];
                var targets = PersonSystem.Targets(w, s.Id);
                for (int k = 0; k < adults.Count; k++)
                {
                    var p = adults[k];
                    if (k < builders) { p.Role = Role.Baumeister; continue; }
                    if (k < builders + carriers) { p.Role = Role.Traeger; continue; }
                    if (k < builders + carriers + farmers) { p.Role = Role.Bauer; continue; }
                    // Ressource mit dem kleinsten (Bestand + 40 je bereits Zugeteiltem) / Ziel.
                    int best = 0;
                    for (int r = 1; r < Res.Count; r++)
                    {
                        long a = (long)(stock[r] + 40 * assigned[r]) * targets[best];
                        long bb = (long)(stock[best] + 40 * assigned[best]) * targets[r];
                        if (a < bb) best = r;
                    }
                    assigned[best]++;
                    p.Role = best == 0 ? Role.Sammler : best == 1 ? Role.Holzfaeller : Role.Bergarbeiter;
                }
            }
        }

        public static ResourceType? RoleResource(Role r)
        {
            switch (r)
            {
                case Role.Sammler: return ResourceType.Nahrung;
                case Role.Holzfaeller: return ResourceType.Holz;
                case Role.Bergarbeiter: return ResourceType.Stein;
                default: return null;
            }
        }

        public static string RoleName(Role r)
        {
            switch (r)
            {
                case Role.Holzfaeller: return "Holzfäller";
                case Role.Traeger: return "Träger";
                default: return r.ToString();
            }
        }
    }
}
