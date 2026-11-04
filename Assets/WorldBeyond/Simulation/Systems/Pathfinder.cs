using System.Collections.Generic;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>Deterministisches A* (4er-Nachbarschaft, Gleichstand nach Zellindex). Puffer werden wiederverwendet.</summary>
    public sealed class Pathfinder
    {
        public const int MaxExpanded = 20000;
        readonly WorldMap _map;
        readonly int[] _g, _from, _stamp, _closed;
        int _run;
        readonly List<long> _heap = new List<long>();
        static readonly int[] Dx = { 1, -1, 0, 0 }, Dy = { 0, 0, 1, -1 };

        public Pathfinder(WorldMap map)
        {
            _map = map;
            int n = map.CellCount;
            _g = new int[n]; _from = new int[n]; _stamp = new int[n]; _closed = new int[n];
        }

        /// <summary>Füllt <paramref name="path"/> mit den Zellen nach dem Start bis einschließlich Ziel. False = unerreichbar.</summary>
        public bool FindPath(int start, int goal, List<int> path)
        {
            path.Clear();
            if (start == goal) return true;
            if (!_map.IsWalkable(goal) || !_map.IsWalkable(start)) return false;
            _run++;
            _heap.Clear();
            _g[start] = 0; _stamp[start] = _run; _from[start] = -1;
            Push(_map.Manhattan(start, goal), start);
            int expanded = 0;
            while (_heap.Count > 0)
            {
                int cur = (int)(Pop() & 0xFFFFFFFF);
                if (_closed[cur] == _run) continue;
                _closed[cur] = _run;
                if (cur == goal) break;
                if (++expanded > MaxExpanded) return false;
                int cx = _map.X(cur), cy = _map.Y(cur);
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + Dx[d], ny = cy + Dy[d];
                    if (!_map.InBounds(nx, ny)) continue;
                    int ni = _map.Index(nx, ny);
                    if (!_map.IsWalkable(ni) || _closed[ni] == _run) continue;
                    int ng = _g[cur] + _map.MoveCost(ni);
                    if (_stamp[ni] != _run || ng < _g[ni])
                    {
                        _stamp[ni] = _run; _g[ni] = ng; _from[ni] = cur;
                        Push(ng + _map.Manhattan(ni, goal), ni);
                    }
                }
            }
            if (_closed[goal] != _run) return false;
            for (int c = goal; c != start; c = _from[c]) path.Add(c);
            path.Reverse();
            return true;
        }

        void Push(int f, int cell)
        {
            long key = ((long)f << 32) | (uint)cell;
            _heap.Add(key);
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_heap[p] <= key) break;
                _heap[i] = _heap[p]; i = p;
            }
            _heap[i] = key;
        }

        long Pop()
        {
            long top = _heap[0];
            long last = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            int n = _heap.Count, i = 0;
            if (n == 0) return top;
            while (true)
            {
                int l = 2 * i + 1;
                if (l >= n) break;
                int r = l + 1;
                int m = (r < n && _heap[r] < _heap[l]) ? r : l;
                if (_heap[m] >= last) break;
                _heap[i] = _heap[m]; i = m;
            }
            _heap[i] = last;
            return top;
        }
    }
}
