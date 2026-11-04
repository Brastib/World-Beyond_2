using System.Collections.Generic;
using System.IO;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Persistence
{
    /// <summary>
    /// Save-Schema v1: explizite Feldreihenfolge, keine Reflexion, keine Unity-Objekte (ADR-005).
    /// Jede Änderung am Schema erhöht <see cref="SaveCodec.CurrentFormatVersion"/> und braucht einen Migrationsschritt.
    /// Abgeleitete Daten (Gebäudebelegung der Karte) werden nicht gespeichert, sondern beim Laden neu berechnet.
    /// </summary>
    public static class WorldSerializer
    {
        public static byte[] Serialize(WorldState w)
        {
            using (var ms = new MemoryStream())
            using (var o = new BinaryWriter(ms))
            {
                o.Write(w.Seed); o.Write(w.Tick); o.Write(w.NextId); o.Write(w.ContentVersion); o.Write(w.PlayerCivId); o.Write(w.Rng.State);
                var m = w.Map;
                o.Write(m.Width); o.Write(m.Height);
                o.Write(m.Elevation); o.Write(m.Moisture);
                for (int i = 0; i < m.CellCount; i++) o.Write((byte)m.Biomes[i]);
                for (int i = 0; i < m.CellCount; i++) o.Write((byte)m.Resource[i]);
                for (int i = 0; i < m.CellCount; i++) o.Write(m.ResourceAmount[i]);
                for (int i = 0; i < m.CellCount; i++) o.Write(m.Road[i]);

                o.Write(w.Civilizations.Count);
                foreach (var c in w.Civilizations) { o.Write(c.Id); o.Write(c.Name); o.Write(c.ColorRgb); o.Write(c.CapitalSettlementId); }
                o.Write(w.Settlements.Count);
                foreach (var s in w.Settlements) { o.Write(s.Id); o.Write(s.CivId); o.Write(s.Name); o.Write(s.CenterCell); }
                o.Write(w.Households.Count);
                foreach (var h in w.Households)
                {
                    o.Write(h.Id); o.Write(h.SettlementId); o.Write(h.Name); o.Write(h.HomeBuildingId); o.Write(h.PendingHouseSiteId);
                    WriteList(o, h.MemberIds);
                }
                o.Write(w.People.Count);
                foreach (var p in w.People)
                {
                    o.Write(p.Id); o.Write(p.HouseholdId); o.Write(p.SettlementId); o.Write(p.CivId); o.Write(p.Name);
                    o.Write(p.AgeDays); o.Write(p.Health); o.Write(p.Hunger); o.Write(p.Cell); o.Write((byte)p.Task);
                    o.Write(p.TargetCell); o.Write(p.TargetBuildingId); o.Write((byte)p.TaskResource); o.Write((byte)p.CarryType);
                    o.Write(p.CarryAmount); o.Write(p.Cooldown); o.Write(p.HaulSiteId); o.Write(p.HaulAmount);
                    o.Write(p.MoveProgress); WriteList(o, p.Path); o.Write(p.PathPos); o.Write(p.TotalGathered);
                }
                o.Write(w.Buildings.Count);
                foreach (var b in w.Buildings)
                {
                    o.Write(b.Id); o.Write(b.DefId); o.Write(b.CivId); o.Write(b.SettlementId); o.Write(b.Cell); o.Write(b.Complete);
                    o.Write(b.LaborRemaining); o.Write(b.Condition); o.Write(b.OwnerHouseholdId);
                    WriteArr(o, b.Delivered); WriteArr(o, b.Incoming); WriteArr(o, b.Stock); WriteArr(o, b.Reserved);
                }
                WriteCommands(o, w.PendingCommands);
                WriteCommands(o, w.CommandHistory);
                o.Write(w.Log.Count);
                foreach (var l in w.Log) { o.Write(l.Tick); o.Write(l.Category); o.Write(l.Text); o.Write(l.EntityId); }
                o.Flush();
                return ms.ToArray();
            }
        }

        public static WorldState Deserialize(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            using (var i = new BinaryReader(ms))
            {
                ulong seed = i.ReadUInt64(); long tick = i.ReadInt64(); int nextId = i.ReadInt32();
                string contentVersion = i.ReadString(); int playerCiv = i.ReadInt32(); ulong rng = i.ReadUInt64();
                int width = i.ReadInt32(), height = i.ReadInt32();
                var m = new WorldMap(width, height);
                int n = m.CellCount;
                ReadExact(i, m.Elevation); ReadExact(i, m.Moisture);
                for (int k = 0; k < n; k++) m.Biomes[k] = (Biome)i.ReadByte();
                for (int k = 0; k < n; k++) m.Resource[k] = (NaturalResource)i.ReadByte();
                for (int k = 0; k < n; k++) m.ResourceAmount[k] = i.ReadInt16();
                for (int k = 0; k < n; k++) m.Road[k] = i.ReadBoolean();

                var w = new WorldState(seed, m) { Tick = tick, NextId = nextId, ContentVersion = contentVersion, PlayerCivId = playerCiv };
                w.Rng.State = rng;
                for (int c = Count(i); c > 0; c--)
                    w.Civilizations.Add(new Civilization { Id = i.ReadInt32(), Name = i.ReadString(), ColorRgb = i.ReadUInt32(), CapitalSettlementId = i.ReadInt32() });
                for (int c = Count(i); c > 0; c--)
                    w.Settlements.Add(new Settlement { Id = i.ReadInt32(), CivId = i.ReadInt32(), Name = i.ReadString(), CenterCell = i.ReadInt32() });
                for (int c = Count(i); c > 0; c--)
                {
                    var h = new Household { Id = i.ReadInt32(), SettlementId = i.ReadInt32(), Name = i.ReadString(), HomeBuildingId = i.ReadInt32(), PendingHouseSiteId = i.ReadInt32() };
                    ReadList(i, h.MemberIds);
                    w.Households.Add(h);
                }
                for (int c = Count(i); c > 0; c--)
                {
                    var p = new Person
                    {
                        Id = i.ReadInt32(), HouseholdId = i.ReadInt32(), SettlementId = i.ReadInt32(), CivId = i.ReadInt32(), Name = i.ReadString(),
                        AgeDays = i.ReadInt32(), Health = i.ReadInt32(), Hunger = i.ReadInt32(), Cell = i.ReadInt32(), Task = (PersonTask)i.ReadByte(),
                        TargetCell = i.ReadInt32(), TargetBuildingId = i.ReadInt32(), TaskResource = (ResourceType)i.ReadByte(), CarryType = (ResourceType)i.ReadByte(),
                        CarryAmount = i.ReadInt32(), Cooldown = i.ReadInt32(), HaulSiteId = i.ReadInt32(), HaulAmount = i.ReadInt32(), MoveProgress = i.ReadInt32()
                    };
                    ReadList(i, p.Path);
                    p.PathPos = i.ReadInt32(); p.TotalGathered = i.ReadInt32();
                    w.People.Add(p);
                }
                for (int c = Count(i); c > 0; c--)
                {
                    var b = new Building
                    {
                        Id = i.ReadInt32(), DefId = i.ReadString(), CivId = i.ReadInt32(), SettlementId = i.ReadInt32(), Cell = i.ReadInt32(),
                        Complete = i.ReadBoolean(), LaborRemaining = i.ReadInt32(), Condition = i.ReadInt32(), OwnerHouseholdId = i.ReadInt32()
                    };
                    b.Delivered = ReadArr(i); b.Incoming = ReadArr(i); b.Stock = ReadArr(i); b.Reserved = ReadArr(i);
                    w.Buildings.Add(b);
                }
                ReadCommands(i, w.PendingCommands);
                ReadCommands(i, w.CommandHistory);
                for (int c = Count(i); c > 0; c--)
                    w.Log.Add(new LogEntry { Tick = i.ReadInt64(), Category = i.ReadString(), Text = i.ReadString(), EntityId = i.ReadInt32() });
                if (ms.Position != ms.Length) throw new SaveLoadException("Unerwartete Zusatzdaten am Ende des Spielstands.");
                w.RebuildDerived();
                return w;
            }
        }

        static void WriteList(BinaryWriter o, List<int> l) { o.Write(l.Count); foreach (var v in l) o.Write(v); }
        static void ReadList(BinaryReader i, List<int> l) { for (int c = Count(i); c > 0; c--) l.Add(i.ReadInt32()); }
        static void WriteArr(BinaryWriter o, int[] a) { o.Write(a.Length); foreach (var v in a) o.Write(v); }

        static int[] ReadArr(BinaryReader i)
        {
            int len = Count(i);
            if (len != Res.Count) throw new SaveLoadException($"Ressourcenvektor mit {len} statt {Res.Count} Einträgen.");
            var a = new int[len];
            for (int k = 0; k < len; k++) a[k] = i.ReadInt32();
            return a;
        }

        static int Count(BinaryReader i)
        {
            int c = i.ReadInt32();
            if (c < 0 || c > 10_000_000) throw new SaveLoadException("Ungültige Listenlänge " + c);
            return c;
        }

        static void ReadExact(BinaryReader i, byte[] target)
        {
            if (i.Read(target, 0, target.Length) != target.Length) throw new SaveLoadException("Kartendaten unvollständig.");
        }

        static void WriteCommands(BinaryWriter o, List<GameCommand> list)
        {
            o.Write(list.Count);
            foreach (var c in list)
            {
                o.Write((byte)c.Type); o.Write(c.Tick); o.Write(c.CivId); o.Write(c.DefId); o.Write(c.CellA); o.Write(c.CellB);
                o.Write(c.Succeeded); o.Write(c.Result);
            }
        }

        static void ReadCommands(BinaryReader i, List<GameCommand> list)
        {
            for (int n = Count(i); n > 0; n--)
                list.Add(new GameCommand
                {
                    Type = (CommandType)i.ReadByte(), Tick = i.ReadInt64(), CivId = i.ReadInt32(), DefId = i.ReadString(),
                    CellA = i.ReadInt32(), CellB = i.ReadInt32(), Succeeded = i.ReadBoolean(), Result = i.ReadString()
                });
        }
    }
}
