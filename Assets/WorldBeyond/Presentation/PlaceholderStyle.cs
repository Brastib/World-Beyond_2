using System.Collections.Generic;
using UnityEngine;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Presentation
{
    public enum PlaceholderShape { Kugel, Quader, Flaeche }

    public sealed class PlaceholderSpec
    {
        public PlaceholderShape Shape;
        public Vector3 Scale;
        public string Label;
        public PlaceholderSpec(PlaceholderShape s, Vector3 scale, string label) { Shape = s; Scale = scale; Label = label; }
    }

    /// <summary>
    /// Zentrale, austauschbare Darstellungsvorschrift für alle Platzhalter (siehe Docs/ASSET_REGISTRY.md).
    /// Gameplay-Code kennt nur Kategorien/Def-Ids, nie Dateinamen finaler Assets.
    /// </summary>
    public static class PlaceholderStyle
    {
        public static readonly PlaceholderSpec Person = new PlaceholderSpec(PlaceholderShape.Kugel, new Vector3(0.45f, 0.45f, 0.45f), "Person");
        static readonly Dictionary<string, PlaceholderSpec> Buildings = new Dictionary<string, PlaceholderSpec>
        {
            { "ortszentrum", new PlaceholderSpec(PlaceholderShape.Quader, new Vector3(0.95f, 1.6f, 0.95f), "Ortszentrum") },
            { "haus", new PlaceholderSpec(PlaceholderShape.Quader, new Vector3(0.75f, 0.7f, 0.75f), "Haus") },
            { "lager", new PlaceholderSpec(PlaceholderShape.Quader, new Vector3(0.95f, 0.5f, 0.95f), "Lager") },
        };
        static readonly PlaceholderSpec UnknownBuilding = new PlaceholderSpec(PlaceholderShape.Quader, Vector3.one * 0.6f, "Unbekannt");

        public static PlaceholderSpec Building(string defId) => Buildings.TryGetValue(defId, out var s) ? s : UnknownBuilding;

        public static Color Civ(uint rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        public static readonly Color ConstructionSite = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color Selection = new Color(1f, 0.95f, 0.2f);
        public static readonly Color ValidPlacement = new Color(0.2f, 0.9f, 0.3f);
        public static readonly Color InvalidPlacement = new Color(0.95f, 0.2f, 0.2f);

        public static Color Resource(ResourceType r)
        {
            switch (r)
            {
                case ResourceType.Nahrung: return new Color(0.95f, 0.45f, 0.65f);
                case ResourceType.Holz: return new Color(0.55f, 0.35f, 0.15f);
                default: return new Color(0.6f, 0.6f, 0.65f);
            }
        }

        public static Color32 BiomeColor(Biome b, byte elevation)
        {
            float shade = 0.8f + elevation / 255f * 0.4f;
            Color c;
            switch (b)
            {
                case Simulation.Core.Biome.Wasser: c = new Color(0.18f, 0.38f, 0.72f); shade = 0.7f + elevation / 255f * 0.6f; break;
                case Simulation.Core.Biome.Wiese: c = new Color(0.45f, 0.70f, 0.32f); break;
                case Simulation.Core.Biome.Wald: c = new Color(0.20f, 0.48f, 0.22f); break;
                case Simulation.Core.Biome.Huegel: c = new Color(0.58f, 0.55f, 0.42f); break;
                default: c = new Color(0.78f, 0.78f, 0.80f); break;
            }
            return new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
        }

        public static readonly Color32 Road = new Color32(150, 110, 70, 255);
        public static readonly Color32 Berries = new Color32(230, 90, 160, 255);
        public static readonly Color32 Trees = new Color32(25, 80, 30, 255);
        public static readonly Color32 Rocks = new Color32(120, 115, 110, 255);
    }

    /// <summary>Materialquelle. Bevorzugt URP-Shader; meldet sichtbar, wenn auf Built-in zurückgefallen wird.</summary>
    public static class PlaceholderMaterials
    {
        static readonly Dictionary<Color, Material> Lit = new Dictionary<Color, Material>();
        static Shader _lit, _unlit;
        public static string ShaderInfo = "";

        static Shader Find(params string[] names)
        {
            foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
            Debug.LogError("[WorldBeyond] Kein Shader gefunden: " + string.Join(", ", names));
            return null;
        }

        public static Material ForColor(Color c)
        {
            if (Lit.TryGetValue(c, out var m)) return m;
            if (_lit == null)
            {
                // Vorlagen-Materialien aus Resources (von "World Beyond/Projekt einrichten" erzeugt) sorgen dafür, dass Shader im Build enthalten sind.
                var tpl = Resources.Load<Material>("WB_Lit");
                _lit = tpl != null ? tpl.shader : Find("Universal Render Pipeline/Lit", "Standard");
                if (tpl == null) Debug.LogWarning("[WorldBeyond] Resources/WB_Lit fehlt – Projekt-Einrichtung ausführen.");
                ShaderInfo = "Objekte: " + (_lit != null ? _lit.name : "FEHLT");
                if (_lit != null && _lit.name == "Standard") Debug.LogWarning("[WorldBeyond] URP-Lit nicht verfügbar, verwende Built-in Standard.");
            }
            m = new Material(_lit) { color = c };
            Lit[c] = m;
            return m;
        }

        public static Material ForTexture(Texture tex)
        {
            if (_unlit == null)
            {
                var tpl = Resources.Load<Material>("WB_Unlit");
                _unlit = tpl != null ? tpl.shader : Find("Universal Render Pipeline/Unlit", "Unlit/Texture");
            }
            return new Material(_unlit) { mainTexture = tex };
        }
    }
}
