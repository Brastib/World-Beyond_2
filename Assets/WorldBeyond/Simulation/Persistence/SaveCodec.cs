using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Persistence
{
    public sealed class SaveLoadException : Exception
    {
        public SaveLoadException(string message) : base(message) { }
        public SaveLoadException(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class SaveHeader
    {
        public int FormatVersion;
        public string SaveName = "";
        public DateTime SavedUtc;
        public string GameVersion = "";
        public ulong Seed;
        public long Tick;
        public int Population;
        public string ChecksumHex = "";
    }

    /// <summary>
    /// Dateiformat: "WBRS" | int Formatversion | Header | int Länge | Nutzdaten | SHA-256(Nutzdaten).
    /// Header ist ohne Laden der Welt lesbar (Slot-Liste). Prüfsumme deckt die Nutzdaten ab.
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>v1: M0. v2: Beruf, Geburtstag des Haushalts, Siedlungsstatus/-zähler (M1). v3: Straßenplanung, Feldwachstum. v4: Zufriedenheit, Baupriorität, Siedlungsverlauf.</summary>
        public const int CurrentFormatVersion = 4;
        public const string GameVersion = "0.0.1-m0";
        static readonly byte[] Magic = Encoding.ASCII.GetBytes("WBRS");

        public static byte[] Encode(WorldState w, string saveName, DateTime savedUtc)
        {
            byte[] payload = WorldSerializer.Serialize(w);
            byte[] hash = Sha256(payload);
            using (var ms = new MemoryStream())
            using (var o = new BinaryWriter(ms))
            {
                o.Write(Magic); o.Write(CurrentFormatVersion);
                o.Write(saveName ?? ""); o.Write(savedUtc.ToUniversalTime().Ticks); o.Write(GameVersion);
                o.Write(w.Seed); o.Write(w.Tick); o.Write(w.People.Count);
                o.Write(payload.Length); o.Write(payload); o.Write(hash);
                o.Flush();
                return ms.ToArray();
            }
        }

        public static SaveHeader ReadHeader(byte[] file) { Parse(file, out var h, out _); return h; }

        public static WorldState Decode(byte[] file, out SaveHeader header)
        {
            Parse(file, out header, out var payload);
            WorldState w;
            try { w = WorldSerializer.Deserialize(payload, header.FormatVersion); }
            catch (SaveLoadException) { throw; }
            catch (Exception ex) { throw new SaveLoadException("Spielstand-Inhalt nicht lesbar: " + ex.Message, ex); }
            var errors = WorldValidator.Validate(w);
            if (errors.Count > 0) throw new SaveLoadException("Spielstand verletzt Invarianten: " + string.Join("; ", errors.GetRange(0, Math.Min(5, errors.Count))));
            return w;
        }

        static void Parse(byte[] file, out SaveHeader h, out byte[] payload)
        {
            if (file == null || file.Length < 8) throw new SaveLoadException("Datei ist leer oder zu kurz.");
            try
            {
                using (var ms = new MemoryStream(file))
                using (var i = new BinaryReader(ms))
                {
                    var magic = i.ReadBytes(4);
                    if (magic.Length != 4 || magic[0] != Magic[0] || magic[1] != Magic[1] || magic[2] != Magic[2] || magic[3] != Magic[3])
                        throw new SaveLoadException("Keine World-Beyond-Reworked-Spielstanddatei.");
                    h = new SaveHeader { FormatVersion = i.ReadInt32() };
                    if (h.FormatVersion > CurrentFormatVersion)
                        throw new SaveLoadException($"Spielstand stammt aus neuerer Version (Format {h.FormatVersion}, unterstützt bis {CurrentFormatVersion}).");
                    if (h.FormatVersion < 1) throw new SaveLoadException($"Unbekannte Formatversion {h.FormatVersion}.");
                    // Ältere Formate werden in WorldSerializer.Deserialize mit deterministischen Standardwerten migriert (ADR-005).
                    h.SaveName = i.ReadString(); h.SavedUtc = new DateTime(i.ReadInt64(), DateTimeKind.Utc); h.GameVersion = i.ReadString();
                    h.Seed = i.ReadUInt64(); h.Tick = i.ReadInt64(); h.Population = i.ReadInt32();
                    int len = i.ReadInt32();
                    if (len < 0 || len > file.Length) throw new SaveLoadException("Ungültige Nutzdatenlänge.");
                    payload = i.ReadBytes(len);
                    var hash = i.ReadBytes(32);
                    if (payload.Length != len || hash.Length != 32) throw new SaveLoadException("Datei ist abgeschnitten.");
                    if (ms.Position != ms.Length) throw new SaveLoadException("Unerwartete Daten nach der Prüfsumme.");
                    var actual = Sha256(payload);
                    for (int k = 0; k < 32; k++)
                        if (actual[k] != hash[k]) throw new SaveLoadException("Prüfsumme stimmt nicht: Datei ist beschädigt.");
                    h.ChecksumHex = Hex(hash);
                }
            }
            catch (EndOfStreamException ex) { throw new SaveLoadException("Datei ist abgeschnitten.", ex); }
        }

        /// <summary>Zustands-Fingerabdruck für Determinismus-Tests und Debug-Overlay.</summary>
        public static string StateHash(WorldState w) => Hex(Sha256(WorldSerializer.Serialize(w)));

        static byte[] Sha256(byte[] data) { using (var sha = SHA256.Create()) return sha.ComputeHash(data); }

        static string Hex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }
}
