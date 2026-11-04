using System;
using UnityEngine;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Presentation
{
    public enum SelectionKind { Keine, Zelle, Person, Gebaeude }
    public enum ToolMode { Auswahl, LagerBauen, StrasseBauen, FeldBauen }

    /// <summary>Einstieg der Szene: baut Session, Ansichten, Kamera und HUD auf und verteilt Eingaben.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public ulong StartSeed = 12345;
        public GameSession Session { get; private set; }
        public MapView Map { get; private set; }
        public EntityViews Entities { get; private set; }
        public CameraRig Rig { get; private set; }
        public SelectionKind SelKind;
        public int SelId;
        public ToolMode Tool;
        public int RoadStart = -1;
        public int HoverCell = -1;
        public string HoverReason;
        public bool ShowDebug = true;
        public bool ShowStats;
        float _sinceTick;
        GameObject _ghost;

        void Awake()
        {
            ulong seed = StartSeed;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-wb-seed" && ulong.TryParse(args[i + 1], out var s)) seed = s;
            Session = new GameSession(GameSession.DefaultSaveDirectory(Application.persistentDataPath), seed);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera") { tag = "MainCamera" };
            if (!camGo.TryGetComponent<CameraRig>(out var rig)) rig = camGo.AddComponent<CameraRig>();
            Rig = rig;
            if (FindFirstObjectByType<Light>() == null)
            {
                var l = new GameObject("Sonne").AddComponent<Light>();
                l.type = LightType.Directional; l.intensity = 1.1f;
                l.transform.rotation = Quaternion.Euler(55, 30, 0);
            }
            Map = new MapView(transform);
            Entities = new EntityViews(transform);
            if (!TryGetComponent<HudGui>(out _)) gameObject.AddComponent<HudGui>();
            Session.WorldReplaced += OnWorldReplaced;
            OnWorldReplaced();
            if (Array.IndexOf(args, "-wb-smoke") >= 0) gameObject.AddComponent<SmokeRunner>();
        }

        void OnDestroy() { if (Session != null) Session.WorldReplaced -= OnWorldReplaced; }

        void OnWorldReplaced()
        {
            var w = Session.World;
            Map.Rebuild(w.Map);
            Entities.Clear();
            Entities.Sync(w, Session.Content);
            Entities.Animate(1f);
            Rig.Bounds = new Vector2(w.Map.Width, w.Map.Height);
            int civ = w.PlayerCivId != 0 ? w.PlayerCivId : w.Civilizations[0].Id;
            Rig.LookAt(EntityViews.CellCenter(w.Map, w.SettlementById(w.Civ(civ).CapitalSettlementId).CenterCell));
            SelKind = SelectionKind.Keine; SelId = 0; Tool = ToolMode.Auswahl; RoadStart = -1;
        }

        void Update()
        {
            HandleHotkeys();
            int n = Session.Update(Time.unscaledDeltaTime);
            if (n > 0)
            {
                _sinceTick = 0;
                Entities.Sync(Session.World, Session.Content);
                Map.Refresh(Session.World.Map);
            }
            else _sinceTick += Time.unscaledDeltaTime;
            float tps = GameSession.TicksPerSecondAt1x * GameSession.Speeds[Session.SpeedIndex];
            Entities.Animate(Session.Paused ? 1f : _sinceTick * tps);
            HandleMouse();
            UpdateSelectionMarker();
        }

        void HandleHotkeys()
        {
            if (InputAdapter.KeyDown(KeyCode.Space)) Session.Paused = !Session.Paused;
            if (InputAdapter.KeyDown(KeyCode.Alpha1)) Session.SetSpeed(0);
            if (InputAdapter.KeyDown(KeyCode.Alpha2)) Session.SetSpeed(1);
            if (InputAdapter.KeyDown(KeyCode.Alpha3)) Session.SetSpeed(2);
            if (InputAdapter.KeyDown(KeyCode.Alpha4)) Session.SetSpeed(3);
            if (InputAdapter.KeyDown(KeyCode.F5)) Session.Save("schnell");
            if (InputAdapter.KeyDown(KeyCode.F9)) Session.Load("schnell");
            if (InputAdapter.KeyDown(KeyCode.F3)) ShowDebug = !ShowDebug;
            if (InputAdapter.KeyDown(KeyCode.T)) ShowStats = !ShowStats;
            if (InputAdapter.KeyDown(KeyCode.F12)) TakeScreenshot();
            if (InputAdapter.KeyDown(KeyCode.L)) { Map.Layer = (MapLayer)(((int)Map.Layer + 1) % 3); Map.Refresh(Session.World.Map); }
            if (InputAdapter.KeyDown(KeyCode.B)) SetTool(ToolMode.LagerBauen);
            if (InputAdapter.KeyDown(KeyCode.R)) SetTool(ToolMode.StrasseBauen);
            if (InputAdapter.KeyDown(KeyCode.F)) SetTool(ToolMode.FeldBauen);
            if (InputAdapter.KeyDown(KeyCode.Escape)) SetTool(ToolMode.Auswahl);
        }

        public void SetTool(ToolMode t)
        {
            if (t != ToolMode.Auswahl && Session.World.PlayerCivId == 0) { Session.Message = "Beobachtermodus: erst eine Zivilisation wählen."; return; }
            Tool = t; RoadStart = -1;
        }

        public string TakeScreenshot()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(path);
            Session.Message = "Screenshot: " + path;
            return path;
        }

        int CellUnderMouse()
        {
            var ray = Rig.Cam.ScreenPointToRay(InputAdapter.MousePosition);
            if (Mathf.Abs(ray.direction.y) < 1e-4f) return -1;
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0) return -1;
            var p = ray.origin + ray.direction * t;
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.z);
            var m = Session.World.Map;
            return m.InBounds(x, y) ? m.Index(x, y) : -1;
        }

        void HandleMouse()
        {
            var w = Session.World;
            HoverCell = HudGui.PointerOverUi ? -1 : CellUnderMouse();
            HoverReason = null;
            if ((Tool == ToolMode.LagerBauen || Tool == ToolMode.FeldBauen) && HoverCell >= 0)
                HoverReason = Session.Sim.Preview(GameCommand.PlaceBuilding(w.PlayerCivId, ToolDef, HoverCell));
            else if (Tool == ToolMode.StrasseBauen && HoverCell >= 0 && RoadStart >= 0)
                HoverReason = Session.Sim.Preview(GameCommand.PlaceRoad(w.PlayerCivId, RoadStart, HoverCell));
            UpdateGhost();

            if (HudGui.PointerOverUi || !InputAdapter.MouseDown(InputAdapter.Btn.Left)) return;
            switch (Tool)
            {
                case ToolMode.Auswahl:
                    var ray = Rig.Cam.ScreenPointToRay(InputAdapter.MousePosition);
                    if (Physics.Raycast(ray, out var hit, 1000f) && hit.collider.TryGetComponent<EntityTag>(out var tag))
                    { SelKind = tag.Kind == EntityKind.Person ? SelectionKind.Person : SelectionKind.Gebaeude; SelId = tag.Id; }
                    else if (HoverCell >= 0) { SelKind = SelectionKind.Zelle; SelId = HoverCell; }
                    else { SelKind = SelectionKind.Keine; }
                    break;
                case ToolMode.LagerBauen:
                case ToolMode.FeldBauen:
                    if (HoverCell >= 0 && Session.Issue(GameCommand.PlaceBuilding(w.PlayerCivId, ToolDef, HoverCell)) == null)
                        Tool = ToolMode.Auswahl;
                    break;
                case ToolMode.StrasseBauen:
                    if (HoverCell < 0) break;
                    if (RoadStart < 0) { RoadStart = HoverCell; Session.Message = "Straße: Endpunkt wählen."; }
                    else if (Session.Issue(GameCommand.PlaceRoad(w.PlayerCivId, RoadStart, HoverCell)) == null) RoadStart = -1;
                    break;
            }
        }

        string ToolDef => Tool == ToolMode.FeldBauen ? DefaultContent.Field : DefaultContent.Storage;

        void UpdateGhost()
        {
            bool show = Tool != ToolMode.Auswahl && HoverCell >= 0;
            if (_ghost == null)
            {
                _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(_ghost.GetComponent<Collider>());
                _ghost.name = "Bauvorschau";
            }
            _ghost.SetActive(show);
            if (!show) return;
            bool valid = Tool == ToolMode.StrasseBauen && RoadStart < 0 ? true : HoverReason == null;
            _ghost.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.ForColor(valid ? PlaceholderStyle.ValidPlacement : PlaceholderStyle.InvalidPlacement);
            float h = Tool == ToolMode.LagerBauen ? 0.5f : 0.08f; // Feld und Straße flach
            _ghost.transform.localScale = new Vector3(0.95f, h, 0.95f);
            _ghost.transform.position = EntityViews.CellCenter(Session.World.Map, HoverCell) + Vector3.up * h * 0.5f;
        }

        void UpdateSelectionMarker()
        {
            var w = Session.World;
            Vector3? pos = null; float size = 1.2f;
            if (SelKind == SelectionKind.Person) { var p = Entities.PersonPosition(SelId); if (p.HasValue) pos = new Vector3(p.Value.x, 0, p.Value.z); size = 0.8f; }
            else if (SelKind == SelectionKind.Gebaeude) { var b = w.BuildingById(SelId); if (b != null) pos = EntityViews.CellCenter(w.Map, b.Cell); }
            else if (SelKind == SelectionKind.Zelle && w.Map.InBounds(SelId)) { pos = EntityViews.CellCenter(w.Map, SelId); size = 1f; }
            Entities.ShowSelection(pos, size);
        }

        public void CyclePlayerCiv()
        {
            var w = Session.World;
            int idx = -1;
            for (int i = 0; i < w.Civilizations.Count; i++) if (w.Civilizations[i].Id == w.PlayerCivId) idx = i;
            int next = idx + 1 >= w.Civilizations.Count ? 0 : w.Civilizations[idx + 1].Id; // nach der letzten: Beobachter (0)
            Session.Issue(GameCommand.ChoosePlayerCiv(next));
            Tool = ToolMode.Auswahl;
        }
    }
}
