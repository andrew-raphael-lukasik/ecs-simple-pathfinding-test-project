using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;

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
            state.RequireForUpdate<PlayerInputSingleton>();
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
            var segmentRW = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var segmentBufferRO = segmentRW.ValueRO.Buffer;
            var segmentBufferRW = segmentRW.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit))
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                var mapData = SystemAPI.GetSingleton<GeneratedMapData>();
                var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();

                var inAttackRange = SystemAPI.GetComponent<InAttackRange>(selectedUnit);
                if (inAttackRange.Coords.Count!=0)
                {
                    var mapDataRef = SystemAPI.GetSingletonRW<GeneratedMapData>();
                    var floors = mapDataRef.ValueRO.FloorArray;

                    segmentBufferRW.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

                    const int numSegmentsPerField = 3;
                    var rot = quaternion.RotateX(math.PIHALF);
                    int bufferPosition = segmentBufferRO.Length;
                    segmentBufferRW.Length += inAttackRange.Coords.Count * numSegmentsPerField;
                    foreach (uint2 coord in inAttackRange.Coords)
                    {
                        int index = GameGrid.ToIndex(coord, mapSettings.Size);
                        if (floors[index]==EFloorType.Traversable)
                        {
                            float3 point = mapData.PositionArray[index];
                            Segments.Plot.Circle(segmentBufferRW, ref bufferPosition, numSegmentsPerField, 0.05f, point, rot);
                        }
                    }

                    Entity targettingEnemy = SystemAPI.GetComponent<TargettingEnemy>(selectedUnit);
                    if (targettingEnemy!=Entity.Null && SystemAPI.Exists(targettingEnemy))
                    {
                        var selectedLtw = SystemAPI.GetComponent<LocalToWorld>(selectedUnit);
                        var enemyLtw = SystemAPI.GetComponent<LocalToWorld>(targettingEnemy);
                        segmentBufferRW.Length += 4;
                        Segments.Plot.Arrow(
                            segmentBufferRW, ref bufferPosition,
                            selectedLtw.Position + new float3(0, 2, 0),
                            enemyLtw.Position + new float3(0, 2, 0),
                            playerInput.PointerRay.origin
                        );
                    }
                }
                else if(segmentBufferRO.Length!=0)
                {
                    segmentBufferRW.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
            }
            else if(segmentBufferRO.Length!=0)
            {
                segmentBufferRW.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }
        }
    }
}
