using System.Collections.Generic;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>Gültigkeitsprüfungen für Bauen und Straßen. Rückgabe null = gültig, sonst verständlicher Grund.</summary>
    public static class BuildRules
    {
        public const int BuildRadius = 12;
        public const int RoadRadius = 16;
        public const int MaxRoadLength = 40;
        public const int RoadStoneCostPerCell = 1;

        public static string CheckBuildingCell(WorldState w, int civId, int cell)
        {
            var m = w.Map;
            if (!m.InBounds(cell)) return "Zelle liegt außerhalb der Karte.";
            if (m.Biomes[cell] != Biome.Wiese) return $"Bauen nur auf Wiese möglich (hier: {m.Biomes[cell]}).";
            if (m.Resource[cell] != NaturalResource.Keine) return $"Zelle ist nicht frei ({m.Resource[cell]}).";
            if (m.BuildingAt[cell] != 0) return "Hier steht bereits ein Gebäude.";
            if (m.Road[cell]) return "Auf einer Straße kann nicht gebaut werden.";
            var s = Economy.NearestSettlementOfCiv(w, civId, cell);
            if (s == null) return "Diese Zivilisation hat keine Siedlung.";
            if (m.Manhattan(s.CenterCell, cell) > BuildRadius) return $"Zu weit von {s.Name} entfernt (max. {BuildRadius} Felder).";
            return null;
        }

        /// <summary>L-förmige Strecke: erst entlang x, dann entlang y. Enthält Start und Ziel.</summary>
        public static List<int> RoadCells(WorldMap m, int from, int to)
        {
            var cells = new List<int>();
            int x = m.X(from), y = m.Y(from), tx = m.X(to), ty = m.Y(to);
            cells.Add(m.Index(x, y));
            while (x != tx) { x += tx > x ? 1 : -1; cells.Add(m.Index(x, y)); }
            while (y != ty) { y += ty > y ? 1 : -1; cells.Add(m.Index(x, y)); }
            return cells;
        }

        /// <summary>Neue Straßenzellen (ohne vorhandene Straßen und Gebäudezellen) oder Grund für Ablehnung.</summary>
        public static string CheckRoad(WorldState w, int civId, int from, int to, out List<int> newCells, out Settlement payer)
        {
            newCells = new List<int>(); payer = null;
            var m = w.Map;
            if (!m.InBounds(from) || !m.InBounds(to)) return "Straße liegt außerhalb der Karte.";
            payer = Economy.NearestSettlementOfCiv(w, civId, from);
            if (payer == null) return "Diese Zivilisation hat keine Siedlung.";
            if (m.Manhattan(payer.CenterCell, from) > RoadRadius || m.Manhattan(payer.CenterCell, to) > RoadRadius)
                return $"Straßen nur bis {RoadRadius} Felder um {payer.Name}.";
            var cells = RoadCells(m, from, to);
            if (cells.Count > MaxRoadLength) return $"Straße zu lang ({cells.Count} > {MaxRoadLength} Felder).";
            foreach (var c in cells)
            {
                if (!m.IsWalkable(c)) return $"Straße kreuzt unbegehbares Gelände ({m.Biomes[c]}) bei {m.X(c)},{m.Y(c)}.";
                if (!m.Road[c] && m.BuildingAt[c] == 0) newCells.Add(c);
            }
            if (newCells.Count == 0) return "Hier liegt schon überall Straße.";
            return null;
        }
    }
}
