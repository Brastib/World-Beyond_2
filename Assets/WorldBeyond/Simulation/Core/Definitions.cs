using System;
using System.Collections.Generic;

namespace WorldBeyond.Simulation.Core
{
    public enum BuildingCategory : byte { Wohnen, Lager, Verwaltung, Nahrung, Rohstoff, Werkstatt, Kultur, Militaer, Infrastruktur }

    /// <summary>Statische Gebäudedefinition. Laufzeitzustand liegt in <see cref="Building"/>.</summary>
    public sealed class BuildingDef
    {
        public string Id = "";
        public string Name = "";
        public BuildingCategory Category;
        public int[] Cost = Res.Empty();
        public int Labor;
        public int StorageCapacity;
        public int HousingCapacity;
        public bool PlayerBuildable;
        /// <summary>&gt; 0 = Feld mit diesem maximalen Wachstum; <see cref="Workers"/> Bauern können gleichzeitig arbeiten.</summary>
        public int FieldCapacity;
        public int Workers;
        /// <summary>Werkstatt-Rezept (null = keins): Eingang aus dem Lager, Arbeitsstunden, Ausgang.</summary>
        public RecipeDef Recipe;
        public string Description = "";
    }

    /// <summary>Produktionsschritt: <see cref="Input"/> wird verbraucht, nach <see cref="Labor"/> Arbeitsstunden entsteht <see cref="OutputAmount"/> × <see cref="Output"/>.</summary>
    public sealed class RecipeDef
    {
        public int[] Input = Res.Empty();
        public int Labor;
        public ResourceType Output;
        public int OutputAmount = 1;
    }

    public sealed class EpochDef
    {
        public string Id = "";
        public string Name = "";
        public int Order;
        /// <summary>Nur aktivierte Epochen sind spielbar. M0: nur Frühzeit.</summary>
        public bool Enabled;
    }

    public sealed class ContentCatalog
    {
        public string Version = "";
        public readonly List<BuildingDef> Buildings = new List<BuildingDef>();
        public readonly List<EpochDef> Epochs = new List<EpochDef>();

        public BuildingDef Building(string id)
        {
            for (int i = 0; i < Buildings.Count; i++) if (Buildings[i].Id == id) return Buildings[i];
            throw new KeyNotFoundException("Unbekannte Gebäudedefinition: " + id);
        }

        public bool HasBuilding(string id)
        {
            for (int i = 0; i < Buildings.Count; i++) if (Buildings[i].Id == id) return true;
            return false;
        }

        /// <summary>Liefert Fehlerliste; leer = gültig.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>();
            foreach (var b in Buildings)
            {
                if (string.IsNullOrEmpty(b.Id)) errors.Add("Gebäude ohne Id");
                else if (!seen.Add(b.Id)) errors.Add("Doppelte Gebäude-Id " + b.Id);
                if (b.Cost == null || b.Cost.Length != Res.Count) errors.Add(b.Id + ": Kostenvektor ungültig");
                else foreach (var c in b.Cost) if (c < 0) errors.Add(b.Id + ": negative Kosten");
                if (b.Labor < 0 || b.StorageCapacity < 0 || b.HousingCapacity < 0) errors.Add(b.Id + ": negative Werte");
                if (b.Recipe != null)
                {
                    if (b.Recipe.Input == null || b.Recipe.Input.Length != Res.Count) errors.Add(b.Id + ": Rezept-Eingang ungültig");
                    else foreach (var c in b.Recipe.Input) if (c < 0) errors.Add(b.Id + ": negativer Rezept-Eingang");
                    if (b.Recipe.Labor <= 0 || b.Recipe.OutputAmount <= 0 || b.Workers <= 0) errors.Add(b.Id + ": Rezept braucht Arbeit, Ertrag und Arbeitsplätze");
                }
            }
            var orders = new HashSet<int>();
            foreach (var e in Epochs) if (!orders.Add(e.Order)) errors.Add("Doppelte Epochen-Reihenfolge " + e.Order);
            return errors;
        }
    }
}
