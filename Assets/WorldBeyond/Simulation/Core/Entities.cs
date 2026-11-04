using System.Collections.Generic;

namespace WorldBeyond.Simulation.Core
{
    public interface IEntity { int Id { get; } }

    public sealed class Civilization : IEntity
    {
        public int Id { get; set; }
        public string Name = "";
        public uint ColorRgb;
        public int CapitalSettlementId;
    }

    public sealed class Settlement : IEntity
    {
        public int Id { get; set; }
        public int CivId;
        public string Name = "";
        public int CenterCell;
        /// <summary>Letzte Wachstumsbegründung (Save v2). Wird täglich neu bewertet, Änderungen kommen ins Protokoll.</summary>
        public string Status = "";
        public int Births;
        public int Deaths;
        /// <summary>Monatlicher Verlauf, höchstens <see cref="HistoryPoint.MaxPoints"/> Einträge (Save v4).</summary>
        public readonly List<HistoryPoint> History = new List<HistoryPoint>();
    }

    public sealed class HistoryPoint
    {
        public const int MaxPoints = 120;
        public long Tick;
        public int Population, Food, Wood, Stone, Satisfaction;
    }

    public sealed class Household : IEntity
    {
        public int Id { get; set; }
        public int SettlementId;
        public string Name = "";
        /// <summary>0 = kein eigenes Haus (wohnt im Ortszentrum).</summary>
        public int HomeBuildingId;
        /// <summary>0 = kein Hausbau beauftragt.</summary>
        public int PendingHouseSiteId;
        /// <summary>Tag der letzten Geburt, -1 = nie (Save v2).</summary>
        public int LastBirthDay = -1;
        public readonly List<int> MemberIds = new List<int>();
    }

    /// <summary>Beruf (Save v2). Kind = unter <see cref="Person.AdultAgeDays"/>, arbeitet nicht. Werte nur anhängen.</summary>
    public enum Role : byte { Sammler, Holzfaeller, Bergarbeiter, Baumeister, Traeger, Kind, Bauer }

    public enum PersonTask : byte { Leerlauf, Sammeln, Abliefern, MaterialHolen, MaterialBringen, Bauen, StrasseBauen, Feldarbeit }

    public sealed class Person : IEntity
    {
        public const int AdultAgeDays = 14 * Calendar.DaysPerYear;
        public int Id { get; set; }
        public Role Role;
        public bool IsAdult => AgeDays >= AdultAgeDays;
        public int HouseholdId;
        public int SettlementId;
        public int CivId;
        public string Name = "";
        public int AgeDays;
        public int Health = 100;
        /// <summary>0..100, bewegt sich täglich um 5 Richtung Zielwert (Save v4, Standard 60).</summary>
        public int Satisfaction = 60;
        /// <summary>0 = satt, 100 = verhungernd.</summary>
        public int Hunger;
        public int Cell;
        public PersonTask Task;
        public int TargetCell;
        public int TargetBuildingId;
        public ResourceType TaskResource;
        public ResourceType CarryType;
        public int CarryAmount;
        /// <summary>Wartezeit in Ticks, bevor wieder eine Aufgabe gesucht wird.</summary>
        public int Cooldown;
        /// <summary>Bei MaterialHolen: Ziel-Baustelle und geplante Menge.</summary>
        public int HaulSiteId;
        public int HaulAmount;
        public int MoveProgress;
        public readonly List<int> Path = new List<int>();
        public int PathPos;
        /// <summary>Lebenslauf-Zähler (historisch relevante Ereignisse kommen ins Ereignisprotokoll).</summary>
        public int TotalGathered;
    }

    public sealed class Building : IEntity
    {
        public int Id { get; set; }
        public string DefId = "";
        public int CivId;
        public int SettlementId;
        public int Cell;
        public bool Complete;
        public int LaborRemaining;
        /// <summary>Zustand 0..100 (Verfall/Reparatur ab M1, Feld existiert ab M0).</summary>
        public int Condition = 100;
        public int OwnerHouseholdId;
        /// <summary>Feld: angesammeltes Wachstum (Save v3). Ernte wandelt 2 Wachstum in 1 Nahrung.</summary>
        public int FieldGrowth;
        /// <summary>Spielerpriorität: Baustellen mit Vorrang werden zuerst beliefert und gebaut (Save v4).</summary>
        public bool HighPriority;
        public int[] Delivered = Res.Empty();
        /// <summary>Material, das Träger gerade zur Baustelle bringen.</summary>
        public int[] Incoming = Res.Empty();
        public int[] Stock = Res.Empty();
        /// <summary>Für Abholungen reservierte Menge aus <see cref="Stock"/>.</summary>
        public int[] Reserved = Res.Empty();
    }

    public sealed class LogEntry
    {
        public long Tick;
        public string Category = "";
        public string Text = "";
        public int EntityId;
    }
}
