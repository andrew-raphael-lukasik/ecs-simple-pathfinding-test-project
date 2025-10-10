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
    public partial struct AttackRangePresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            var lineMat = Resources.Load<Material>("game-attack-range-lines");
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
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();
            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit))
            {
                var inAttackRange = SystemAPI.GetComponent<InAttackRange>(selectedUnit);
                if (inAttackRange.Coords.Count!=0)
                {
                    var mapDataRef = SystemAPI.GetSingletonRW<GeneratedMapData>();
                    var floors = mapDataRef.ValueRO.FloorArray;

                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

                    const int numSegmentsPerField = 3;
                    var rot = quaternion.RotateX(math.PIHALF);
                    int bufferPosition = buffer.Length;
                    buffer.Length += inAttackRange.Coords.Count * numSegmentsPerField;
                    foreach (uint2 coord in inAttackRange.Coords)
                    {
                        int index = GameGrid.ToIndex(coord, mapSettings.Size);
                        if (floors[index]==EFloorType.Traversable)
                        {
                            float3 point = mapData.PositionArray[index];
                            Segments.Plot.Circle(buffer, ref bufferPosition, numSegmentsPerField, 0.05f, point, rot);
                        }
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
