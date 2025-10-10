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
    public partial struct PathfindingPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            var lineMat = Resources.Load<Material>("game-move-path-lines");
            Segments.Core.Create(out _segments, lineMat);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && em.Exists(selectedUnit) && em.HasComponent<PathfindingQueryResult>(selectedUnit))
            {
                var pathResults = em.GetComponentData<PathfindingQueryResult>(selectedUnit);
                if (pathResults.Success==1)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, em);

                    int pathLength = pathResults.Path.Length;
                    int bufferStart = buffer.Length;
                    buffer.Length += pathLength;
                    uint2 coord = pathResults.Path[0];
                    for (int i = 1; i < pathLength; i++)
                    {
                        int indexPrev = GameGrid.ToIndex(coord, mapSettings.Size);
                        coord = pathResults.Path[i];
                        int index = GameGrid.ToIndex(coord, mapSettings.Size);
                        buffer[bufferStart+i] = new float3x2(
                            mapData.PositionArray[indexPrev] + new float3(0, .2f, 0),
                            mapData.PositionArray[index] + new float3(0, .2f, 0)
                        );
                    }
                }
                else if(buffer.Length!=0)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, em);
                }
            }
            else if(buffer.Length!=0)
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, em);
            }
        }
    }
}
