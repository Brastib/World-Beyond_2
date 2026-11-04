namespace WorldBeyond.Simulation.Core
{
    /// <summary>M0-Untermenge des Ressourcenkatalogs. Neue Werte nur anhängen (Save-Kompatibilität).</summary>
    public enum ResourceType : byte { Nahrung = 0, Holz = 1, Stein = 2 }

    public static class Res
    {
        public const int Count = 3;
        public static readonly ResourceType[] All = { ResourceType.Nahrung, ResourceType.Holz, ResourceType.Stein };
        public static int[] Empty() => new int[Count];
        public static int Sum(int[] a) { int s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
        public static string Name(ResourceType r) => r.ToString();
    }
}
