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
            readonly float _hMultiplier;
            readonly uint _stepBudget;
            readonly bool _resultsStartAtIndexZero;

            NativeArray<half> _g, _f;
            NativeArray<uint2> _solution;
            NativeMinHeap<uint2,half,Comparer> _frontier;
            NativeHashSet<uint2> _visited;

            ProfilerMarker __initialization, __search, __neighbours, __frontier_push, __frontier_pop, __update_fg, __trace;

            public AStarJob
            (
                uint2 start,
                uint2 destination,
                NativeArray<EFloorType> moveCost,
                uint2 mapSize,
                NativeList<uint2> results,
                float hMultiplier = 1,
                uint stepBudget = uint.MaxValue,
                bool resultsStartAtIndexZero = true
            )
            {
                this._src = start;
                this._dst = destination;
                this._moveCost = moveCost;
                this._mapSize = mapSize;
                this.results = results;
                this._hMultiplier = hMultiplier;
                this._stepBudget = stepBudget;
                this._resultsStartAtIndexZero = resultsStartAtIndexZero;

                int length = moveCost.Length;
                this._g = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this._f = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this._solution = new (length, Allocator.TempJob);
                this._frontier = new (length, Allocator.TempJob, new Comparer(mapSize), this._f);
                this._visited = new (length, Allocator.TempJob);

                this.__initialization = new ("initialization");
                this.__search = new ("search");
                this.__neighbours = new ("scan neighbors");
                this.__frontier_push = new ("frontier.push");
                this.__frontier_pop = new ("frontier.pop");
                this.__update_fg = new ("update f & g");
                this.__trace = new ("trace path");
            }
            public void Execute()
            {
                __initialization.Begin();
                int srcIndex = GameGrid.ToIndex(_src, _mapSize);
                int dstIndex = GameGrid.ToIndex(_dst, _mapSize);
                {
                    if (_moveCost[srcIndex]!=EFloorType.Traversable) return;
                    if (_moveCost[dstIndex]!=EFloorType.Traversable) return;
                }
                {
                    for (int i=_g.Length-1 ; i!=-1 ; i--)
                        _g[i] = (half) half.MaxValue;
                    _g[srcIndex] = half.zero;
                }
                {
                    for (int i=_f.Length-1 ; i!=-1 ; i--)
                        _f[i] = (half) half.MaxValue;
                    _f[srcIndex] = half.zero;
                }
                _solution[srcIndex] = _src;
                _frontier.Push(_src);
                _visited.Add(_src);
                __initialization.End();

                __search.Begin();
                uint2 currentCoord = new uint2(uint.MaxValue, uint.MaxValue);
                uint numSearchSteps = 0;
                bool destinationReached = false;
                while (
                        _frontier.Length!=0
                    &&  !destinationReached
                    &&  numSearchSteps++<_stepBudget
                )
                {
                    __initialization.Begin();
                    __frontier_pop.Begin();
                    currentCoord = _frontier.Pop();
                    __frontier_pop.End();
                    int currentIndex = GameGrid.ToIndex(currentCoord, _mapSize);
                    float node_g = _g[currentIndex];
                    __initialization.End();

                    __neighbours.Begin();
                    var enumerator = new NeighbourEnumerator(coord:currentCoord, mapSize:_mapSize);
                    while (enumerator.MoveNext(out uint2 neighbourCoord))
                    {
                        int neighbourIndex = GameGrid.ToIndex(neighbourCoord, _mapSize);
                        byte moveCostByte = _moveCost[neighbourIndex]==EFloorType.Traversable
                            ? (byte) 1
                            : (byte) 255;
                        if (moveCostByte==255) continue;// 100% obstacle
                        float movecost = moveCostByte/255f;

                        float g = node_g +(1f + movecost);
                        float h = EuclideanHeuristic(neighbourCoord, _dst) * _hMultiplier;
                        float f = g + h;

                        if (g<_g[neighbourIndex])
                        {
                            __update_fg.Begin();
                            _f[neighbourIndex] = (half) f;
                            _g[neighbourIndex] = (half) g;
                            _solution[neighbourIndex] = currentCoord;
                            __update_fg.End();
                        }

                        __frontier_push.Begin();
                        if (!_visited.Contains(neighbourCoord))
                            _frontier.Push(neighbourCoord);
                        __frontier_push.End();

                        _visited.Add(neighbourCoord);
                    }

                    destinationReached = math.all(currentCoord==_dst);

                    __neighbours.End();
                }
                __search.End();

                __trace.Begin();
                if (destinationReached)
                {
                    bool backtrackSuccess = BacktrackToPath(_solution, _mapSize, _dst, results, _resultsStartAtIndexZero);
                    
                    #if UNITY_ASSERTIONS
                    Assert.IsTrue(backtrackSuccess);
                    #endif
                }
                else
                {
                    results.Clear();
                }
                __trace.End();
            }

            public static float EuclideanHeuristic(uint2 a, uint2 b) => math.length((int2) a - (int2) b);

            public void Dispose()
            {
                this._g.Dispose();
                this._f.Dispose();
                this._solution.Dispose();
                this._frontier.Dispose();
                this._visited.Dispose();
            }
            public struct Comparer : INativeMinHeapComparer<uint2,half>
            {
                public readonly uint2 _mapSize;
                public Comparer(uint2 mapSize) => this._mapSize = mapSize;

                public int Compare(uint2 lhs, uint2 rhs, NativeSlice<half> comparables)
                {
                    float lhsValue = comparables[GameGrid.ToIndex(lhs, _mapSize)];
                    float rhsValue = comparables[GameGrid.ToIndex(rhs, _mapSize)];
                    return lhsValue.CompareTo(rhsValue);
                }
            }
        }
 
        static bool BacktrackToPath
        (
            NativeArray<uint2> solution,
            uint2 mapSize,
            uint2 destination,
            NativeList<uint2> results,
            bool resultsStartAtIndexZero
        )
        {
            results.Clear();
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

            if (resultsStartAtIndexZero)
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
