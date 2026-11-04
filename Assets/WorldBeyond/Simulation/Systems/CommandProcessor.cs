using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    public static class CommandProcessor
    {
        /// <summary>Prüft ohne Zustandsänderung. null = ausführbar.</summary>
        public static string Validate(WorldSimulation sim, GameCommand c)
        {
            var w = sim.World;
            if (c.Type == CommandType.SpielerZivilisationWaehlen)
                return c.CivId == 0 || w.Civ(c.CivId) != null ? null : "Unbekannte Zivilisation.";
            if (w.Civ(c.CivId) == null) return "Unbekannte Zivilisation.";
            if (c.CivId != w.PlayerCivId) return "Befehle sind nur für die gespielte Zivilisation erlaubt.";
            switch (c.Type)
            {
                case CommandType.GebaeudePlatzieren:
                    if (!sim.Content.HasBuilding(c.DefId)) return "Unbekanntes Gebäude: " + c.DefId;
                    if (!sim.Content.Building(c.DefId).PlayerBuildable) return sim.Content.Building(c.DefId).Name + " kann nicht vom Spieler gebaut werden.";
                    return BuildRules.CheckBuildingCell(w, c.CivId, c.CellA);
                case CommandType.StrassePlatzieren:
                    var reason = BuildRules.CheckRoad(w, c.CivId, c.CellA, c.CellB, out var cells, out var payer);
                    if (reason != null) return reason;
                    int cost = cells.Count * BuildRules.RoadStoneCostPerCell;
                    int have = Economy.SettlementAvailable(w, sim.Content, payer.Id, ResourceType.Stein);
                    if (have < cost) return $"Zu wenig Stein in {payer.Name}: {have}/{cost}.";
                    return null;
                default:
                    return "Unbekannter Befehl.";
            }
        }

        public static void Execute(WorldSimulation sim, GameCommand c)
        {
            var w = sim.World;
            string reason = Validate(sim, c);
            if (reason != null)
            {
                c.Succeeded = false; c.Result = reason;
                w.AddLog("Befehl", "Abgelehnt: " + reason);
                return;
            }
            if (c.Type == CommandType.SpielerZivilisationWaehlen)
            {
                w.PlayerCivId = c.CivId;
                c.Succeeded = true;
                c.Result = c.CivId == 0 ? "Beobachtermodus." : "Du spielst jetzt " + w.Civ(c.CivId).Name + ".";
                w.AddLog("Spieler", c.Result);
                return;
            }
            if (c.Type == CommandType.GebaeudePlatzieren)
            {
                var s = Economy.NearestSettlementOfCiv(w, c.CivId, c.CellA);
                var b = Construction.PlaceSite(w, sim.Content, c.DefId, s, c.CellA, 0);
                c.Succeeded = true; c.Result = $"Baustelle {sim.Content.Building(c.DefId).Name} #{b.Id} angelegt.";
                w.AddLog("Bau", $"{s.Name}: Baustelle {sim.Content.Building(c.DefId).Name} angelegt (Spielerauftrag).", b.Id);
            }
            else
            {
                BuildRules.CheckRoad(w, c.CivId, c.CellA, c.CellB, out var cells, out var payer);
                int cost = cells.Count * BuildRules.RoadStoneCostPerCell;
                if (!Economy.TryTake(w, sim.Content, payer.Id, ResourceType.Stein, cost))
                {
                    c.Succeeded = false; c.Result = "Stein konnte nicht entnommen werden.";
                    w.AddLog("Befehl", "Abgelehnt: " + c.Result);
                    return;
                }
                foreach (var cell in cells) w.Map.RoadWork[cell] = BuildRules.RoadLaborPerCell;
                c.Succeeded = true; c.Result = $"{cells.Count} Straßenfelder geplant ({cost} Stein, je {BuildRules.RoadLaborPerCell} Std. Arbeit).";
                w.AddLog("Bau", $"{payer.Name}: {cells.Count} Straßenfelder geplant, {cost} Stein verbraucht. Baumeister bauen sie Feld für Feld.");
            }
        }
    }
}
