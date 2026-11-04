using System;

namespace WorldBeyond.Simulation.Core
{
    /// <summary>Zufallsschnittstelle der Simulation. Alle Simulationszufälle laufen hierüber.</summary>
    public interface IRandomSource
    {
        ulong NextULong();
        int NextInt(int maxExclusive);
    }

    /// <summary>
    /// SplitMix64. Bewusst eigene Implementierung statt System.Random,
    /// weil dessen Algorithmus nicht über .NET-/Unity-Versionen garantiert ist.
    /// Der gesamte Zustand ist <see cref="State"/> und wird gespeichert.
    /// </summary>
    public sealed class DeterministicRandom : IRandomSource
    {
        public ulong State;

        public DeterministicRandom(ulong seed) { State = seed; }

        public ulong NextULong()
        {
            ulong z = (State += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        public int Range(int minInclusive, int maxExclusive) => minInclusive + NextInt(maxExclusive - minInclusive);

        /// <summary>Zustandsloser Hash für Kartengenerierung (gleiche Eingabe = gleiche Ausgabe).</summary>
        public static ulong Hash(ulong seed, long a, long b, ulong salt)
        {
            ulong z = seed ^ (salt * 0xD6E8FEB86659FD93UL);
            z ^= (ulong)a * 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 31)) * 0xBF58476D1CE4E5B9UL;
            z ^= (ulong)b * 0xC2B2AE3D27D4EB4FUL;
            z = (z ^ (z >> 29)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 32);
        }
    }
}
