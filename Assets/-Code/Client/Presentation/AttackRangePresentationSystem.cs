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
        NativeHashSet<uint2> _reachable;
        Entity _reachable_owner;
        uint2 _reachable_coord;
        GameNavigation.AttackRangeJob _reachable_job;
        JobHandle _reachable_dependency;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            _reachable = new (64, Allocator.Persistent);

            var lineMat = Resources.Load<Material>("game-attack-range-lines");
            Segments.Core.Create(out _segments, lineMat);
            state.EntityManager.AddComponent<IsPlayStateOnly>(_segments);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            _reachable_dependency.Complete();
            _reachable_job.Dispose();
            if (_reachable.IsCreated) _reachable.Dispose();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            if (_reachable_owner!=Entity.Null && em.Exists(_reachable_owner))
            if (_reachable_dependency.IsCompleted)
            {
                _reachable_dependency.Complete();
                _reachable_job.Dispose();
                _reachable_owner = Entity.Null;
                _reachable_coord = 0;

                var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                var buffer = segmentRef.ValueRW.Buffer;

                if (_reachable.Count!=0)
                {
                    var mapDataRef = SystemAPI.GetSingletonRW<GeneratedMapData>();
                    var floors = mapDataRef.ValueRO.FloorArray;

                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, em);

                    const int numSegmentsPerField = 3;
                    var rot = quaternion.RotateX(math.PIHALF);
                    int bufferPosition = buffer.Length;
                    buffer.Length += _reachable.Count * numSegmentsPerField;
                    foreach (uint2 coord in _reachable)
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
                    Segments.Core.SetSegmentChanged(_segments, em);
                }
            }

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            uint2 selectedCoord = em.Exists(selectedUnit) && SystemAPI.HasComponent<UnitCoord>(selectedUnit)
                ? em.GetComponentData<UnitCoord>(selectedUnit)
                : uint.MaxValue;
            if (_reachable_owner!=selectedUnit || math.any(_reachable_coord!=selectedCoord))
            {
                if (selectedUnit!=Entity.Null && em.Exists(selectedUnit))
                {
                    ushort attackRange = em.GetComponentData<AttackRange>(selectedUnit);

                    _reachable_job = new GameNavigation.AttackRangeJob(
                        start: selectedCoord,
                        range: attackRange,
                        floor: mapData.FloorArray,
                        mapSize: mapSettings.Size,
                        reachable: _reachable
                    );
                    _reachable_dependency = _reachable_job.Schedule(state.Dependency);
                    _reachable_owner = selectedUnit;
                    _reachable_coord = selectedCoord;
                }
                else
                {
                    _reachable_owner = Entity.Null;
                    _reachable_coord = 0;

                    var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                    segmentRef.ValueRW.Buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
            }
        }
    }
}
