using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct MoveRangePresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            var lineMat = Resources.Load<Material>("game-move-range-lines");
            Segments.Core.Create(out _segments, lineMat);
            state.EntityManager.AddComponent<IsPlayStateOnly>(_segments);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            state.EntityManager.DestroyEntity(_segments);
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
            if (selectedUnit!=Entity.Null && em.Exists(selectedUnit))
            {
                var inMoveRange = SystemAPI.GetComponent<InMoveRange>(selectedUnit);
                if (inMoveRange.Coords.Count!=0)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, em);

                    const int numSegmentsPerField = 3;
                    var rot = quaternion.RotateX(math.PIHALF);
                    int bufferPosition = buffer.Length;
                    buffer.Length += inMoveRange.Coords.Count * numSegmentsPerField;
                    foreach (uint2 coord in inMoveRange.Coords)
                    {
                        int index = GameGrid.ToIndex(coord, mapSettings.Size);
                        float3 point = mapData.PositionArray[index];
                        Segments.Plot.Circle(buffer, ref bufferPosition, numSegmentsPerField, 0.15f, point, rot);
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
