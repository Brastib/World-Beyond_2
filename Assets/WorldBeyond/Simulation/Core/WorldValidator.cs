using System.Collections.Generic;

namespace WorldBeyond.Simulation.Core
{
    /// <summary>Invarianten-Prüfung: genutzt nach dem Laden und in Szenario-/Property-Tests.</summary>
    public static class WorldValidator
    {
        public static List<string> Validate(WorldState w)
        {
            var e = new List<string>();
            var ids = new HashSet<int>();
            void Id(IEntity x, string kind)
            {
                if (x.Id <= 0 || x.Id >= w.NextId) e.Add($"{kind} {x.Id}: Id außerhalb 1..{w.NextId - 1}");
                if (!ids.Add(x.Id)) e.Add($"{kind} {x.Id}: doppelte Id");
            }
            void Sorted<T>(List<T> l, string kind) where T : IEntity
            {
                for (int i = 1; i < l.Count; i++) if (l[i - 1].Id >= l[i].Id) e.Add(kind + "-Liste nicht nach Id sortiert");
            }
            Sorted(w.Civilizations, "Zivilisation"); Sorted(w.Settlements, "Siedlung"); Sorted(w.Households, "Haushalt");
            Sorted(w.People, "Person"); Sorted(w.Buildings, "Gebäude");

            foreach (var c in w.Civilizations)
            {
                Id(c, "Zivilisation");
                if (w.SettlementById(c.CapitalSettlementId) == null) e.Add($"Zivilisation {c.Id}: Hauptstadt fehlt");
            }
            foreach (var s in w.Settlements)
            {
                Id(s, "Siedlung");
                if (w.Civ(s.CivId) == null) e.Add($"Siedlung {s.Id}: Zivilisation fehlt");
                if (!w.Map.InBounds(s.CenterCell)) e.Add($"Siedlung {s.Id}: Zentrum außerhalb");
            }
            foreach (var h in w.Households)
            {
                Id(h, "Haushalt");
                if (w.SettlementById(h.SettlementId) == null) e.Add($"Haushalt {h.Id}: Siedlung fehlt");
                if (h.HomeBuildingId != 0 && w.BuildingById(h.HomeBuildingId) == null) e.Add($"Haushalt {h.Id}: Haus fehlt");
                if (h.PendingHouseSiteId != 0 && w.BuildingById(h.PendingHouseSiteId) == null) e.Add($"Haushalt {h.Id}: Baustelle fehlt");
                foreach (var m in h.MemberIds)
                {
                    var p = w.PersonById(m);
                    if (p == null) e.Add($"Haushalt {h.Id}: Mitglied {m} fehlt");
                    else if (p.HouseholdId != h.Id) e.Add($"Haushalt {h.Id}: Mitglied {m} zeigt auf anderen Haushalt");
                }
            }
            if (w.PlayerCivId != 0 && w.Civ(w.PlayerCivId) == null) e.Add("Spieler-Zivilisation fehlt");
            foreach (var p in w.People)
            {
                Id(p, "Person");
                var h = w.HouseholdById(p.HouseholdId);
                if (h == null || !h.MemberIds.Contains(p.Id)) e.Add($"Person {p.Id}: Haushaltszuordnung kaputt");
                if (!w.Map.InBounds(p.Cell)) e.Add($"Person {p.Id}: Ort außerhalb der Karte");
                else if (!w.Map.IsWalkable(p.Cell)) e.Add($"Person {p.Id}: steht auf unbegehbarer Zelle");
                if (p.CarryAmount < 0) e.Add($"Person {p.Id}: negative Traglast");
                if (p.ToolUses < 0) e.Add($"Person {p.Id}: negative Werkzeugeinsätze");
                if (p.Hunger < 0 || p.Hunger > 100 || p.Health < 0 || p.Health > 100 || p.Satisfaction < 0 || p.Satisfaction > 100) e.Add($"Person {p.Id}: Werte außerhalb 0..100");
                if (p.PathPos < 0 || p.PathPos > p.Path.Count) e.Add($"Person {p.Id}: Pfadposition ungültig");
                if (p.TargetBuildingId != 0 && w.BuildingById(p.TargetBuildingId) == null) e.Add($"Person {p.Id}: Zielgebäude fehlt");
            }
            for (int c = 0; c < w.Map.CellCount; c++)
                if (w.Map.RoadWork[c] > 0 && (w.Map.Road[c] || !w.Map.IsWalkable(c))) { e.Add($"Zelle {c}: Straßenplanung auf Straße/unbegehbar"); break; }
            foreach (var b in w.Buildings)
            {
                Id(b, "Gebäude");
                if (b.FieldGrowth < 0) e.Add($"Gebäude {b.Id}: negatives Feldwachstum");
                if (b.CraftProgress < 0) e.Add($"Gebäude {b.Id}: negativer Werkstückfortschritt");
                if (w.SettlementById(b.SettlementId) == null) e.Add($"Gebäude {b.Id}: Siedlung fehlt");
                if (!w.Map.InBounds(b.Cell)) e.Add($"Gebäude {b.Id}: außerhalb");
                else if (w.Map.BuildingAt[b.Cell] != b.Id) e.Add($"Gebäude {b.Id}: Kartenbelegung inkonsistent");
                for (int r = 0; r < Res.Count; r++)
                {
                    if (b.Stock[r] < 0 || b.Reserved[r] < 0 || b.Delivered[r] < 0 || b.Incoming[r] < 0)
                        e.Add($"Gebäude {b.Id}: negative Menge ({(ResourceType)r})");
                    if (b.Reserved[r] > b.Stock[r]) e.Add($"Gebäude {b.Id}: Reservierung > Bestand ({(ResourceType)r})");
                }
            }
            // Reservierungen müssen genau den laufenden Abholungen entsprechen.
            var expectedReserved = new Dictionary<int, int[]>();
            var expectedIncoming = new Dictionary<int, int[]>();
            foreach (var p in w.People)
            {
                if (p.Task == PersonTask.MaterialHolen)
                {
                    if (!expectedReserved.TryGetValue(p.TargetBuildingId, out var a)) expectedReserved[p.TargetBuildingId] = a = Res.Empty();
                    a[(int)p.TaskResource] += p.HaulAmount;
                }
                if (p.Task == PersonTask.MaterialHolen || p.Task == PersonTask.MaterialBringen)
                {
                    int site = p.Task == PersonTask.MaterialBringen ? p.TargetBuildingId : p.HaulSiteId;
                    if (!expectedIncoming.TryGetValue(site, out var a)) expectedIncoming[site] = a = Res.Empty();
                    a[(int)p.TaskResource] += p.Task == PersonTask.MaterialBringen ? p.CarryAmount : p.HaulAmount;
                }
            }
            foreach (var b in w.Buildings)
            {
                expectedReserved.TryGetValue(b.Id, out var er); er = er ?? Res.Empty();
                expectedIncoming.TryGetValue(b.Id, out var ei); ei = ei ?? Res.Empty();
                for (int r = 0; r < Res.Count; r++)
                {
                    if (b.Reserved[r] != er[r]) e.Add($"Gebäude {b.Id}: Reservierung {b.Reserved[r]} ≠ laufende Abholungen {er[r]} ({(ResourceType)r})");
                    if (b.Incoming[r] != ei[r]) e.Add($"Gebäude {b.Id}: Zulauf {b.Incoming[r]} ≠ Träger unterwegs {ei[r]} ({(ResourceType)r})");
                }
            }
            return e;
        }
    }
}
