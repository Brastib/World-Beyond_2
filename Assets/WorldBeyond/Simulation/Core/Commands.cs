namespace WorldBeyond.Simulation.Core
{
    public enum CommandType : byte { GebaeudePlatzieren = 1, StrassePlatzieren = 2, SpielerZivilisationWaehlen = 3, BauPrioritaet = 4 }

    /// <summary>
    /// Spieleraktion. Wird vor dem nächsten Tick ausgeführt und mit Ergebnis in der Historie gespeichert.
    /// Flache Struktur, damit Speicherung und Wiedergabe trivial bleiben.
    /// </summary>
    public sealed class GameCommand
    {
        public CommandType Type;
        /// <summary>Tick, vor dessen Verarbeitung der Befehl ausgeführt wird.</summary>
        public long Tick;
        public int CivId;
        public string DefId = "";
        public int CellA;
        public int CellB;
        public bool Succeeded;
        public string Result = "";

        public GameCommand Clone() => new GameCommand
        {
            Type = Type, Tick = Tick, CivId = CivId, DefId = DefId, CellA = CellA, CellB = CellB,
            Succeeded = Succeeded, Result = Result
        };

        public static GameCommand PlaceBuilding(int civId, string defId, int cell) =>
            new GameCommand { Type = CommandType.GebaeudePlatzieren, CivId = civId, DefId = defId, CellA = cell, CellB = cell };

        /// <summary>Setzt die Priorität einer eigenen Baustelle (CellA = Gebäude-Id, CellB = 1 hoch / 0 normal).</summary>
        public static GameCommand SetPriority(int civId, int buildingId, bool high) =>
            new GameCommand { Type = CommandType.BauPrioritaet, CivId = civId, CellA = buildingId, CellB = high ? 1 : 0 };

        /// <summary>civId 0 = Beobachtermodus.</summary>
        public static GameCommand ChoosePlayerCiv(int civId) =>
            new GameCommand { Type = CommandType.SpielerZivilisationWaehlen, CivId = civId };

        public static GameCommand PlaceRoad(int civId, int fromCell, int toCell) =>
            new GameCommand { Type = CommandType.StrassePlatzieren, CivId = civId, CellA = fromCell, CellB = toCell };
    }
}
