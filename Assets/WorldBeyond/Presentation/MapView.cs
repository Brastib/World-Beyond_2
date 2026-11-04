using System.Collections.Generic;
using UnityEngine;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Presentation
{
    public enum MapLayer { Biome, Ressourcen, Strassen }

    /// <summary>Karte als ein Quad mit Punkt-gefilterter Textur je Chunk (32×32 Zellen). 1 Zelle = 1 Welteinheit.</summary>
    public sealed class MapView
    {
        readonly Transform _root;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        readonly List<GameObject> _chunks = new List<GameObject>();
        Color32[] _buffer;
        public MapLayer Layer = MapLayer.Ressourcen;
        public int ChunkCount => _chunks.Count;

        public MapView(Transform parent)
        {
            _root = new GameObject("Karte").transform;
            _root.SetParent(parent, false);
        }

        public void Rebuild(WorldMap m)
        {
            foreach (var c in _chunks) Object.Destroy(c);
            foreach (var t in _textures) Object.Destroy(t);
            _chunks.Clear(); _textures.Clear();
            int cs = WorldMap.ChunkSize;
            _buffer = new Color32[cs * cs];
            for (int cy = 0; cy < m.ChunksY; cy++)
                for (int cx = 0; cx < m.ChunksX; cx++)
                {
                    var tex = new Texture2D(cs, cs, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = $"Chunk {cx},{cy}" };
                    var go = new GameObject($"Chunk {cx},{cy}");
                    go.transform.SetParent(_root, false);
                    go.AddComponent<MeshFilter>().sharedMesh = Quad(cs);
                    go.AddComponent<MeshRenderer>().sharedMaterial = PlaceholderMaterials.ForTexture(tex);
                    go.transform.localPosition = new Vector3(cx * cs, 0f, cy * cs);
                    _chunks.Add(go); _textures.Add(tex);
                }
            Refresh(m);
        }

        public void Refresh(WorldMap m)
        {
            int cs = WorldMap.ChunkSize;
            for (int cy = 0; cy < m.ChunksY; cy++)
                for (int cx = 0; cx < m.ChunksX; cx++)
                {
                    for (int y = 0; y < cs; y++)
                        for (int x = 0; x < cs; x++)
                            _buffer[y * cs + x] = CellColor(m, m.Index(cx * cs + x, cy * cs + y));
                    var tex = _textures[cy * m.ChunksX + cx];
                    tex.SetPixels32(_buffer);
                    tex.Apply(false);
                }
        }

        Color32 CellColor(WorldMap m, int i)
        {
            Color32 c = PlaceholderStyle.BiomeColor(m.Biomes[i], m.Elevation[i]);
            if (m.Road[i]) return PlaceholderStyle.Road;
            if (Layer == MapLayer.Strassen) return Color32.Lerp(c, new Color32(40, 40, 40, 255), 0.6f);
            if (Layer == MapLayer.Ressourcen && m.ResourceAmount[i] > 0)
            {
                switch (m.Resource[i])
                {
                    case NaturalResource.Beeren: return PlaceholderStyle.Berries;
                    case NaturalResource.Baeume: return Color32.Lerp(c, PlaceholderStyle.Trees, 0.6f);
                    case NaturalResource.Felsen: return Color32.Lerp(c, PlaceholderStyle.Rocks, 0.5f);
                }
            }
            return c;
        }

        static Mesh _quad;
        static Mesh Quad(int size)
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "ChunkQuad" };
            _quad.vertices = new[] { new Vector3(0, 0, 0), new Vector3(0, 0, size), new Vector3(size, 0, size), new Vector3(size, 0, 0) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.RecalculateNormals();
            _quad.RecalculateBounds();
            return _quad;
        }
    }
}
