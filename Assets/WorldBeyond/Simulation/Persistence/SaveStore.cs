using System;
using System.Collections.Generic;
using System.IO;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Persistence
{
    public sealed class SlotInfo
    {
        public string Slot = "";
        public string Path = "";
        public SaveHeader Header;
        /// <summary>Gesetzt, wenn die Datei nicht lesbar ist. Die Datei wird dann nie überschrieben.</summary>
        public string Error;
    }

    /// <summary>
    /// Speicherplätze in einem Ordner. Schreiben über Temp-Datei + Ersetzen (kein halbes File bei Absturz).
    /// Eine vorhandene, beschädigte Datei wird vor dem Speichern nach *.corrupt-*.wbsave verschoben, nicht überschrieben.
    /// </summary>
    public sealed class SaveStore
    {
        public const string Extension = ".wbsave";
        public const int AutosaveSlots = 3;
        public readonly string Directory;

        public SaveStore(string directory)
        {
            Directory = directory;
            System.IO.Directory.CreateDirectory(directory);
        }

        public string PathFor(string slot)
        {
            foreach (var ch in slot)
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')) throw new ArgumentException("Ungültiger Slotname: " + slot);
            return System.IO.Path.Combine(Directory, slot + Extension);
        }

        public SlotInfo Save(string slot, WorldState w, string displayName, DateTime nowUtc)
        {
            string target = PathFor(slot);
            if (File.Exists(target))
            {
                try { SaveCodec.ReadHeader(File.ReadAllBytes(target)); }
                catch (SaveLoadException)
                {
                    string aside = System.IO.Path.Combine(Directory, $"{slot}.corrupt-{nowUtc:yyyyMMddHHmmss}{Extension}");
                    File.Move(target, aside);
                }
            }
            byte[] data = SaveCodec.Encode(w, displayName, nowUtc);
            string tmp = target + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush(true);
            }
            // Gegenprobe vor dem Ersetzen: was nicht lesbar ist, ersetzt keinen guten Spielstand.
            SaveCodec.ReadHeader(File.ReadAllBytes(tmp));
            if (File.Exists(target)) File.Replace(tmp, target, target + ".bak");
            else File.Move(tmp, target);
            return Info(slot);
        }

        public WorldState Load(string slot, out SaveHeader header)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) throw new SaveLoadException($"Speicherplatz '{slot}' ist leer.");
            return SaveCodec.Decode(File.ReadAllBytes(path), out header);
        }

        /// <summary>Rotiert über autosave_0..2: überschreibt den ältesten oder einen leeren Platz.</summary>
        public SlotInfo Autosave(WorldState w, DateTime nowUtc)
        {
            string chosen = null; DateTime oldest = DateTime.MaxValue;
            for (int k = 0; k < AutosaveSlots; k++)
            {
                var info = Info("autosave_" + k);
                if (info.Header == null) { chosen = info.Slot; break; }
                if (info.Header.SavedUtc < oldest) { oldest = info.Header.SavedUtc; chosen = info.Slot; }
            }
            return Save(chosen, w, "Autosave", nowUtc);
        }

        public SlotInfo Info(string slot)
        {
            var info = new SlotInfo { Slot = slot, Path = PathFor(slot) };
            if (!File.Exists(info.Path)) return info;
            try { info.Header = SaveCodec.ReadHeader(File.ReadAllBytes(info.Path)); }
            catch (SaveLoadException ex) { info.Error = ex.Message; }
            return info;
        }

        public List<SlotInfo> List()
        {
            var result = new List<SlotInfo>();
            var files = System.IO.Directory.GetFiles(Directory, "*" + Extension);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var f in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(f);
                if (name.Contains(".corrupt-")) continue;
                result.Add(Info(name));
            }
            return result;
        }
    }
}
