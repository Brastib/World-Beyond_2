using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Presentation
{
    /// <summary>
    /// Platzhalter-HUD mit IMGUI (ADR-008): Zeit, Ressourcen, Werkzeuge, Speicherplätze, Inspektor, Meldungen, Debug.
    /// Referenzauflösung 1920×1080, skalierbar. Wird im UI-Pass durch UI Toolkit ersetzt.
    /// </summary>
    public sealed class HudGui : MonoBehaviour
    {
        public static bool PointerOverUi;
        static readonly string[] Slots = { "slot1", "slot2", "slot3" };
        GameBootstrap _g;
        float _userScale = 1f;
        bool _help;
        string _seedText = "12345";
        string _hash = "";
        float _fps;
        readonly List<Rect> _rects = new List<Rect>();
        GUIStyle _box, _label, _small, _title;

        void Awake() { _g = GetComponent<GameBootstrap>(); }

        void Update()
        {
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-4f), 0.05f);
            var mp = InputAdapter.MousePosition;
            float scale = Scale;
            var gp = new Vector2(mp.x / scale, (Screen.height - mp.y) / scale);
            PointerOverUi = _rects.Any(r => r.Contains(gp));
        }

        float Scale => Screen.height / 1080f * _userScale;

        void Styles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(8, 8, 6, 6) };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            _small = new GUIStyle(_label) { fontSize = 13 };
            _title = new GUIStyle(_label) { fontSize = 17, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (_g == null || _g.Session == null) return;
            Styles();
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            _rects.Clear();
            float w = Screen.width / Scale, h = Screen.height / Scale;
            TopBar(w);
            LeftPanel();
            Inspector(w);
            Messages(h);
            if (_g.ShowDebug) DebugOverlay(w, h);
            if (_g.ShowStats) Stats(w);
            if (_help) Help(w, h);
            if (_g.HoverReason != null && !PointerOverUi)
            {
                var mp = InputAdapter.MousePosition;
                GUI.Label(new Rect(mp.x / Scale + 16, (Screen.height - mp.y) / Scale + 8, 360, 60), "<color=#ff8080>" + _g.HoverReason + "</color>", _label);
            }
        }

        Rect Panel(Rect r) { _rects.Add(r); GUI.Box(r, GUIContent.none, _box); return r; }

        void TopBar(float width)
        {
            var s = _g.Session; var world = s.World;
            var r = Panel(new Rect(0, 0, width, 40));
            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 6, r.width - 16, 30));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{Calendar.Format(world.Tick)}</b>", _label, GUILayout.Width(380));
            if (GUILayout.Button(s.Paused ? "▶ Weiter" : "❚❚ Pause", GUILayout.Width(90))) s.Paused = !s.Paused;
            for (int i = 0; i < GameSession.Speeds.Length; i++)
            {
                bool on = !s.Paused && s.SpeedIndex == i;
                if (GUILayout.Toggle(on, GameSession.Speeds[i] + "×", "Button", GUILayout.Width(44)) && !on) s.SetSpeed(i);
            }
            GUILayout.Space(20);
            var civ = world.PlayerCivId != 0 ? world.Civ(world.PlayerCivId) : null;
            if (civ != null)
            {
                var stock = Economy.SettlementStock(world, s.Content, civ.CapitalSettlementId);
                foreach (var res in Res.All)
                {
                    var c = PlaceholderStyle.Resource(res);
                    GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>■</color> {Res.Name(res)} {stock[(int)res]}", _label, GUILayout.Width(120));
                }
            }
            GUILayout.FlexibleSpace();
            string who = civ != null ? "Du spielst: " + civ.Name : "Beobachter";
            if (GUILayout.Button(new GUIContent(who + " ▸", "Wechselt die gespielte Zivilisation (nach der letzten: Beobachter)."), GUILayout.Width(200))) _g.CyclePlayerCiv();
            if (GUILayout.Button(_g.ShowStats ? "Statistik ✕" : "Statistik (T)", GUILayout.Width(110))) _g.ShowStats = !_g.ShowStats;
            if (GUILayout.Button(_help ? "Hilfe ✕" : "Hilfe", GUILayout.Width(70))) _help = !_help;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void LeftPanel()
        {
            var s = _g.Session;
            var r = Panel(new Rect(8, 48, 250, 540));
            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12));
            GUILayout.Label("Bauen", _title);
            var lager = s.Content.Building(DefaultContent.Storage);
            if (GUILayout.Toggle(_g.Tool == ToolMode.LagerBauen, $"Lager bauen (B)\n{lager.Cost[1]} Holz, {lager.Cost[2]} Stein, {lager.Labor} Std. Arbeit", "Button"))
            { if (_g.Tool != ToolMode.LagerBauen) _g.SetTool(ToolMode.LagerBauen); }
            else if (_g.Tool == ToolMode.LagerBauen) _g.SetTool(ToolMode.Auswahl);
            if (GUILayout.Toggle(_g.Tool == ToolMode.StrasseBauen, $"Straße (R)\n{BuildRules.RoadStoneCostPerCell} Stein + {BuildRules.RoadLaborPerCell} Std. Arbeit pro Feld", "Button"))
            { if (_g.Tool != ToolMode.StrasseBauen) _g.SetTool(ToolMode.StrasseBauen); }
            else if (_g.Tool == ToolMode.StrasseBauen) _g.SetTool(ToolMode.Auswahl);
            var feld = s.Content.Building(DefaultContent.Field);
            if (GUILayout.Toggle(_g.Tool == ToolMode.FeldBauen, $"Feld anlegen (F)\n{feld.Cost[1]} Holz, {feld.Labor} Std. Arbeit, Ernte ab Herbst", "Button"))
            { if (_g.Tool != ToolMode.FeldBauen) _g.SetTool(ToolMode.FeldBauen); }
            else if (_g.Tool == ToolMode.FeldBauen) _g.SetTool(ToolMode.Auswahl);
            if (_g.Tool != ToolMode.Auswahl) GUILayout.Label("<i>Linksklick auf Karte, Esc bricht ab.</i>", _small);

            GUILayout.Space(8);
            GUILayout.Label("Kartenebene (L)", _title);
            GUILayout.BeginHorizontal();
            foreach (MapLayer layer in System.Enum.GetValues(typeof(MapLayer)))
                if (GUILayout.Toggle(_g.Map.Layer == layer, layer.ToString(), "Button") && _g.Map.Layer != layer)
                { _g.Map.Layer = layer; _g.Map.Refresh(s.World.Map); }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Spielstände", _title);
            foreach (var slot in Slots)
            {
                var info = s.Saves.Info(slot);
                string desc = info.Error != null ? "<color=#ff8080>beschädigt</color>" :
                              info.Header == null ? "leer" : $"{Calendar.Format(info.Header.Tick).Split('(')[0]}";
                GUILayout.Label($"{slot}: {desc}", _small);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Speichern")) s.Save(slot);
                GUI.enabled = info.Header != null;
                if (GUILayout.Button("Laden")) s.Load(slot);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("<i>F5 Schnellspeichern, F9 Schnellladen</i>", _small);

            GUILayout.Space(8);
            GUILayout.Label("Neue Welt", _title);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", _small, GUILayout.Width(40));
            _seedText = GUILayout.TextField(_seedText, 18);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Starten"))
            {
                if (ulong.TryParse(_seedText, out var seed)) TryNew(seed);
                else s.Message = "Seed muss eine ganze Zahl ≥ 0 sein.";
            }
            if (GUILayout.Button("Zufällig")) { var seed = (ulong)Random.Range(1, int.MaxValue); _seedText = seed.ToString(); TryNew(seed); }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI-Größe", _small, GUILayout.Width(70));
            if (GUILayout.Button("−")) _userScale = Mathf.Max(0.6f, _userScale - 0.1f);
            if (GUILayout.Button("+")) _userScale = Mathf.Min(1.8f, _userScale + 0.1f);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void TryNew(ulong seed)
        {
            try { _g.Session.NewWorld(seed); }
            catch (System.InvalidOperationException ex) { _g.Session.Message = ex.Message; }
        }

        void Inspector(float width)
        {
            var w = _g.Session.World; var content = _g.Session.Content;
            var sb = new StringBuilder();
            switch (_g.SelKind)
            {
                case SelectionKind.Person:
                {
                    var p = w.PersonById(_g.SelId);
                    if (p == null) { sb.Append("Person existiert nicht mehr."); break; }
                    sb.AppendLine($"<b>Person {p.Name}</b> #{p.Id}");
                    sb.AppendLine($"Alter: {p.AgeDays / Calendar.DaysPerYear} Jahre · Beruf: <b>{Jobs.RoleName(p.Role)}</b>");
                    sb.AppendLine($"Gesundheit {p.Health} · Hunger {p.Hunger}");
                    sb.AppendLine($"Aufgabe: <b>{p.Task}</b>{TaskDetail(w, content, p)}");
                    sb.AppendLine($"Trägt: {(p.CarryAmount > 0 ? p.CarryAmount + " " + p.CarryType : "nichts")} · gesammelt gesamt {p.TotalGathered}");
                    sb.AppendLine($"Ort: {w.Map.X(p.Cell)},{w.Map.Y(p.Cell)}");
                    var h = w.HouseholdById(p.HouseholdId);
                    sb.AppendLine();
                    sb.AppendLine($"<b>{h.Name}</b> #{h.Id}");
                    sb.AppendLine("Mitglieder: " + string.Join(", ", h.MemberIds.Select(id => w.PersonById(id).Name)));
                    sb.AppendLine("Wohnort: " + (h.HomeBuildingId != 0 ? "Haus #" + h.HomeBuildingId :
                        h.PendingHouseSiteId != 0 ? "Ortszentrum (Haus #" + h.PendingHouseSiteId + " im Bau)" : "Ortszentrum (wartet auf 20 freies Holz)"));
                    AppendSettlement(sb, w, content, p.SettlementId);
                    break;
                }
                case SelectionKind.Gebaeude:
                {
                    var b = w.BuildingById(_g.SelId);
                    if (b == null) { sb.Append("Gebäude existiert nicht mehr."); break; }
                    var def = content.Building(b.DefId);
                    sb.AppendLine($"<b>{def.Name}</b> #{b.Id} {(b.Complete ? "" : "(Baustelle)")}");
                    sb.AppendLine(def.Description);
                    if (!b.Complete)
                    {
                        sb.AppendLine("Material geliefert: " + Vec(b.Delivered) + " von " + Vec(def.Cost));
                        sb.AppendLine("Unterwegs: " + Vec(b.Incoming));
                        sb.AppendLine($"Arbeit offen: {b.LaborRemaining} Std.");
                        sb.AppendLine(Construction.MaterialsComplete(b, def) ? "→ wartet auf Bauarbeiter" : "→ wartet auf Material aus dem Lager");
                    }
                    if (def.FieldCapacity > 0 && b.Complete)
                    {
                        int allowed = System.Math.Min(def.FieldCapacity, PersonSystem.AllowedGrowth(w, def));
                        int workers = w.People.Count(p => p.Task == PersonTask.Feldarbeit && p.TargetBuildingId == b.Id);
                        sb.AppendLine($"Wachstum {b.FieldGrowth}/{def.FieldCapacity} → Ertrag {b.FieldGrowth / 2} Nahrung · Bauern am Feld {workers}/{def.Workers}");
                        sb.AppendLine(PersonSystem.IsGrowingSeason(w)
                            ? (b.FieldGrowth < allowed ? "Wächst – braucht Bauern." : "Wächst planmäßig (Soll erreicht).")
                            : (b.FieldGrowth >= 2 ? "Erntezeit: Bauern bringen Nahrung ins Lager." : "Abgeerntet, ruht bis zum Frühling."));
                    }
                    if (def.StorageCapacity > 0)
                        sb.AppendLine($"Lager: {Vec(b.Stock)} ({Res.Sum(b.Stock)}/{def.StorageCapacity}), reserviert {Vec(b.Reserved)}");
                    if (b.OwnerHouseholdId != 0) sb.AppendLine("Besitzer: " + w.HouseholdById(b.OwnerHouseholdId).Name);
                    sb.AppendLine($"Zustand: {b.Condition}%");
                    AppendSettlement(sb, w, content, b.SettlementId);
                    break;
                }
                case SelectionKind.Zelle:
                {
                    var m = w.Map; int c = _g.SelId;
                    sb.AppendLine($"<b>Zelle {m.X(c)},{m.Y(c)}</b> (Chunk {m.X(c) / WorldMap.ChunkSize},{m.Y(c) / WorldMap.ChunkSize})");
                    sb.AppendLine($"Biom: {m.Biomes[c]} · Höhe {m.Elevation[c]} · Feuchte {m.Moisture[c]}");
                    sb.AppendLine($"Ressource: {m.Resource[c]} {(m.ResourceAmount[c] > 0 ? m.ResourceAmount[c].ToString() : "")}");
                    sb.AppendLine($"Straße: {(m.Road[c] ? "ja" : m.RoadWork[c] > 0 ? "geplant, noch " + m.RoadWork[c] + " Std. Arbeit" : "nein")} · Bewegungskosten: {(m.IsWalkable(c) ? m.MoveCost(c).ToString() : "unbegehbar")}");
                    string reason = w.PlayerCivId != 0 ? BuildRules.CheckBuildingCell(w, w.PlayerCivId, c) : "Beobachtermodus";
                    sb.AppendLine("Bebaubar: " + (reason == null ? "ja" : "nein – " + reason));
                    break;
                }
                default:
                    sb.AppendLine("<b>Inspektor</b>");
                    sb.AppendLine("Klicke eine Person (Kugel), ein Gebäude (Quader) oder eine Zelle an.");
                    if (w.PlayerCivId != 0) AppendSettlement(sb, w, content, w.Civ(w.PlayerCivId).CapitalSettlementId);
                    break;
            }
            var r = Panel(new Rect(width - 408, 48, 400, 470));
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12), sb.ToString(), _small);
        }

        static string Vec(int[] v) => $"N {v[0]} · H {v[1]} · S {v[2]}";

        static string TaskDetail(WorldState w, ContentCatalog c, Person p)
        {
            switch (p.Task)
            {
                case PersonTask.Sammeln: return $" ({p.TaskResource} bei {w.Map.X(p.TargetCell)},{w.Map.Y(p.TargetCell)})";
                case PersonTask.Abliefern: return $" (zu Lager #{p.TargetBuildingId})";
                case PersonTask.MaterialHolen: return $" ({p.HaulAmount} {p.TaskResource} aus #{p.TargetBuildingId} für Baustelle #{p.HaulSiteId})";
                case PersonTask.MaterialBringen: return $" (zu Baustelle #{p.TargetBuildingId})";
                case PersonTask.Bauen: return $" (an #{p.TargetBuildingId})";
                case PersonTask.StrasseBauen: return $" (Straßenfeld {w.Map.X(p.TargetCell)},{w.Map.Y(p.TargetCell)})";
                case PersonTask.Feldarbeit: return $" (Feld #{p.TargetBuildingId}, {(PersonSystem.IsGrowingSeason(w) ? "bestellen" : "ernten")})";
                default: return p.Cooldown > 0 ? $" (wartet {p.Cooldown} Std.)" : "";
            }
        }

        static void AppendSettlement(StringBuilder sb, WorldState w, ContentCatalog content, int sid)
        {
            var s = w.SettlementById(sid); var civ = w.Civ(s.CivId);
            int pop = w.People.Count(p => p.SettlementId == sid);
            int houses = w.Buildings.Count(b => b.SettlementId == sid && b.Complete && b.DefId == DefaultContent.House);
            int sites = w.Buildings.Count(b => b.SettlementId == sid && !b.Complete);
            int cap = Economy.Storages(w, content, sid).Sum(b => content.Building(b.DefId).StorageCapacity);
            var stock = Economy.SettlementStock(w, content, sid);
            sb.AppendLine();
            sb.AppendLine($"<b>Siedlung {s.Name}</b> · Nation <b>{civ.Name}</b>");
            sb.AppendLine($"Einwohner {pop} · Häuser {houses} · Baustellen {sites} · Geburten {s.Births} · Tode {s.Deaths}");
            if (!string.IsNullOrEmpty(s.Status)) sb.AppendLine("<b>Lage:</b> " + s.Status);
            sb.AppendLine($"Lager: {Vec(stock)} ({Res.Sum(stock)}/{cap})");
            if (Res.Sum(stock) >= cap - 5) sb.AppendLine("<color=#ffb060>Lager voll: Sammeln stockt. Baue ein Lager (B).</color>");
        }

        void Stats(float width)
        {
            var w = _g.Session.World; var content = _g.Session.Content;
            var r = Panel(new Rect(266, 48, Mathf.Min(900, width - 680), 60 + 120 * w.Settlements.Count));
            var sb = new StringBuilder("<b>Statistik</b> (T schließt)\n");
            foreach (var s in w.Settlements)
            {
                var civ = w.Civ(s.CivId);
                var people = w.People.Where(p => p.SettlementId == s.Id).ToList();
                var hh = w.Households.Where(h => h.SettlementId == s.Id).ToList();
                int houses = w.Buildings.Count(b => b.SettlementId == s.Id && b.Complete && b.DefId == DefaultContent.House);
                int storages = w.Buildings.Count(b => b.SettlementId == s.Id && b.Complete && b.DefId == DefaultContent.Storage);
                var fields = w.Buildings.Where(b => b.SettlementId == s.Id && b.Complete && b.DefId == DefaultContent.Field).ToList();
                int roadWork = PersonSystem.OpenRoadWork(w.Map, s);
                var stock = Economy.SettlementStock(w, content, s.Id);
                var roles = string.Join(", ", System.Enum.GetValues(typeof(Role)).Cast<Role>()
                    .Select(ro => new { ro, n = people.Count(p => p.Role == ro) }).Where(x => x.n > 0).Select(x => $"{Jobs.RoleName(x.ro)} {x.n}"));
                sb.AppendLine();
                sb.AppendLine($"<b><color=#{ColorUtility.ToHtmlStringRGB(PlaceholderStyle.Civ(civ.ColorRgb))}>■</color> {s.Name}</b> ({civ.Name}{(civ.Id == w.PlayerCivId ? ", du" : ", KI")})");
                sb.AppendLine($"Einwohner {people.Count} (Kinder {people.Count(p => !p.IsAdult)}) · Haushalte {hh.Count} (ohne Haus {hh.Count(h => h.HomeBuildingId == 0)}) · Häuser {houses} · Lagerorte {storages + 1} (inkl. Ortszentrum)");
                sb.AppendLine($"Bestand: {Vec(stock)} · Geburten {s.Births} · Tode {s.Deaths}");
                sb.AppendLine("Berufe: " + roles);
                sb.AppendLine($"Felder {fields.Count} (erwarteter Ertrag {fields.Sum(f => f.FieldGrowth) / 2} Nahrung) · offene Straßenfelder {roadWork}");
                sb.AppendLine("Lage: " + s.Status);
            }
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12), sb.ToString(), _small);
        }

        void Messages(float height)
        {
            var w = _g.Session.World;
            var r = Panel(new Rect(266, height - 150, 700, 142));
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_g.Session.Message)) sb.AppendLine("<b>" + _g.Session.Message + "</b>");
            for (int i = w.Log.Count - 1, n = 0; i >= 0 && n < 6; i--, n++)
                sb.AppendLine($"<color=#bbbbbb>T{Calendar.Day(w.Log[i].Tick) + 1}</color> [{w.Log[i].Category}] {w.Log[i].Text}");
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12), sb.ToString(), _small);
        }

        void DebugOverlay(float width, float height)
        {
            var s = _g.Session; var w = s.World;
            var r = Panel(new Rect(width - 408, height - 190, 400, 182));
            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12));
            GUILayout.Label($"<b>Debug (F3)</b> · FPS {_fps:0} · Tick {w.Tick}", _small);
            GUILayout.Label($"Tickdauer letzte {s.LastTickMs:0.000} ms · Mittel {s.AvgTickMs:0.000} ms", _small);
            GUILayout.Label($"Entitäten: {w.People.Count} Personen, {w.Households.Count} Haushalte, {w.Buildings.Count} Gebäude · Chunks {_g.Map.ChunkCount}", _small);
            GUILayout.Label($"Seed {w.Seed} · Befehle {w.CommandHistory.Count} · {PlaceholderMaterials.ShaderInfo}", _small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Zustands-Hash")) _hash = s.StateHash().Substring(0, 16);
            if (GUILayout.Button("Screenshot (F12)")) _g.TakeScreenshot();
            GUILayout.EndHorizontal();
            if (_hash.Length > 0) GUILayout.Label("Hash: " + _hash, _small);
            GUILayout.EndArea();
        }

        void Help(float width, float height)
        {
            var r = Panel(new Rect(width / 2 - 260, 60, 520, 330));
            GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16),
                "<b>Steuerung</b>\n" +
                "WASD/Pfeile: Kamera schwenken · Q/E oder rechte Maustaste: drehen · Mausrad: zoomen · mittlere Maustaste: ziehen\n" +
                "Linksklick: auswählen · B: Lager bauen · F: Feld anlegen · R: Straße (Start, dann Ende klicken) · Esc: abbrechen\n" +
                "Leertaste: Pause · 1–4: Tempo 1×/2×/4×/8× · L: Kartenebene · T: Statistik · F5/F9: Schnell speichern/laden · F3: Debug · F12: Screenshot\n\n" +
                "<b>Platzhalter</b>\nKugel = Person (Farbe = Nation, kleine Kugel = Kind), kleiner Würfel darüber = Traglast (rosa Nahrung, braun Holz, grau Stein)\n" +
                "Quader = Gebäude (hoch: Ortszentrum, mittel: Haus, flach: Lager), grau und flach = Baustelle\n" +
                "Flache Platte = Feld (braun leer → gold reif)\nRosa Felder = Beeren, dunkelgrün = Bäume, grau = Felsen, braun = Straße, hell kariert = geplante Straße", _small);
        }
    }
}
