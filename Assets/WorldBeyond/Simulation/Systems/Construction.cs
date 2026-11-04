using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    public static class Construction
    {
        public static Building PlaceSite(WorldState w, ContentCatalog content, string defId, Settlement s, int cell, int ownerHouseholdId)
        {
            var def = content.Building(defId);
            var b = new Building
            {
                Id = w.NewId(), DefId = defId, CivId = s.CivId, SettlementId = s.Id, Cell = cell,
                Complete = false, LaborRemaining = def.Labor, OwnerHouseholdId = ownerHouseholdId
            };
            w.Buildings.Add(b);
            w.Map.BuildingAt[cell] = b.Id;
            return b;
        }

        public static bool MaterialsComplete(Building b, BuildingDef def)
        {
            for (int r = 0; r < Res.Count; r++) if (b.Delivered[r] < def.Cost[r]) return false;
            return true;
        }

        public static void Finish(WorldState w, ContentCatalog content, Building b)
        {
            b.Complete = true; b.LaborRemaining = 0;
            var def = content.Building(b.DefId);
            var s = w.SettlementById(b.SettlementId);
            w.AddLog("Bau", $"{s.Name}: {def.Name} #{b.Id} fertiggestellt.", b.Id);
            if (b.OwnerHouseholdId != 0)
            {
                var h = w.HouseholdById(b.OwnerHouseholdId);
                h.HomeBuildingId = b.Id;
                h.PendingHouseSiteId = 0;
                w.AddLog("Haushalt", $"{h.Name} bezieht sein Haus #{b.Id}.", h.Id);
            }
        }
    }
}
