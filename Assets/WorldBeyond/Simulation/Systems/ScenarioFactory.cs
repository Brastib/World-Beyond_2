using System;
using System.Collections.Generic;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>Standard-Startszenario: gemäßigte Seed-Karte, drei Zivilisationen mit je einer Siedlung.</summary>
    public static class ScenarioFactory
    {
        public const int HouseholdsPerSettlement = 2;
        public const int PeoplePerHousehold = 3;
        static readonly int[] StartStock = { 60, 30, 10 };
        static readonly string[] CivNames = { "Aurer", "Belen", "Corvik" };
        static readonly string[] TownNames = { "Aurheim", "Belhaven", "Corvstedt" };
        static readonly uint[] CivColors = { 0xD9483Bu, 0x3B6FD9u, 0xE0B532u };
        static readonly string[] Syl1 = { "Al", "Bo", "Ca", "Da", "El", "Fe", "Gu", "Ha", "Il", "Jo", "Ka", "Lu", "Ma", "No", "Or", "Pi", "Ra", "Su", "Ta", "Ve" };
        static readonly string[] Syl2 = { "ra", "mo", "lin", "dra", "nor", "ke", "sa", "tin", "ve", "lo", "ric", "na" };

        public static WorldState CreateDefault(ulong seed, ContentCatalog content, int size = WorldGenerator.DefaultSize)
        {
            var errors = content.Validate();
            if (errors.Count > 0) throw new InvalidOperationException("Content ungültig: " + string.Join("; ", errors));
            var map = WorldGenerator.Generate(seed, size, size);
            var w = new WorldState(seed, map) { ContentVersion = content.Version };
            var sites = PickSettlementSites(w);
            for (int c = 0; c < 3; c++)
            {
                var civ = new Civilization { Id = w.NewId(), Name = CivNames[c], ColorRgb = CivColors[c] };
                w.Civilizations.Add(civ);
                var s = new Settlement { Id = w.NewId(), CivId = civ.Id, Name = TownNames[c], CenterCell = sites[c] };
                w.Settlements.Add(s);
                civ.CapitalSettlementId = s.Id;
                var tc = new Building
                {
                    Id = w.NewId(), DefId = DefaultContent.TownCenter, CivId = civ.Id, SettlementId = s.Id,
                    Cell = sites[c], Complete = true
                };
                Array.Copy(StartStock, tc.Stock, Res.Count);
                w.Buildings.Add(tc);
                // Startfläche räumen, damit das Ortszentrum nicht auf einer Ressource steht.
                map.Resource[tc.Cell] = NaturalResource.Keine; map.ResourceAmount[tc.Cell] = 0;
                for (int h = 0; h < HouseholdsPerSettlement; h++)
                {
                    var hh = new Household { Id = w.NewId(), SettlementId = s.Id };
                    w.Households.Add(hh);
                    string family = null;
                    for (int p = 0; p < PeoplePerHousehold; p++)
                    {
                        var person = new Person
                        {
                            Id = w.NewId(), HouseholdId = hh.Id, SettlementId = s.Id, CivId = civ.Id,
                            Name = RandomName(w.Rng), AgeDays = w.Rng.Range(18, 40) * Calendar.DaysPerYear + w.Rng.NextInt(Calendar.DaysPerYear),
                            Cell = sites[c]
                        };
                        if (family == null) family = person.Name;
                        w.People.Add(person);
                        hh.MemberIds.Add(person.Id);
                    }
                    hh.Name = "Haushalt " + family;
                }
            }
            w.PlayerCivId = w.Civilizations[0].Id;
            w.RebuildDerived();
            w.AddLog("Welt", $"Neue Welt mit Seed {seed} erzeugt: {w.Settlements.Count} Siedlungen, {w.People.Count} Personen.");
            return w;
        }

        static string RandomName(DeterministicRandom rng) => Syl1[rng.NextInt(Syl1.Length)] + Syl2[rng.NextInt(Syl2.Length)];

        static List<int> PickSettlementSites(WorldState w)
        {
            var m = w.Map;
            var candidates = new List<int>();
            for (int y = 8; y < m.Height - 8; y++)
                for (int x = 8; x < m.Width - 8; x++)
                {
                    int i = m.Index(x, y);
                    if (m.Biomes[i] != Biome.Wiese) continue;
                    if (Count(m, i, 8, NaturalResource.Baeume) >= 8 && Count(m, i, 12, NaturalResource.Felsen) >= 4 &&
                        Count(m, i, 8, NaturalResource.Beeren) >= 3)
                        candidates.Add(i);
                }
            // Dokumentierte Lockerung (ADR-007): erst großer Mindestabstand, dann kleiner. Keine stille Ersatzkarte.
            foreach (int minDist in new[] { 40, 28, 16 })
            {
                var rng = new DeterministicRandom(w.Seed ^ 0x51u ^ (ulong)minDist);
                for (int attempt = 0; attempt < 200 && candidates.Count >= 3; attempt++)
                {
                    var chosen = new List<int>();
                    for (int k = 0; k < 60 && chosen.Count < 3; k++)
                    {
                        int c = candidates[rng.NextInt(candidates.Count)];
                        bool ok = true;
                        foreach (var o in chosen) if (m.Manhattan(o, c) < minDist) { ok = false; break; }
                        if (ok) chosen.Add(c);
                    }
                    if (chosen.Count == 3) return chosen;
                }
            }
            throw new InvalidOperationException($"Seed {w.Seed} erzeugt keine gültige Startkarte (zu wenige Siedlungsplätze: {candidates.Count}).");
        }

        static int Count(WorldMap m, int center, int r, NaturalResource res)
        {
            int cx = m.X(center), cy = m.Y(center), n = 0;
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if (m.InBounds(x, y) && Math.Abs(x - cx) + Math.Abs(y - cy) <= r && m.Resource[m.Index(x, y)] == res) n++;
            return n;
        }
    }
}
