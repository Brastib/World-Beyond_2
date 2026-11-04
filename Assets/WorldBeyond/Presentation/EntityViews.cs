using System.Collections.Generic;
using UnityEngine;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Presentation
{
    public enum EntityKind { Person, Gebaeude }

    /// <summary>Verknüpft ein Platzhalter-Objekt mit einer Simulations-Id (für Auswahl per Raycast).</summary>
    public sealed class EntityTag : MonoBehaviour
    {
        public EntityKind Kind;
        public int Id;
    }

    /// <summary>Platzhalter für Personen (Kugeln) und Gebäude (Quader). Liest den Zustand, schreibt ihn nie.</summary>
    public sealed class EntityViews
    {
        readonly Transform _root;
        readonly Dictionary<int, GameObject> _people = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> _buildings = new Dictionary<int, GameObject>();
        readonly Dictionary<int, Vector3> _from = new Dictionary<int, Vector3>();
        readonly Dictionary<int, Vector3> _to = new Dictionary<int, Vector3>();
        readonly HashSet<int> _seen = new HashSet<int>();
        readonly List<int> _remove = new List<int>();
        GameObject _selection;

        public EntityViews(Transform parent)
        {
            _root = new GameObject("Entitäten").transform;
            _root.SetParent(parent, false);
        }

        public static Vector3 CellCenter(WorldMap m, int cell) => new Vector3(m.X(cell) + 0.5f, 0f, m.Y(cell) + 0.5f);

        public void Clear()
        {
            foreach (var g in _people.Values) Object.Destroy(g);
            foreach (var g in _buildings.Values) Object.Destroy(g);
            _people.Clear(); _buildings.Clear(); _from.Clear(); _to.Clear();
        }

        /// <summary>Nach jedem Simulationstick: Zielpositionen und Gebäudezustand übernehmen.</summary>
        public void Sync(WorldState w)
        {
            var m = w.Map;
            _seen.Clear();
            foreach (var p in w.People)
            {
                _seen.Add(p.Id);
                if (!_people.TryGetValue(p.Id, out var go))
                {
                    go = Create(PlaceholderShape.Kugel, PlaceholderStyle.Person.Scale, PlaceholderStyle.Civ(w.Civ(p.CivId).ColorRgb), EntityKind.Person, p.Id, "Person " + p.Name);
                    var carry = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(carry.GetComponent<Collider>());
                    carry.name = "Traglast";
                    carry.transform.SetParent(go.transform, false);
                    carry.transform.localPosition = new Vector3(0, 0.9f, 0);
                    carry.transform.localScale = Vector3.one * 0.5f;
                    _people[p.Id] = go;
                    _to[p.Id] = CellCenter(m, p.Cell);
                }
                _from[p.Id] = go.transform.position - Vector3.up * PlaceholderStyle.Person.Scale.y * 0.5f;
                _to[p.Id] = CellCenter(m, p.Cell);
                var carryT = go.transform.GetChild(0).gameObject;
                carryT.SetActive(p.CarryAmount > 0);
                if (p.CarryAmount > 0) carryT.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.ForColor(PlaceholderStyle.Resource(p.CarryType));
            }
            Prune(_people, _seen);

            _seen.Clear();
            foreach (var b in w.Buildings)
            {
                _seen.Add(b.Id);
                var spec = PlaceholderStyle.Building(b.DefId);
                if (!_buildings.TryGetValue(b.Id, out var go))
                {
                    go = Create(PlaceholderShape.Quader, spec.Scale, Color.white, EntityKind.Gebaeude, b.Id, spec.Label);
                    _buildings[b.Id] = go;
                }
                var color = b.Complete ? PlaceholderStyle.Civ(w.Civ(b.CivId).ColorRgb) : PlaceholderStyle.ConstructionSite;
                go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.ForColor(color);
                float h = b.Complete ? spec.Scale.y : Mathf.Max(0.1f, spec.Scale.y * 0.25f);
                go.transform.localScale = new Vector3(spec.Scale.x, h, spec.Scale.z);
                go.transform.position = CellCenter(m, b.Cell) + Vector3.up * h * 0.5f;
            }
            Prune(_buildings, _seen);
        }

        /// <summary>Jedes Frame: Personen zwischen letzter und aktueller Zelle interpolieren.</summary>
        public void Animate(float t)
        {
            foreach (var kv in _people)
            {
                var pos = Vector3.Lerp(_from[kv.Key], _to[kv.Key], Mathf.Clamp01(t));
                kv.Value.transform.position = pos + Vector3.up * PlaceholderStyle.Person.Scale.y * 0.5f;
            }
            // Auswahl-Raycasts laufen im selben Frame: Collider sofort nachziehen, nicht erst beim nächsten Physik-Schritt.
            Physics.SyncTransforms();
        }

        public void ShowSelection(Vector3? worldPos, float size)
        {
            if (_selection == null)
            {
                _selection = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(_selection.GetComponent<Collider>());
                _selection.name = "Auswahlmarkierung";
                _selection.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.ForColor(PlaceholderStyle.Selection);
            }
            _selection.SetActive(worldPos.HasValue);
            if (worldPos.HasValue)
            {
                _selection.transform.position = worldPos.Value + Vector3.up * 0.02f;
                _selection.transform.localScale = new Vector3(size, 0.01f, size);
            }
        }

        public Vector3? PersonPosition(int id) => _people.TryGetValue(id, out var g) ? g.transform.position : (Vector3?)null;

        GameObject Create(PlaceholderShape shape, Vector3 scale, Color color, EntityKind kind, int id, string name)
        {
            var go = GameObject.CreatePrimitive(shape == PlaceholderShape.Kugel ? PrimitiveType.Sphere : PrimitiveType.Cube);
            go.name = $"{name} #{id}";
            go.transform.SetParent(_root, false);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.ForColor(color);
            var tag = go.AddComponent<EntityTag>();
            tag.Kind = kind; tag.Id = id;
            return go;
        }

        void Prune(Dictionary<int, GameObject> map, HashSet<int> alive)
        {
            _remove.Clear();
            foreach (var k in map.Keys) if (!alive.Contains(k)) _remove.Add(k);
            foreach (var k in _remove) { Object.Destroy(map[k]); map.Remove(k); }
        }
    }
}
