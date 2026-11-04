using System.Collections.Generic;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>Lagerregeln: Bestand, Reservierung, Kapazität. Keine Methode erzeugt oder vernichtet Waren stillschweigend.</summary>
    public static class Economy
    {
        public static int Available(Building b, ResourceType r) => b.Stock[(int)r] - b.Reserved[(int)r];

        public static int FreeCapacity(Building b, ContentCatalog content) =>
            b.Complete ? content.Building(b.DefId).StorageCapacity - Res.Sum(b.Stock) : 0;

        public static bool IsStorage(Building b, ContentCatalog content) =>
            b.Complete && content.Building(b.DefId).StorageCapacity > 0;

        public static IEnumerable<Building> Storages(WorldState w, ContentCatalog content, int settlementId)
        {
            foreach (var b in w.Buildings)
                if (b.SettlementId == settlementId && IsStorage(b, content)) yield return b;
        }

        public static int SettlementAvailable(WorldState w, ContentCatalog content, int settlementId, ResourceType r)
        {
            int sum = 0;
            foreach (var b in Storages(w, content, settlementId)) sum += Available(b, r);
            return sum;
        }

        public static int[] SettlementStock(WorldState w, ContentCatalog content, int settlementId)
        {
            var s = Res.Empty();
            foreach (var b in Storages(w, content, settlementId))
                for (int i = 0; i < Res.Count; i++) s[i] += b.Stock[i];
            return s;
        }

        /// <summary>Noch nicht gelieferter und nicht unterwegs befindlicher Materialbedarf aller offenen Baustellen.</summary>
        public static int OpenSiteDemand(WorldState w, ContentCatalog content, int settlementId, ResourceType r)
        {
            int sum = 0;
            foreach (var b in w.Buildings)
                if (b.SettlementId == settlementId && !b.Complete)
                    sum += content.Building(b.DefId).Cost[(int)r] - b.Delivered[(int)r] - b.Incoming[(int)r];
            return sum;
        }

        /// <summary>Entnimmt atomar: entweder die volle Menge (Lager in Id-Reihenfolge) oder nichts.</summary>
        public static bool TryTake(WorldState w, ContentCatalog content, int settlementId, ResourceType r, int amount)
        {
            if (amount < 0) return false;
            if (SettlementAvailable(w, content, settlementId, r) < amount) return false;
            foreach (var b in Storages(w, content, settlementId))
            {
                if (amount == 0) break;
                int take = System.Math.Min(amount, Available(b, r));
                b.Stock[(int)r] -= take;
                amount -= take;
            }
            return true;
        }

        public static Settlement NearestSettlementOfCiv(WorldState w, int civId, int cell)
        {
            Settlement best = null; int bestD = int.MaxValue;
            foreach (var s in w.Settlements)
            {
                if (s.CivId != civId) continue;
                int d = w.Map.Manhattan(s.CenterCell, cell);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }
    }
}
