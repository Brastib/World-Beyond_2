namespace WorldBeyond.Simulation.Core
{
    /// <summary>Frühzeit-Untermenge des Ressourcenkatalogs. Neue Werte nur anhängen (Save-Kompatibilität).</summary>
    public enum ResourceType : byte { Nahrung = 0, Holz = 1, Stein = 2, Werkzeug = 3 }

    public static class Res
    {
        /// <summary>Save v5: 4 Einträge (Werkzeug). Ältere Stände haben 3 und werden mit 0 aufgefüllt.</summary>
        public const int Count = 4;
        /// <summary>Nur die ersten Ressourcen kommen aus der Natur; Werkzeug entsteht nur in der Werkstatt.</summary>
        public const int GatherableCount = 3;
        public static readonly ResourceType[] All = { ResourceType.Nahrung, ResourceType.Holz, ResourceType.Stein, ResourceType.Werkzeug };
        public static int[] Empty() => new int[Count];
        public static int Sum(int[] a) { int s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
        public static string Name(ResourceType r) => r.ToString();
    }
}
