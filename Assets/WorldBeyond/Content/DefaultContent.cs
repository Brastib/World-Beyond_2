using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Content
{
    /// <summary>
    /// Statische M0-Definitionen. Später durch JSON/ScriptableObject-Quellen ersetzbar (ADR-006);
    /// Gameplay-Code referenziert nur die Ids, nie Asset-Dateinamen.
    /// </summary>
    public static class DefaultContent
    {
        public const string Version = "m0-content-1";
        public const string TownCenter = "ortszentrum";
        public const string House = "haus";
        public const string Storage = "lager";
        public const string Field = "feld";
        public const string Workshop = "werkstatt";

        public static ContentCatalog Create()
        {
            var c = new ContentCatalog { Version = Version };
            c.Buildings.Add(new BuildingDef
            {
                Id = TownCenter, Name = "Ortszentrum", Category = BuildingCategory.Verwaltung,
                Cost = new[] { 0, 0, 0, 0 }, Labor = 0, StorageCapacity = 300, HousingCapacity = 0,
                PlayerBuildable = false, Description = "Startgebäude jeder Siedlung. Lagert Waren; Haushalte ohne Haus wohnen hier."
            });
            c.Buildings.Add(new BuildingDef
            {
                Id = House, Name = "Haus", Category = BuildingCategory.Wohnen,
                Cost = new[] { 0, 20, 0, 0 }, Labor = 24, StorageCapacity = 0, HousingCapacity = 4,
                PlayerBuildable = false, Description = "Wird von Haushalten selbst beauftragt, wenn genug Holz da ist."
            });
            c.Buildings.Add(new BuildingDef
            {
                Id = Storage, Name = "Lager", Category = BuildingCategory.Lager,
                Cost = new[] { 0, 15, 5, 0 }, Labor = 16, StorageCapacity = 200, HousingCapacity = 0,
                PlayerBuildable = true, Description = "Öffentliches Lager. Sammler liefern an das nächste Lager mit freiem Platz."
            });

            c.Buildings.Add(new BuildingDef
            {
                Id = Field, Name = "Feld", Category = BuildingCategory.Nahrung,
                Cost = new[] { 0, 10, 0, 0 }, Labor = 12, StorageCapacity = 0, HousingCapacity = 0,
                PlayerBuildable = true, FieldCapacity = 600, Workers = 2,
                Description = "Bauern bestellen es im Frühling und Sommer und ernten ab dem Herbst (2 Wachstum = 1 Nahrung)."
            });

            c.Buildings.Add(new BuildingDef
            {
                Id = Workshop, Name = "Werkstatt", Category = BuildingCategory.Werkstatt,
                Cost = new[] { 0, 20, 10, 0 }, Labor = 20, StorageCapacity = 0, HousingCapacity = 0,
                PlayerBuildable = true, Workers = 1,
                Recipe = new RecipeDef { Input = new[] { 0, 2, 1, 0 }, Labor = 6, Output = ResourceType.Werkzeug, OutputAmount = 1 },
                Description = "Ein Handwerker holt 2 Holz + 1 Stein aus dem Lager und fertigt in 6 Stunden 1 Werkzeug. "
                            + "Holzfäller, Bergarbeiter und Baumeister nehmen Werkzeug im Lager mit und arbeiten damit doppelt so schnell (hält 50 Einsätze)."
            });

            string[] epochs = { "Frühzeit", "Antike", "Mittelalter", "Industrie", "Moderne", "Nahe Zukunft", "Raumfahrt", "Planeten", "Galaxie" };
            for (int i = 0; i < epochs.Length; i++)
                c.Epochs.Add(new EpochDef { Id = "epoche_" + i, Name = epochs[i], Order = i, Enabled = i == 0 });
            return c;
        }
    }
}
