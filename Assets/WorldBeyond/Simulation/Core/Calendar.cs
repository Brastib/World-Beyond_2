namespace WorldBeyond.Simulation.Core
{
    public enum Season : byte { Fruehling, Sommer, Herbst, Winter }

    /// <summary>1 Tick = 1 Stunde. 24 Stunden = 1 Tag (kleinste Wirtschaftseinheit), 30 Tage = 1 Monat, 12 Monate = 1 Jahr.</summary>
    public static class Calendar
    {
        public const int TicksPerDay = 24;
        public const int DaysPerMonth = 30;
        public const int MonthsPerYear = 12;
        public const int DaysPerYear = DaysPerMonth * MonthsPerYear;

        public static long Day(long tick) => tick / TicksPerDay;
        public static int Hour(long tick) => (int)(tick % TicksPerDay);
        public static int DayOfMonth(long tick) => (int)(Day(tick) % DaysPerMonth) + 1;
        public static int Month(long tick) => (int)(Day(tick) / DaysPerMonth % MonthsPerYear) + 1;
        public static int Year(long tick) => (int)(Day(tick) / DaysPerYear) + 1;
        public static Season SeasonOf(long tick) => (Season)((Month(tick) - 1) / 3);

        public static string SeasonName(Season s)
        {
            switch (s)
            {
                case Season.Fruehling: return "Frühling";
                case Season.Sommer: return "Sommer";
                case Season.Herbst: return "Herbst";
                default: return "Winter";
            }
        }

        public static string Format(long tick) =>
            $"Jahr {Year(tick)}, Monat {Month(tick)}, Tag {DayOfMonth(tick)}, {Hour(tick):00}:00 ({SeasonName(SeasonOf(tick))})";
    }
}
