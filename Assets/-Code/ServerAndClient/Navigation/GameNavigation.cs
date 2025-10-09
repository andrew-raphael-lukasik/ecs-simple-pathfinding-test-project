using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

using ServerAndClient.Gameplay;

namespace ServerAndClient.Navigation
{
    public static class GameNavigation
    {

        [Unity.Burst.BurstCompile]
        public struct AStarJob : IJob, System.IDisposable
        {
            public NativeList<uint2> results;

            readonly uint2 _src, _dst;
            [ReadOnly] readonly NativeArray<EFloorType> _moveCost;
            readonly uint2 _mapSize;
            readonly ushort _gmax;
            readonly float _hMultiplier;

            [DeallocateOnJobCompletion] NativeArray<ushort> _g, _f;
            [DeallocateOnJobCompletion] NativeArray<uint2> _solution;
            NativeMinHeap<uint2, ushort, TieBreakingComparer_uint16> _frontier;

            ProfilerMarker __initialization, __search, __neighbours, __frontier_push, __frontier_pop, __update_fg, __trace;

            public AStarJob
            (
                uint2 start,
                uint2 destination,
                ushort moveRange,
                NativeArray<EFloorType> moveCost,
                uint2 mapSize,
                NativeList<uint2> results,
                float hMultiplier = 1
            )
            {
                this._src = start;
                this._dst = destination;
                this._gmax = moveRange;
                this._moveCost = moveCost;
                this._mapSize = mapSize;
                this.results = results;
                this._hMultiplier = hMultiplier;

                int length = moveCost.Length;
                this._g = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this._f = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this._solution = new (length, Allocator.TempJob);
                this._frontier = new (length, Allocator.TempJob, new (src: start, dst: destination, mapSize: mapSize), this._f);

                this.__initialization = new ("initialization");
                this.__search = new ("search");
                this.__neighbours = new ("scan neighbors");
                this.__frontier_push = new ("frontier.push");
                this.__frontier_pop = new ("frontier.pop");
                this.__update_fg = new ("update f & g");
                this.__trace = new ("trace path");
            }
            void IJob.Execute()
            {
                __initialization.Begin();
                results.Clear();
                int srcIndex = GameGrid.ToIndex(_src, _mapSize);
                int dstIndex = GameGrid.ToIndex(_dst, _mapSize);
                {
                    if (_moveCost[srcIndex]!=EFloorType.Traversable) return;
                    if (_moveCost[dstIndex]!=EFloorType.Traversable) return;
                }
                {
                    for (int i=_g.Length-1 ; i!=-1 ; i--)
                        _g[i] = ushort.MaxValue;
                    _g[srcIndex] = 0;
                }
                {
                    for (int i=_f.Length-1 ; i!=-1 ; i--)
                        _f[i] = ushort.MaxValue;
                    _f[srcIndex] = 0;
                }
                _solution[srcIndex] = _src;
                _frontier.Push(_src);
                __initialization.End();

                __search.Begin();
                uint2 coord;
                bool destinationReached = false;
                while (
                        _frontier.Length!=0
                    &&  !destinationReached
                )
                {
                    __initialization.Begin();
                    __frontier_pop.Begin();
                    coord = _frontier.Pop();
                    __frontier_pop.End();
                    int index = GameGrid.ToIndex(coord, _mapSize);
                    ushort node_g = _g[index];
                    __initialization.End();

                    __neighbours.Begin();
                    var enumerator = new NeighbourEnumerator(coord:coord, mapSize:_mapSize);
                    while (enumerator.MoveNext(out uint2 neighbourCoord))
                    {
                        int neighbourIndex = GameGrid.ToIndex(neighbourCoord, _mapSize);
                        if (_moveCost[neighbourIndex]!=EFloorType.Traversable) continue;// 100% obstacle

                        ushort g = (ushort)(node_g + 1);
                        if (g>_gmax) continue;// range limit reached

                        if (g<_g[neighbourIndex])
                        {
                            __update_fg.Begin();
                            ushort h = (ushort)(ManhattanHeuristic(neighbourCoord, _dst) * _hMultiplier);
                            ushort f = (ushort)(g + h);
                            _f[neighbourIndex] = f;
                            _g[neighbourIndex] = g;
                            _solution[neighbourIndex] = coord;
                            __update_fg.End();

                            __frontier_push.Begin();
                            _frontier.Push(neighbourCoord);
                            __frontier_push.End();
                        }
                    }
                    __neighbours.End();

                    destinationReached = math.all(coord==_dst);
                }
                __search.End();

                __trace.Begin();
                if (destinationReached)
                {
                    bool backtrackSuccess = BacktrackToPath(_solution, _mapSize, _dst, results);
                    
                    #if UNITY_ASSERTIONS
                    Assert.IsTrue(backtrackSuccess);
                    #endif
                }
                __trace.End();
            }

            public void Dispose()
            {
                this._frontier.Dispose();
            }
        }

        [Unity.Burst.BurstCompile]
        public struct MoveReachJob : IJob, System.IDisposable
        {
            public NativeHashSet<uint2> reachable;
            readonly uint2 _src;
            [ReadOnly] readonly NativeArray<EFloorType> _moveCost;
            readonly uint2 _mapSize;
            readonly ushort _gmax;

            [DeallocateOnJobCompletion] NativeArray<ushort> _g;
            NativeMinHeap<uint2, ushort, BasicComparer_uint16> _frontier;

            ProfilerMarker __initialization, __search, __neighbours, __frontier_push, __frontier_pop, __update_g;

            public MoveReachJob
            (
                uint2 start,
                ushort range,
                NativeArray<EFloorType> moveCost,
                uint2 mapSize,
                NativeHashSet<uint2> reachable
            )
            {
                this._src = start;
                this._gmax = range;
                this._moveCost = moveCost;
                this._mapSize = mapSize;
                this.reachable = reachable;

                int length = moveCost.Length;
                this._g = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this._frontier = new (length, Allocator.TempJob, new (mapSize), _g);

                this.__initialization = new ("initialization");
                this.__search = new ("search");
                this.__neighbours = new ("scan neighbors");
                this.__frontier_push = new ("frontier.push");
                this.__frontier_pop = new ("frontier.pop");
                this.__update_g = new ("update g");
            }
            void IJob.Execute()
            {
                __initialization.Begin();
                reachable.Clear();
                int srcIndex = GameGrid.ToIndex(_src, _mapSize);
                if (_moveCost[srcIndex]!=EFloorType.Traversable) return;
                {
                    for (int i=_g.Length-1 ; i!=-1 ; i--)
                        _g[i] = ushort.MaxValue;
                    _g[srcIndex] = 0;
                }
                _frontier.Push(_src);
                __initialization.End();

                __search.Begin();
                uint2 coord;
                while (_frontier.Length!=0)
                {
                    __initialization.Begin();
                    __frontier_pop.Begin();
                    coord = _frontier.Pop();
                    __frontier_pop.End();
                    int index = GameGrid.ToIndex(coord, _mapSize);
                    ushort node_g = _g[index];
                    __initialization.End();

                    __neighbours.Begin();
                    var enumerator = new NeighbourEnumerator(coord:coord, mapSize:_mapSize);
                    while (enumerator.MoveNext(out uint2 neighbourCoord))
                    {
                        int neighbourIndex = GameGrid.ToIndex(neighbourCoord, _mapSize);
                        if (_moveCost[neighbourIndex]!=EFloorType.Traversable) continue;// 100% obstacle

                        ushort g = (ushort)(node_g + 1);
                        if (g>_gmax) continue;// range limit reached

                        if (g<_g[neighbourIndex])
                        {
                            __update_g.Begin();
                            _g[neighbourIndex] = g;
                            reachable.Add(neighbourCoord);
                            __update_g.End();

                            __frontier_push.Begin();
                            _frontier.Push(neighbourCoord);
                            __frontier_push.End();
                        }
                    }
                    __neighbours.End();
                }
                __search.End();
            }

            public void Dispose()
            {
                if (this._frontier.IsCreated) this._frontier.Dispose();
            }
        }

        public static float EuclideanHeuristic(uint2 a, uint2 b) => math.length((int2) a - (int2) b);
        public static float ManhattanHeuristic(uint2 a, uint2 b) => math.csum(math.abs((int2) a - (int2) b));
 
        static bool BacktrackToPath
        (
            NativeArray<uint2> solution,
            uint2 mapSize,
            uint2 destination,
            NativeList<uint2> results
        )
        {
            int solutionLength = solution.Length;

            uint2 posCoord = destination;
            int posIndex = GameGrid.ToIndex(posCoord, mapSize);
            int step = 0;
            while (!math.all(posCoord==solution[posIndex]) && step<solutionLength)
            {
                results.Add(posCoord);
                posCoord = solution[posIndex];
                posIndex = GameGrid.ToIndex(posCoord, mapSize);
                step++;
            }
            bool wasDestinationReached = math.all(posCoord==solution[posIndex]);

            ReverseArray(results.AsArray());

            return wasDestinationReached;
        }

        static void ReverseArray <T>(NativeArray<T> array) where T : unmanaged
        {
            int length = array.Length;
            int lengthHalf = length / 2;
            int last = length-1;
            for (int i=0 ; i<lengthHalf ; i++)
            {
                var tmp = array[i];
                array[i] = array[last-i];
                array[last-i] = tmp;
            }
        }

        struct BasicComparer_uint16 : INativeMinHeapComparer<uint2,ushort>
        {
            public readonly uint2 _mapSize;
            public BasicComparer_uint16(uint2 mapSize) => this._mapSize = mapSize;

            public int Compare(uint2 lhs, uint2 rhs, NativeSlice<ushort> comparables)
            {
                float lhsValue = comparables[GameGrid.ToIndex(lhs, _mapSize)];
                float rhsValue = comparables[GameGrid.ToIndex(rhs, _mapSize)];
                return lhsValue.CompareTo(rhsValue);
            }
        }

        struct TieBreakingComparer_uint16 : INativeMinHeapComparer<uint2,ushort>
        {
            readonly uint2 _mapSize;
            readonly float2 _src, _dst;
            public TieBreakingComparer_uint16(uint2 src,uint2 dst, uint2 mapSize)
            {
                this._src = (float2) src;
                this._dst = (float2) dst;
                this._mapSize = mapSize;
            }

            public int Compare(uint2 lhs, uint2 rhs, NativeSlice<ushort> comparables)
            {
                float lhsF = comparables[GameGrid.ToIndex(lhs, _mapSize)];
                float rhsF = comparables[GameGrid.ToIndex(rhs, _mapSize)];
                int fComparison = lhsF.CompareTo(rhsF);
                if (fComparison!=0) return fComparison;
                float lhsD = pointSegmentDistance(_src, _dst, (float2) lhs);
                float rhsD = pointSegmentDistance(_src, _dst, (float2) rhs);
                return lhsD.CompareTo(rhsD);
            }

            float pointSegmentDistance(float2 a, float2 b, float2 p)
            {
                float2 ab = b - a;
                float2 bp = p - b;
                float2 ap = p - a;
                if (math.dot(ab, bp) > 0) return math.distance(b, p);
                else if (math.dot(ab, ap) < 0) return math.distance(a, p);
                else return math.abs(ab.x * ap.y - ab.y * ap.x) / math.length(ab);
            }
        }

        struct NeighbourEnumerator
        {
            readonly int2 _coord;
            readonly uint _xMax, _yMax;
            uint2 _current;
            byte _tick;

            public NeighbourEnumerator(uint2 coord, uint2 mapSize)
            {
                this._coord = (int2) coord;
                this._xMax = mapSize.x - 1;
                this._yMax = mapSize.y - 1;
                this._current = new uint2(uint.MaxValue, uint.MaxValue);
                this._tick = 0;
            }

            public uint2 Current => _current;

            public bool MoveNext()
            {
                uint2 candidate;
                switch(_tick++)
                {
                    case 0: candidate = (uint2)(_coord + new int2(0, -1)); break;
                    case 1: candidate = (uint2)(_coord + new int2(-1, 0)); break;
                    case 2: candidate = (uint2)(_coord + new int2(1, 0)); break;
                    case 3: candidate = (uint2)(_coord + new int2(0, 1)); break;
                    default: return false;
                }
                if (math.any(new bool4(candidate.x<0, candidate.y<0, candidate.x>_xMax, candidate.y>_yMax))) return MoveNext();
                _current = candidate;
                return true;
            }

            public bool MoveNext(out uint2 neighbourCoord)
            {
                bool success = MoveNext();
                neighbourCoord = _current;
                return success;
            }

            public void Reset()
            {
                _current = (uint2) _coord;
                _tick = 0;
            }

        }

    }
}
