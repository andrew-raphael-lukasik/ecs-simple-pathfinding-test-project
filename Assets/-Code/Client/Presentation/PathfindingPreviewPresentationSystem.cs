using UnityEngine;
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
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            var lineMat = Resources.Load<Material>("game-move-preview-path-lines");
            Segments.Core.Create(out _segments, lineMat);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit) && SystemAPI.HasComponent<PathfindingPreviewQueryResult>(selectedUnit))
            {
                var pathResults = SystemAPI.GetComponent<PathfindingPreviewQueryResult>(selectedUnit);
                if (pathResults.Success==1)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

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

                    int pathLength = pathResults.Path.Length;
                    int bufferPosition = buffer.Length;
                    const int num_segments_per_line = 3;
                    buffer.Length += pathLength * num_segments_per_line;
                    uint2 coord = pathResults.Path[firstIndexOutsideMoveRange];
                    for (int i = firstIndexOutsideMoveRange+1; i < pathLength; i++)
                    {
                        int indexPrev = GameGrid.ToIndex(coord, mapSettings.Size);
                        coord = pathResults.Path[i];
                        int index = GameGrid.ToIndex(coord, mapSettings.Size);
                        float3 p0 = mapData.PositionArray[indexPrev] + new float3(0, .2f, 0);
                        float3 p1 = mapData.PositionArray[index] + new float3(0, .2f, 0);

                        Segments.Plot.DashedLine(
                            segments: buffer,
                            index: ref bufferPosition,
                            numSegments: num_segments_per_line,
                            start: p0,
                            end: p1
                        );
                    }
                }
                else if(buffer.Length!=0)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
            }
            else if(buffer.Length!=0)
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }
        }
    }
}
