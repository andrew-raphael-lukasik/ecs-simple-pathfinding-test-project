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
            public NativeList<uint2> Results;

            public readonly uint2 Src;
            public readonly uint2 Dst;
            [ReadOnly] public readonly NativeArray<EFloorType> MoveCost;
            public readonly uint2 MapSize;
            public readonly float HMultiplier;
            public readonly uint StepBudget;
            public readonly bool ResultsStartAtIndexZero;

            public NativeArray<half> G;
            public NativeArray<half> F;
            public NativeArray<uint2> Solution;
            public NativeMinHeap<uint2,half,Comparer> Frontier;
            public NativeHashSet<uint2> Visited;

            ProfilerMarker _PM_Initialization, _PM_Search, _PM_Neighbours, _PM_FrontierPush, _PM_FrontierPop, _PM_UpdateFG, _PM_Trace;

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
                this.Src = start;
                this.Dst = destination;
                this.MoveCost = moveCost;
                this.MapSize = mapSize;
                this.Results = results;
                this.HMultiplier = hMultiplier;
                this.StepBudget = stepBudget;
                this.ResultsStartAtIndexZero = resultsStartAtIndexZero;

                int length = moveCost.Length;
                this.G = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this.F = new (length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                this.Solution = new (length, Allocator.TempJob);
                this.Frontier = new (length, Allocator.TempJob, new Comparer(mapSize), this.F);
                this.Visited = new (length, Allocator.TempJob);

                this._PM_Initialization = new ("initialization");
                this._PM_Search = new ("search");
                this._PM_Neighbours = new ("scan neighbors");
                this._PM_FrontierPush = new ("frontier.push");
                this._PM_FrontierPop = new ("frontier.pop");
                this._PM_UpdateFG = new ("update f & g");
                this._PM_Trace = new ("trace path");
            }
            public void Execute()
            {
                _PM_Initialization.Begin();
                int srcIndex = GameGrid.ToIndex(Src, MapSize);
                int dstIndex = GameGrid.ToIndex(Dst, MapSize);
                {
                    if (MoveCost[srcIndex]!=EFloorType.Traversable) return;
                    if (MoveCost[dstIndex]!=EFloorType.Traversable) return;
                }
                {
                    for (int i=G.Length-1 ; i!=-1 ; i--)
                        G[i] = (half) half.MaxValue;
                    G[srcIndex] = half.zero;
                }
                {
                    for (int i=F.Length-1 ; i!=-1 ; i--)
                        F[i] = (half) half.MaxValue;
                    F[srcIndex] = half.zero;
                }
                Solution[srcIndex] = Src;
                Frontier.Push(Src);
                Visited.Add(Src);
                _PM_Initialization.End();

                _PM_Search.Begin();
                uint2 currentCoord = new uint2(uint.MaxValue, uint.MaxValue);
                uint numSearchSteps = 0;
                bool destinationReached = false;
                while (
                        Frontier.Length!=0
                    &&  !destinationReached
                    &&  numSearchSteps++<StepBudget
                )
                {
                    _PM_Initialization.Begin();
                    _PM_FrontierPop.Begin();
                    currentCoord = Frontier.Pop();
                    _PM_FrontierPop.End();
                    int currentIndex = GameGrid.ToIndex(currentCoord, MapSize);
                    float node_g = G[currentIndex];
                    _PM_Initialization.End();

                    _PM_Neighbours.Begin();
                    var enumerator = new NeighbourEnumerator(coord:currentCoord, mapSize:MapSize.x);
                    while (enumerator.MoveNext(out uint2 neighbourCoord))
                    {
                        int neighbourIndex = GameGrid.ToIndex(neighbourCoord, MapSize);
                        byte moveCostByte = MoveCost[neighbourIndex]==EFloorType.Traversable
                            ? (byte) 1
                            : (byte) 255;
                        if (moveCostByte==255) continue;// 100% obstacle
                        float movecost = moveCostByte/255f;

                        float g = node_g +(1f + movecost);
                        float h = EuclideanHeuristic(neighbourCoord, Dst) * HMultiplier;
                        float f = g + h;
                        
                        if (g<G[neighbourIndex])
                        {
                            _PM_UpdateFG.Begin();
                            F[neighbourIndex] = (half) f;
                            G[neighbourIndex] = (half) g;
                            Solution[neighbourIndex] = currentCoord;
                            _PM_UpdateFG.End();
                        }

                        _PM_FrontierPush.Begin();
                        if (!Visited.Contains(neighbourCoord))
                            Frontier.Push(neighbourCoord);
                        _PM_FrontierPush.End();

                        Visited.Add(neighbourCoord);
                    }

                    destinationReached = math.all(currentCoord==Dst);

                    _PM_Neighbours.End();
                }
                _PM_Search.End();

                _PM_Trace.Begin();
                if (destinationReached)
                {
                    bool backtrackSuccess = BacktrackToPath(Solution, MapSize, Dst, Results, ResultsStartAtIndexZero);
                    
                    #if UNITY_ASSERTIONS
                    Assert.IsTrue(backtrackSuccess);
                    #endif
                }
                else
                {
                    Results.Clear();
                }
                _PM_Trace.End();
            }

            public static float EuclideanHeuristic(uint2 a, uint2 b) => math.length((int2) a - (int2) b);

            public void Dispose()
            {
                this.G.Dispose();
                this.F.Dispose();
                this.Solution.Dispose();
                this.Frontier.Dispose();
                this.Visited.Dispose();
            }
            public struct Comparer : INativeMinHeapComparer<uint2,half>
            {
                public uint2 mapSize;
                public Comparer(uint2 mapSize) => this.mapSize = mapSize;

                public int Compare(uint2 lhs, uint2 rhs, NativeSlice<half> comparables)
                {
                    float lhsValue = comparables[GameGrid.ToIndex(lhs, mapSize)];
                    float rhsValue = comparables[GameGrid.ToIndex(rhs, mapSize)];
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
            if (results.Capacity<mapSize.x*2) results.Capacity = (int)mapSize.x*2;
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
