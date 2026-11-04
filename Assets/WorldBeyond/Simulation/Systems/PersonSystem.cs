using System;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Personen-Verhalten pro Tick (Id-Reihenfolge). Priorität im Leerlauf:
    /// 1. Traglast abliefern, 2. Material zu Baustellen tragen, 3. an Baustelle arbeiten, 4. knappste Ressource sammeln.
    /// </summary>
    public static class PersonSystem
    {
        public const int CarryMax = 5;
        public const int GatherRadius = 24;
        public const int MaxBuildersPerSite = 3;
        public const int RetryCooldown = 24;
        public const int MovePointsPerTick = 2;
        /// <summary>Grundziele je Siedlung; die Ressource mit dem geringsten Bestand/Ziel-Verhältnis wird gesammelt.</summary>
        public static readonly int[] StockTargets = { 100, 80, 40 };
        /// <summary>Wintervorrat: Nahrungsziel wächst mit der Bevölkerung (90 Wintertage × 1 Nahrung, halb gedeckt durch Restbeeren).</summary>
        public const int FoodTargetPerPerson = 45;

        public static int[] Targets(WorldState w, int settlementId)
        {
            int pop = 0;
            foreach (var p in w.People) if (p.SettlementId == settlementId) pop++;
            return new[] { Math.Max(StockTargets[0], pop * FoodTargetPerPerson), StockTargets[1], StockTargets[2] };
        }

        public static void Tick(WorldSimulation sim)
        {
            var people = sim.World.People;
            for (int i = 0; i < people.Count; i++) Update(sim, people[i]);
        }

        static void Update(WorldSimulation sim, Person p)
        {
            if (p.Cooldown > 0) { p.Cooldown--; return; }
            if (p.Task == PersonTask.Leerlauf)
            {
                Decide(sim, p);
                if (p.Task == PersonTask.Leerlauf) return;
            }
            if (p.PathPos < p.Path.Count)
            {
                Move(sim.World.Map, p);
                if (p.PathPos < p.Path.Count) return;
            }
            Arrive(sim, p);
        }

        static void Move(WorldMap m, Person p)
        {
            p.MoveProgress += MovePointsPerTick;
            while (p.PathPos < p.Path.Count)
            {
                int next = p.Path[p.PathPos];
                int cost = m.MoveCost(next);
                if (p.MoveProgress < cost) break;
                p.MoveProgress -= cost;
                p.Cell = next;
                p.PathPos++;
            }
            if (p.PathPos >= p.Path.Count) { p.Path.Clear(); p.PathPos = 0; p.MoveProgress = 0; }
        }

        static bool GoTo(WorldSimulation sim, Person p, int cell)
        {
            p.TargetCell = cell; p.PathPos = 0; p.MoveProgress = 0;
            return sim.Paths.FindPath(p.Cell, cell, p.Path);
        }

        static void SetIdle(Person p, int cooldown)
        {
            p.Task = PersonTask.Leerlauf; p.TargetBuildingId = 0; p.Path.Clear(); p.PathPos = 0; p.MoveProgress = 0;
            p.HaulSiteId = 0; p.HaulAmount = 0; p.Cooldown = cooldown;
        }

        static void Decide(WorldSimulation sim, Person p)
        {
            if (p.CarryAmount > 0) { StartDeliver(sim, p); return; }
            switch (p.Role)
            {
                case Role.Kind: SetIdle(p, RetryCooldown); return;
                case Role.Baumeister:
                    if (TryBuild(sim, p) || TryRoad(sim, p) || TryHaul(sim, p)) return;
                    break;
                case Role.Bauer:
                    if (TryFarm(sim, p)) return;
                    break;
                case Role.Traeger:
                    if (TryHaul(sim, p) || TryBuild(sim, p)) return;
                    break;
                default:
                    // Rohstoffberufe sammeln ihren Rohstoff; ist das nicht möglich, helfen sie wie bisher überall aus.
                    var own = Jobs.RoleResource(p.Role);
                    if (own.HasValue && TryGather(sim, p, (int)own.Value)) return;
                    break;
            }
            if (TryHaul(sim, p)) return;
            if (TryBuild(sim, p)) return;
            if (TryRoad(sim, p)) return;
            if (TryFarm(sim, p)) return;
            if (TryGather(sim, p, -1)) return;
            SetIdle(p, RetryCooldown / 2);
        }

        static void StartDeliver(WorldSimulation sim, Person p)
        {
            var w = sim.World;
            Building best = null; int bestD = int.MaxValue;
            foreach (var b in Economy.Storages(w, sim.Content, p.SettlementId))
            {
                if (Economy.FreeCapacity(b, sim.Content) <= 0) continue;
                int d = w.Map.Manhattan(p.Cell, b.Cell);
                if (d < bestD) { bestD = d; best = b; }
            }
            if (best == null || !GoTo(sim, p, best.Cell)) { SetIdle(p, RetryCooldown); return; }
            p.Task = PersonTask.Abliefern; p.TargetBuildingId = best.Id;
        }

        static bool TryHaul(WorldSimulation sim, Person p)
        {
            var w = sim.World;
            foreach (var site in w.Buildings)
            {
                if (site.Complete || site.SettlementId != p.SettlementId) continue;
                var def = sim.Content.Building(site.DefId);
                for (int r = 0; r < Res.Count; r++)
                {
                    int need = def.Cost[r] - site.Delivered[r] - site.Incoming[r];
                    if (need <= 0) continue;
                    Building src = null; int bestD = int.MaxValue;
                    foreach (var b in Economy.Storages(w, sim.Content, p.SettlementId))
                    {
                        if (Economy.Available(b, (ResourceType)r) <= 0) continue;
                        int d = w.Map.Manhattan(p.Cell, b.Cell);
                        if (d < bestD) { bestD = d; src = b; }
                    }
                    if (src == null) continue;
                    if (!GoTo(sim, p, src.Cell)) continue;
                    int amount = Math.Min(CarryMax, Math.Min(need, Economy.Available(src, (ResourceType)r)));
                    src.Reserved[r] += amount;
                    site.Incoming[r] += amount;
                    p.Task = PersonTask.MaterialHolen; p.TargetBuildingId = src.Id; p.TaskResource = (ResourceType)r;
                    p.HaulSiteId = site.Id; p.HaulAmount = amount;
                    return true;
                }
            }
            return false;
        }

        static bool TryBuild(WorldSimulation sim, Person p)
        {
            var w = sim.World;
            foreach (var site in w.Buildings)
            {
                if (site.Complete || site.SettlementId != p.SettlementId || site.LaborRemaining <= 0) continue;
                if (!Construction.MaterialsComplete(site, sim.Content.Building(site.DefId))) continue;
                int builders = 0;
                foreach (var o in w.People) if (o.Task == PersonTask.Bauen && o.TargetBuildingId == site.Id) builders++;
                if (builders >= MaxBuildersPerSite) continue;
                if (!GoTo(sim, p, site.Cell)) continue;
                p.Task = PersonTask.Bauen; p.TargetBuildingId = site.Id;
                return true;
            }
            return false;
        }

        static bool TryRoad(WorldSimulation sim, Person p)
        {
            var w = sim.World; var m = w.Map;
            var s = w.SettlementById(p.SettlementId);
            int best = -1, bestD = int.MaxValue;
            int cx = m.X(s.CenterCell), cy = m.Y(s.CenterCell), r = BuildRules.RoadRadius;
            for (int y = Math.Max(0, cy - r); y <= Math.Min(m.Height - 1, cy + r); y++)
                for (int x = Math.Max(0, cx - r); x <= Math.Min(m.Width - 1, cx + r); x++)
                {
                    int i = m.Index(x, y);
                    if (m.RoadWork[i] == 0) continue;
                    int d = m.Manhattan(p.Cell, i);
                    if (d >= bestD) continue;
                    bool taken = false;
                    foreach (var o in w.People) if (o.Task == PersonTask.StrasseBauen && o.TargetCell == i) { taken = true; break; }
                    if (!taken) { bestD = d; best = i; }
                }
            if (best < 0 || !GoTo(sim, p, best)) return false;
            p.Task = PersonTask.StrasseBauen; p.TargetBuildingId = 0;
            return true;
        }

        /// <summary>Wachstum, das ein Feld zum aktuellen Zeitpunkt der Wachstumsperiode (Frühling+Sommer) höchstens haben darf.</summary>
        public static int AllowedGrowth(WorldState w, BuildingDef def)
        {
            var season = Calendar.SeasonOf(w.Tick);
            if (season != Season.Fruehling && season != Season.Sommer) return 0;
            int dayOfSeason = (int)(Calendar.Day(w.Tick) % Calendar.DaysPerYear); // Frühling beginnt bei Tag 0
            return def.FieldCapacity * (dayOfSeason + 1) / (Calendar.DaysPerYear / 2);
        }

        public static bool IsGrowingSeason(WorldState w)
        {
            var season = Calendar.SeasonOf(w.Tick);
            return season == Season.Fruehling || season == Season.Sommer;
        }

        static bool TryFarm(WorldSimulation sim, Person p)
        {
            var w = sim.World;
            bool growing = IsGrowingSeason(w);
            if (!growing)
            {
                int free = 0;
                foreach (var b in Economy.Storages(w, sim.Content, p.SettlementId)) free += Economy.FreeCapacity(b, sim.Content);
                if (free <= 0) return false;
            }
            Building best = null; int bestD = int.MaxValue;
            foreach (var f in w.Buildings)
            {
                if (!f.Complete || f.SettlementId != p.SettlementId) continue;
                var def = sim.Content.Building(f.DefId);
                if (def.FieldCapacity <= 0) continue;
                bool work = growing ? f.FieldGrowth < Math.Min(def.FieldCapacity, AllowedGrowth(w, def)) : f.FieldGrowth >= 2;
                if (!work) continue;
                int workers = 0;
                foreach (var o in w.People) if (o.Task == PersonTask.Feldarbeit && o.TargetBuildingId == f.Id) workers++;
                if (workers >= def.Workers) continue;
                int d = w.Map.Manhattan(p.Cell, f.Cell);
                if (d < bestD) { bestD = d; best = f; }
            }
            if (best == null || !GoTo(sim, p, best.Cell)) return false;
            p.Task = PersonTask.Feldarbeit; p.TargetBuildingId = best.Id;
            return true;
        }

        static bool TryGather(WorldSimulation sim, Person p, int only)
        {
            var w = sim.World;
            int free = 0;
            foreach (var b in Economy.Storages(w, sim.Content, p.SettlementId)) free += Economy.FreeCapacity(b, sim.Content);
            if (free <= 0) return false;
            var stock = Economy.SettlementStock(w, sim.Content, p.SettlementId);
            var targets = Targets(w, p.SettlementId);
            var order = new[] { 0, 1, 2 };
            // Stabile Sortierung nach Bestand/Ziel (Kreuzprodukt, keine Gleitkommazahlen).
            for (int i = 1; i < order.Length; i++)
                for (int j = i; j > 0 && (long)stock[order[j]] * targets[order[j - 1]] < (long)stock[order[j - 1]] * targets[order[j]]; j--)
                { int t = order[j]; order[j] = order[j - 1]; order[j - 1] = t; }
            foreach (int r in order)
            {
                if (only >= 0 && r != only) continue;
                // Lagerplatz nicht mit Überfluss blockieren: über dem doppelten Ziel wird diese Ressource nicht gesammelt.
                if (stock[r] >= targets[r] * 2) continue;
                int cell = NearestResource(w.Map, p.Cell, (ResourceType)r);
                if (cell < 0 || !GoTo(sim, p, cell)) continue;
                p.Task = PersonTask.Sammeln; p.TaskResource = (ResourceType)r; p.TargetBuildingId = 0;
                return true;
            }
            return false;
        }

        static int NearestResource(WorldMap m, int from, ResourceType r)
        {
            int fx = m.X(from), fy = m.Y(from), best = -1, bestD = int.MaxValue;
            for (int y = Math.Max(0, fy - GatherRadius); y <= Math.Min(m.Height - 1, fy + GatherRadius); y++)
                for (int x = Math.Max(0, fx - GatherRadius); x <= Math.Min(m.Width - 1, fx + GatherRadius); x++)
                {
                    int i = m.Index(x, y);
                    if (m.ResourceAmount[i] <= 0 || WorldMap.Yield(m.Resource[i]) != r || !m.IsWalkable(i)) continue;
                    int d = Math.Abs(x - fx) + Math.Abs(y - fy);
                    if (d < bestD) { bestD = d; best = i; }
                }
            return best;
        }

        public static int OpenRoadWork(WorldMap m, Settlement s)
        {
            int n = 0, cx = m.X(s.CenterCell), cy = m.Y(s.CenterCell), r = BuildRules.RoadRadius;
            for (int y = Math.Max(0, cy - r); y <= Math.Min(m.Height - 1, cy + r); y++)
                for (int x = Math.Max(0, cx - r); x <= Math.Min(m.Width - 1, cx + r); x++)
                    if (m.RoadWork[m.Index(x, y)] > 0) n++;
            return n;
        }

        static void Arrive(WorldSimulation sim, Person p)
        {
            var w = sim.World;
            var m = w.Map;
            switch (p.Task)
            {
                case PersonTask.Sammeln:
                {
                    int c = p.Cell;
                    bool fits = WorldMap.Yield(m.Resource[c]) == p.TaskResource && m.ResourceAmount[c] > 0;
                    if (fits && p.CarryAmount < CarryMax)
                    {
                        m.ResourceAmount[c]--;
                        p.CarryType = p.TaskResource; p.CarryAmount++; p.TotalGathered++;
                        if (m.ResourceAmount[c] == 0 && m.Resource[c] != NaturalResource.Beeren)
                        {
                            w.AddLog("Natur", $"{m.Resource[c]} bei {m.X(c)},{m.Y(c)} erschöpft.");
                            m.Resource[c] = NaturalResource.Keine;
                        }
                    }
                    bool depleted = m.ResourceAmount[c] <= 0;
                    if (p.CarryAmount >= CarryMax || depleted)
                    {
                        if (p.CarryAmount > 0) StartDeliver(sim, p); else SetIdle(p, 0);
                    }
                    break;
                }
                case PersonTask.Abliefern:
                {
                    var b = w.BuildingById(p.TargetBuildingId);
                    int put = Math.Min(p.CarryAmount, Economy.FreeCapacity(b, sim.Content));
                    b.Stock[(int)p.CarryType] += put;
                    p.CarryAmount -= put;
                    if (p.CarryAmount > 0) StartDeliver(sim, p); else SetIdle(p, 0);
                    break;
                }
                case PersonTask.MaterialHolen:
                {
                    var src = w.BuildingById(p.TargetBuildingId);
                    int r = (int)p.TaskResource;
                    src.Stock[r] -= p.HaulAmount;
                    src.Reserved[r] -= p.HaulAmount;
                    p.CarryType = p.TaskResource; p.CarryAmount = p.HaulAmount;
                    var site = w.BuildingById(p.HaulSiteId);
                    p.HaulSiteId = 0; p.HaulAmount = 0;
                    p.Task = PersonTask.MaterialBringen; p.TargetBuildingId = site.Id;
                    if (!GoTo(sim, p, site.Cell))
                        throw new InvalidOperationException($"Person {p.Id}: Baustelle {site.Id} unerreichbar, obwohl Lager erreichbar war.");
                    break;
                }
                case PersonTask.MaterialBringen:
                {
                    var site = w.BuildingById(p.TargetBuildingId);
                    int r = (int)p.CarryType;
                    site.Delivered[r] += p.CarryAmount;
                    site.Incoming[r] -= p.CarryAmount;
                    p.CarryAmount = 0;
                    SetIdle(p, 0);
                    break;
                }
                case PersonTask.StrasseBauen:
                {
                    int c = p.Cell;
                    if (m.RoadWork[c] == 0) { SetIdle(p, 0); break; }
                    m.RoadWork[c]--;
                    if (m.RoadWork[c] == 0)
                    {
                        m.Road[c] = true;
                        var s = w.SettlementById(p.SettlementId);
                        if (OpenRoadWork(m, s) == 0) w.AddLog("Bau", $"{s.Name}: alle geplanten Straßenfelder sind fertig.");
                        SetIdle(p, 0);
                    }
                    break;
                }
                case PersonTask.Feldarbeit:
                {
                    var f = w.BuildingById(p.TargetBuildingId);
                    var def = sim.Content.Building(f.DefId);
                    if (IsGrowingSeason(w))
                    {
                        if (f.FieldGrowth < Math.Min(def.FieldCapacity, AllowedGrowth(w, def))) f.FieldGrowth++;
                        else SetIdle(p, 0);
                        break;
                    }
                    if (f.FieldGrowth >= 2 && p.CarryAmount < CarryMax)
                    {
                        f.FieldGrowth -= 2;
                        p.CarryType = ResourceType.Nahrung; p.CarryAmount++;
                    }
                    if (p.CarryAmount >= CarryMax || f.FieldGrowth < 2)
                    {
                        if (p.CarryAmount > 0) StartDeliver(sim, p); else SetIdle(p, 0);
                    }
                    break;
                }
                case PersonTask.Bauen:
                {
                    var site = w.BuildingById(p.TargetBuildingId);
                    if (site.Complete || !Construction.MaterialsComplete(site, sim.Content.Building(site.DefId))) { SetIdle(p, 0); break; }
                    site.LaborRemaining--;
                    if (site.LaborRemaining <= 0) { Construction.Finish(w, sim.Content, site); SetIdle(p, 0); }
                    break;
                }
            }
        }
    }
}
