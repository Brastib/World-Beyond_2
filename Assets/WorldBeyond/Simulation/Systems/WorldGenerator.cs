using System;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Seed-Karte aus ganzzahligem Value-Noise (keine Gleitkommazahlen → plattformstabil).
    /// Biom-Schwellen sind Perzentile, damit jede Karte ähnliche Anteile Wasser/Land/Hügel hat.
    /// </summary>
    public static class WorldGenerator
    {
        public const int DefaultSize = 128;

        public static WorldMap Generate(ulong seed, int width = DefaultSize, int height = DefaultSize)
        {
            var m = new WorldMap(width, height);
            int n = m.CellCount;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = m.Index(x, y);
                    m.Elevation[i] = (byte)Fractal(seed, 1, x, y);
                    m.Moisture[i] = (byte)Fractal(seed, 2, x, y);
                }
            int water = Percentile(m.Elevation, 30), hills = Percentile(m.Elevation, 80), mountain = Percentile(m.Elevation, 94);
            int forest = Percentile(m.Moisture, 58);
            for (int i = 0; i < n; i++)
            {
                byte e = m.Elevation[i];
                Biome b;
                if (e < water) b = Biome.Wasser;
                else if (e >= mountain) b = Biome.Gebirge;
                else if (e >= hills) b = Biome.Huegel;
                else if (m.Moisture[i] >= forest) b = Biome.Wald;
                else b = Biome.Wiese;
                m.Biomes[i] = b;
                switch (b)
                {
                    case Biome.Wald: m.Resource[i] = NaturalResource.Baeume; m.ResourceAmount[i] = 30; break;
                    case Biome.Huegel: m.Resource[i] = NaturalResource.Felsen; m.ResourceAmount[i] = 40; break;
                    case Biome.Wiese:
                        if (DeterministicRandom.Hash(seed, m.X(i), m.Y(i), 3) % 6 == 0)
                        { m.Resource[i] = NaturalResource.Beeren; m.ResourceAmount[i] = 20; }
                        break;
                }
            }
            return m;
        }

        static int Fractal(ulong seed, ulong salt, int x, int y) =>
            (Value(seed, salt, x, y, 32) * 4 + Value(seed, salt + 10, x, y, 16) * 2 + Value(seed, salt + 20, x, y, 8)) / 7;

        static int Value(ulong seed, ulong salt, int x, int y, int s)
        {
            int x0 = x / s, y0 = y / s, fx = x % s, fy = y % s;
            int v00 = Corner(seed, salt, x0, y0), v10 = Corner(seed, salt, x0 + 1, y0);
            int v01 = Corner(seed, salt, x0, y0 + 1), v11 = Corner(seed, salt, x0 + 1, y0 + 1);
            int top = v00 * (s - fx) + v10 * fx;
            int bottom = v01 * (s - fx) + v11 * fx;
            return (top * (s - fy) + bottom * fy) / (s * s);
        }

        static int Corner(ulong seed, ulong salt, int x, int y) => (int)(DeterministicRandom.Hash(seed, x, y, salt) & 255);

        static int Percentile(byte[] values, int p)
        {
            var hist = new int[256];
            foreach (var v in values) hist[v]++;
            int target = (int)((long)values.Length * p / 100), acc = 0;
            for (int v = 0; v < 256; v++) { acc += hist[v]; if (acc > target) return v; }
            return 255;
        }
    }
}
