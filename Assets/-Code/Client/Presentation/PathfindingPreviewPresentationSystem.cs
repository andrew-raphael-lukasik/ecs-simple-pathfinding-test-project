using UnityEngine;
using UnityEngine.AddressableAssets;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PathfindingPreviewPresentationSystem : ISystem
    {
        Entity _segments, _segments2;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var mat1LoadOp = Addressables.LoadAssetAsync<Material>("game-move-preview-path-lines.mat");
            var mat2LoadOp = Addressables.LoadAssetAsync<Material>("game-attack-range-lines.mat");

            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            Segments.Core.Create(out _segments, mat1LoadOp.WaitForCompletion());
            Segments.Core.Create(out _segments2, mat2LoadOp.WaitForCompletion());
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            var bufferRW = SystemAPI.GetComponentRW<Segments.Segment>(_segments).ValueRW.Buffer;
            var buffer2RW = SystemAPI.GetComponentRW<Segments.Segment>(_segments2).ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit) && SystemAPI.HasComponent<PathfindingPreviewQueryResult>(selectedUnit))
            {
                var pathResults = SystemAPI.GetComponent<PathfindingPreviewQueryResult>(selectedUnit);
                if (pathResults.Success==1)
                {
                    bufferRW.Length = 0;
                    buffer2RW.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                    Segments.Core.SetSegmentChanged(_segments2, state.EntityManager);

                    int firstIndexOutsideMoveRange = 0;
                    {
                        var inMoveRange = SystemAPI.GetComponentRW<InMoveRange>(selectedUnit);
                        foreach (uint2 next in pathResults.Path)
                        if (inMoveRange.ValueRO.Coords.Contains(next))
                            firstIndexOutsideMoveRange++;
                        else
                        {
                            firstIndexOutsideMoveRange = math.max(firstIndexOutsideMoveRange - 1, 0);
                            break;
                        }
                    }

                    {
                        int pathLength = pathResults.Path.Length;
                        int bufferPosition = bufferRW.Length;
                        const int num_segments_per_line = 3;
                        bufferRW.Length += pathLength * num_segments_per_line;
                        uint2 coord = pathResults.Path[firstIndexOutsideMoveRange];
                        for (int i = firstIndexOutsideMoveRange+1; i < pathLength; ++i)
                        {
                            int indexPrev = GameGrid.ToIndex(coord, mapSettings.Size);
                            coord = pathResults.Path[i];
                            int index = GameGrid.ToIndex(coord, mapSettings.Size);
                            float3 p0 = mapData.PositionArray[indexPrev] + new float3(0, .2f, 0);
                            float3 p1 = mapData.PositionArray[index] + new float3(0, .2f, 0);

                            Segments.Plot.DashedLine(
                                segments: bufferRW,
                                index: ref bufferPosition,
                                numSegments: num_segments_per_line,
                                start: p0,
                                end: p1
                            );
                        }
                    }

                    bool isPathLeadingToEnemyUnit;
                    {
                        uint2 dstCoord = pathResults.Path[pathResults.Path.Length-1];
                        var unitsRO = SystemAPI.GetSingletonRW<UnitsSingleton>().ValueRO;
                        int dstIndex = GameGrid.ToIndex(dstCoord, mapSettings.Size);
                        Entity dstUnit = unitsRO.Lookup[dstIndex];
                        if (dstUnit==Entity.Null) isPathLeadingToEnemyUnit = false;
                        else
                        {
                            isPathLeadingToEnemyUnit = SystemAPI.HasComponent<IsPlayerUnit>(selectedUnit)
                                ? SystemAPI.HasComponent<IsEnemyUnit>(dstUnit)
                                : SystemAPI.HasComponent<IsPlayerUnit>(dstUnit);
                        }
                    }
                    if (isPathLeadingToEnemyUnit)
                    {
                        ushort attackRange = SystemAPI.GetComponentRW<AttackRange>(selectedUnit).ValueRO;
                        int possibleAttackIndex = math.max(pathResults.Path.Length - 1 - attackRange, 0);

                        int pathLength = pathResults.Path.Length;
                        const int numSegmentsPerField = 3;
                        var rot = quaternion.RotateX(math.PIHALF);
                        int buffer2Position = buffer2RW.Length;
                        buffer2RW.Length += attackRange * numSegmentsPerField;
                        for (int i = possibleAttackIndex; i < pathLength; ++i)
                        {
                            uint2 coord = pathResults.Path[i];
                            int index = GameGrid.ToIndex(coord, mapSettings.Size);
                            float3 point = mapData.PositionArray[index];
                            Segments.Plot.Circle(buffer2RW, ref buffer2Position, numSegmentsPerField, 0.05f, point, rot);
                        }
                    }
                }
                else
                {
                    if(bufferRW.Length!=0)
                    {
                        bufferRW.Length = 0;
                        Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                    }
                    if(buffer2RW.Length!=0)
                    {
                        buffer2RW.Length = 0;
                        Segments.Core.SetSegmentChanged(_segments2, state.EntityManager);
                    }
                }
            }
            else
            {
                if(bufferRW.Length!=0)
                {
                    bufferRW.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
                if(buffer2RW.Length!=0)
                {
                    buffer2RW.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments2, state.EntityManager);
                }
            }
        }
    }
}
