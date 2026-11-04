using System;

namespace WorldBeyond.Simulation.Core
{
    public enum Biome : byte { Wasser, Wiese, Wald, Huegel, Gebirge }

    public enum NaturalResource : byte { Keine, Beeren, Baeume, Felsen }

    /// <summary>
    /// Quadratisches Simulationsraster (ADR-002), Zeilen-Index = y * Width + x, 4er-Nachbarschaft.
    /// Darstellung liest nur; Gebäude-Belegung wird aus der Gebäudeliste abgeleitet.
    /// </summary>
    public sealed class WorldMap
    {
        public const int ChunkSize = 32;
        public const short BerryMax = 30;

        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Elevation;
        public readonly byte[] Moisture;
        public readonly Biome[] Biomes;
        public readonly NaturalResource[] Resource;
        public readonly short[] ResourceAmount;
        public readonly bool[] Road;
        /// <summary>Geplante Straße: verbleibende Arbeitsstunden je Zelle, 0 = keine Planung (Save v3).</summary>
        public readonly byte[] RoadWork;
        /// <summary>Abgeleitet (nicht gespeichert): Gebäude-ID je Zelle, 0 = frei.</summary>
        public readonly int[] BuildingAt;

        public WorldMap(int width, int height)
        {
            if (width <= 0 || height <= 0 || width % ChunkSize != 0 || height % ChunkSize != 0)
                throw new ArgumentException("Kartengröße muss ein positives Vielfaches von " + ChunkSize + " sein.");
            Width = width; Height = height;
            int n = width * height;
            Elevation = new byte[n]; Moisture = new byte[n]; Biomes = new Biome[n];
            Resource = new NaturalResource[n]; ResourceAmount = new short[n]; Road = new bool[n]; RoadWork = new byte[n]; BuildingAt = new int[n];
        }

        public int CellCount => Width * Height;
        public int ChunksX => Width / ChunkSize;
        public int ChunksY => Height / ChunkSize;
        public int Index(int x, int y) => y * Width + x;
        public int X(int i) => i % Width;
        public int Y(int i) => i / Width;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(int i) => i >= 0 && i < CellCount;
        public int Manhattan(int a, int b) => Math.Abs(X(a) - X(b)) + Math.Abs(Y(a) - Y(b));

        public bool IsWalkable(int i) => Biomes[i] != Biome.Wasser && Biomes[i] != Biome.Gebirge;

        /// <summary>Bewegungspunkte zum Betreten einer Zelle. Personen erhalten 2 Punkte pro Tick.</summary>
        public int MoveCost(int i)
        {
            if (Road[i]) return 1;
            switch (Biomes[i])
            {
                case Biome.Wiese: return 2;
                case Biome.Wald: return 3;
                case Biome.Huegel: return 3;
                default: return int.MaxValue;
            }
        }

        public static ResourceType? Yield(NaturalResource r)
        {
            switch (r)
            {
                case NaturalResource.Beeren: return ResourceType.Nahrung;
                case NaturalResource.Baeume: return ResourceType.Holz;
                case NaturalResource.Felsen: return ResourceType.Stein;
                default: return null;
            }
        }
    }
}
