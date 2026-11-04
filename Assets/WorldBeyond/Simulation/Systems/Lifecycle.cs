using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Abstrakter, jugendfreier Lebenszyklus (täglich): Tod durch Alter oder Hunger, Geburt in Haushalten mit Haus,
    /// Auszug eines Erwachsenen in einen neuen Haushalt. Alle Zufälle über den gespeicherten Simulationszufall.
    /// </summary>
    public static class Lifecycle
    {
        public const int OldAgeYears = 60;
        public const int BirthChancePerDay = 60;     // 1 zu 60
        public const int MoveOutChancePerDay = 30;   // 1 zu 30
        public const int BirthCooldownDays = 360;
        public const int FoodReservePerPerson = 3;
        public const int PartnershipChancePerDay = 20; // 1 zu 20

        public static void Run(WorldSimulation sim)
        {
            var w = sim.World;
            for (int i = w.People.Count - 1; i >= 0; i--)
            {
                var p = w.People[i];
                int years = p.AgeDays / Calendar.DaysPerYear;
                if (p.Health <= 0) RemovePerson(w, p, "ist verhungert");
                else if (years >= OldAgeYears && w.Rng.NextInt(1000) < years - OldAgeYears + 1) RemovePerson(w, p, "ist im Alter von " + years + " Jahren gestorben");
            }
            int capacity = sim.Content.Building(DefaultContent.House).HousingCapacity;
            int day = (int)Calendar.Day(w.Tick);
            int count = w.Households.Count;
            for (int k = 0; k < count; k++)
            {
                var h = w.Households[k];
                if (!FoodOk(sim, h.SettlementId) || Satisfaction.Average(w, h.SettlementId) < Satisfaction.BirthMinimum) continue;
                int adults = 0;
                foreach (var id in h.MemberIds) if (w.PersonById(id).IsAdult) adults++;
                if (h.HomeBuildingId != 0 && h.MemberIds.Count < capacity && adults >= 2 &&
                    (h.LastBirthDay < 0 || day - h.LastBirthDay >= BirthCooldownDays) && w.Rng.NextInt(BirthChancePerDay) == 0)
                    Birth(w, h, day);
                else if (h.MemberIds.Count >= capacity && adults >= 3 && w.Rng.NextInt(MoveOutChancePerDay) == 0)
                    MoveOut(w, h);
            }
            foreach (var s in w.Settlements) Partnership(w, s.Id);
        }

        /// <summary>Gibt es in der Siedlung einen Haushalt, der (sofort oder nach Wartezeit) wachsen kann? Für Lagetext-Prüfung.</summary>
        public static bool CanGrowSomehow(WorldState w, ContentCatalog content, int settlementId)
        {
            int capacity = content.Building(DefaultContent.House).HousingCapacity, singles = 0;
            foreach (var h in w.Households)
            {
                if (h.SettlementId != settlementId) continue;
                int adults = 0;
                foreach (var id in h.MemberIds) if (w.PersonById(id).IsAdult) adults++;
                if (IsSingle(w, h)) singles++;
                if (h.MemberIds.Count >= capacity ? adults >= 3 : h.HomeBuildingId != 0 && adults >= 2) return true;
            }
            return singles >= 2;
        }

        /// <summary>Alleinstehender Erwachsener ohne Kinder (Haushalt mit genau einem Mitglied).</summary>
        public static bool IsSingle(WorldState w, Household h) =>
            h.MemberIds.Count == 1 && w.PersonById(h.MemberIds[0]).IsAdult && h.PendingHouseSiteId == 0;

        /// <summary>
        /// Abstrakte Familienbildung: die zwei ältesten Haushalte von Alleinstehenden einer Siedlung (Id-Reihenfolge)
        /// werden mit 1/20 pro Tag zusammengelegt. Der Haushalt mit Haus bleibt bestehen, ein zweites Haus wird frei.
        /// </summary>
        static void Partnership(WorldState w, int settlementId)
        {
            Household a = null, b = null;
            foreach (var h in w.Households)
            {
                if (h.SettlementId != settlementId || !IsSingle(w, h)) continue;
                if (a == null) a = h; else { b = h; break; }
            }
            if (b == null || w.Rng.NextInt(PartnershipChancePerDay) != 0) return;
            if (a.HomeBuildingId == 0 && b.HomeBuildingId != 0) { var t = a; a = b; b = t; }
            var mover = w.PersonById(b.MemberIds[0]);
            var stay = w.PersonById(a.MemberIds[0]);
            b.MemberIds.Clear();
            a.MemberIds.Add(mover.Id);
            mover.HouseholdId = a.Id;
            foreach (var bd in w.Buildings) if (bd.OwnerHouseholdId == b.Id) bd.OwnerHouseholdId = 0;
            w.Households.Remove(b);
            w.AddLog("Bevölkerung", $"{stay.Name} und {mover.Name} gründen eine Familie ({a.Name})" +
                (b.HomeBuildingId != 0 ? $"; Haus #{b.HomeBuildingId} wird frei." : "."), a.Id);
        }

        static bool FoodOk(WorldSimulation sim, int settlementId)
        {
            int pop = 0;
            foreach (var p in sim.World.People) if (p.SettlementId == settlementId) pop++;
            return Economy.SettlementAvailable(sim.World, sim.Content, settlementId, ResourceType.Nahrung) >= pop * FoodReservePerPerson;
        }

        static void Birth(WorldState w, Household h, int day)
        {
            var parent = w.PersonById(h.MemberIds[0]);
            var home = w.BuildingById(h.HomeBuildingId);
            var child = new Person
            {
                Id = w.NewId(), HouseholdId = h.Id, SettlementId = h.SettlementId, CivId = parent.CivId,
                Name = ScenarioFactory.RandomName(w.Rng), AgeDays = 0, Cell = home.Cell, Role = Role.Kind
            };
            w.People.Add(child);
            h.MemberIds.Add(child.Id);
            h.LastBirthDay = day;
            w.SettlementById(h.SettlementId).Births++;
            w.AddLog("Bevölkerung", $"{h.Name}: {child.Name} wurde geboren.", child.Id);
        }

        static void MoveOut(WorldState w, Household h)
        {
            Person youngest = null;
            foreach (var id in h.MemberIds)
            {
                var p = w.PersonById(id);
                if (p.IsAdult && (youngest == null || p.AgeDays < youngest.AgeDays)) youngest = p;
            }
            var nh = new Household { Id = w.NewId(), SettlementId = h.SettlementId, Name = "Haushalt " + youngest.Name };
            w.Households.Add(nh);
            h.MemberIds.Remove(youngest.Id);
            nh.MemberIds.Add(youngest.Id);
            youngest.HouseholdId = nh.Id;
            w.AddLog("Bevölkerung", $"{youngest.Name} zieht aus {h.Name} aus und gründet einen eigenen Haushalt.", nh.Id);
        }

        /// <summary>Entfernt eine Person ohne kaputte Referenzen: Reservierungen werden gelöst, Traglast geht verloren (protokolliert).</summary>
        public static void RemovePerson(WorldState w, Person p, string reason)
        {
            if (p.Task == PersonTask.MaterialHolen)
            {
                var src = w.BuildingById(p.TargetBuildingId);
                var site = w.BuildingById(p.HaulSiteId);
                src.Reserved[(int)p.TaskResource] -= p.HaulAmount;
                site.Incoming[(int)p.TaskResource] -= p.HaulAmount;
            }
            else if (p.Task == PersonTask.MaterialBringen)
                w.BuildingById(p.TargetBuildingId).Incoming[(int)p.CarryType] -= p.CarryAmount;
            if (p.CarryAmount > 0) w.AddLog("Bevölkerung", $"{p.CarryAmount} {p.CarryType} von {p.Name} gingen verloren.");
            var h = w.HouseholdById(p.HouseholdId);
            h.MemberIds.Remove(p.Id);
            w.People.Remove(p);
            var s = w.SettlementById(p.SettlementId);
            s.Deaths++;
            w.AddLog("Bevölkerung", $"{p.Name} ({s.Name}) {reason}.");
            if (h.MemberIds.Count == 0)
            {
                foreach (var b in w.Buildings) if (b.OwnerHouseholdId == h.Id) b.OwnerHouseholdId = 0;
                w.Households.Remove(h);
                w.AddLog("Bevölkerung", $"{h.Name} ist erloschen; sein Haus steht leer.");
            }
        }
    }
}
