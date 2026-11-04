using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Persistence;

namespace WorldBeyond.Tests
{
    public class SaveLoadTests
    {
        static readonly DateTime Now = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Roundtrip_IsByteIdentical_AndContinuesIdentically()
        {
            var a = TestUtil.NewSim();
            a.Run(500);
            var b = TestUtil.Reload(a);
            CollectionAssert.AreEqual(WorldSerializer.Serialize(a.World), WorldSerializer.Serialize(b.World));
            a.Run(1500); b.Run(1500);
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void SaveDuringActiveConstructionAndHauling_LoadsAndContinuesIdentically()
        {
            var a = TestUtil.NewSim();
            a.Submit(GameCommand.PlaceBuilding(a.World.PlayerCivId, DefaultContent.Storage, TestUtil.FindPlayerBuildCell(a)));
            bool found = false;
            for (int t = 0; t < 3000 && !found; t++)
            {
                a.Step();
                found = a.World.Buildings.Any(x => !x.Complete && Res.Sum(x.Incoming) > 0)
                        && a.World.People.Any(p => p.Task == PersonTask.MaterialBringen && p.PathPos < p.Path.Count);
            }
            Assert.IsTrue(found, "Kein Zeitpunkt mit laufender Baustelle + Träger unterwegs gefunden");
            var b = TestUtil.Reload(a);
            TestUtil.AssertValid(b.World);
            a.Run(1000); b.Run(1000);
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void PendingCommand_IsSavedAndExecutedAfterLoad()
        {
            var a = TestUtil.NewSim();
            a.Submit(GameCommand.PlaceBuilding(a.World.PlayerCivId, DefaultContent.Storage, TestUtil.FindPlayerBuildCell(a)));
            var b = TestUtil.Reload(a);
            Assert.AreEqual(1, b.World.PendingCommands.Count);
            a.Step(); b.Step();
            Assert.IsTrue(b.World.CommandHistory[0].Succeeded, b.World.CommandHistory[0].Result);
            Assert.AreEqual(SaveCodec.StateHash(a.World), SaveCodec.StateHash(b.World));
        }

        [Test]
        public void FlippedByte_IsRejectedByChecksum()
        {
            var sim = TestUtil.NewSim();
            var bytes = SaveCodec.Encode(sim.World, "x", Now);
            bytes[bytes.Length - 100] ^= 0xFF;
            var ex = Assert.Throws<SaveLoadException>(() => SaveCodec.Decode(bytes, out _));
            StringAssert.Contains("Prüfsumme", ex.Message);
        }

        [Test]
        public void TruncatedFile_IsRejected()
        {
            var bytes = SaveCodec.Encode(TestUtil.NewSim().World, "x", Now);
            Assert.Throws<SaveLoadException>(() => SaveCodec.Decode(bytes.Take(bytes.Length / 2).ToArray(), out _));
            Assert.Throws<SaveLoadException>(() => SaveCodec.Decode(new byte[0], out _));
        }

        [Test]
        public void FutureFormatVersion_IsRejectedWithMessage()
        {
            var bytes = SaveCodec.Encode(TestUtil.NewSim().World, "x", Now);
            BitConverter.GetBytes(SaveCodec.CurrentFormatVersion + 1).CopyTo(bytes, 4);
            var ex = Assert.Throws<SaveLoadException>(() => SaveCodec.Decode(bytes, out _));
            StringAssert.Contains("neuerer Version", ex.Message);
        }

        [Test]
        public void Header_IsReadableWithoutWorld()
        {
            var sim = TestUtil.NewSim(); sim.Run(48);
            var h = SaveCodec.ReadHeader(SaveCodec.Encode(sim.World, "Mein Spiel", Now));
            Assert.AreEqual("Mein Spiel", h.SaveName);
            Assert.AreEqual(TestUtil.Seed, h.Seed);
            Assert.AreEqual(48, h.Tick);
            Assert.AreEqual(Now, h.SavedUtc);
            Assert.AreEqual(18, h.Population);
        }

        [Test]
        public void SaveStore_SlotsAutosaveAndCorruptFilesAreKept()
        {
            string dir = Path.Combine(Path.GetTempPath(), "wbr_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveStore(dir);
                var sim = TestUtil.NewSim(); sim.Run(30);
                store.Save("slot1", sim.World, "Erster", Now);
                var loaded = store.Load("slot1", out var header);
                Assert.AreEqual(SaveCodec.StateHash(sim.World), SaveCodec.StateHash(loaded));
                Assert.AreEqual("Erster", header.SaveName);

                // Beschädigen → Laden schlägt fehl, Datei bleibt; neues Speichern verschiebt sie beiseite.
                var p = store.PathFor("slot1");
                var raw = File.ReadAllBytes(p); raw[raw.Length - 5] ^= 1; File.WriteAllBytes(p, raw);
                Assert.Throws<SaveLoadException>(() => store.Load("slot1", out _));
                Assert.IsNotNull(store.Info("slot1").Error);
                store.Save("slot1", sim.World, "Zweiter", Now);
                Assert.AreEqual(1, Directory.GetFiles(dir, "slot1.corrupt-*").Length);
                Assert.AreEqual("Zweiter", store.Info("slot1").Header.SaveName);

                for (int k = 0; k < 4; k++) store.Autosave(sim.World, Now.AddMinutes(k));
                Assert.AreEqual(3, store.List().Count(s => s.Slot.StartsWith("autosave_")));
                Assert.Throws<SaveLoadException>(() => store.Load("leer", out _));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
