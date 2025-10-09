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
        NativeHashSet<uint2> _reachable;
        Entity _reachable_owner;
        GameNavigation.MoveReachJob _reachable_job;
        JobHandle _reachable_dependency;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<SelectedUnitSingleton>();

            _reachable = new (64, Allocator.Persistent);

            var lineMat = Resources.Load<Material>("game-move-range-lines");
            Segments.Core.Create(out _segments, lineMat);
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

                var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                var buffer = segmentRef.ValueRW.Buffer;

                if (_reachable.Count!=0)
                {
                    buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, em);

                    const int numSegmentsPerField = 3;
                    var rot = quaternion.RotateX(math.PIHALF);
                    int bufferPosition = buffer.Length;
                    buffer.Length += _reachable.Count * numSegmentsPerField;
                    foreach (uint2 coord in _reachable)
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

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (_reachable_owner!=selectedUnit)
            {
                if (selectedUnit!=Entity.Null && em.Exists(selectedUnit))
                {
                    ushort moveRange = em.GetComponentData<MoveRange>(selectedUnit);
                    uint2 coord = em.GetComponentData<UnitCoord>(selectedUnit);

                    _reachable_job = new GameNavigation.MoveReachJob(
                        start: coord,
                        range: moveRange,
                        moveCost: mapData.FloorArray,
                        mapSize: mapSettings.Size,
                        reachable: _reachable
                    );
                    _reachable_dependency = _reachable_job.Schedule(state.Dependency);
                    _reachable_owner = selectedUnit;
                }
                else
                {
                    _reachable_owner = Entity.Null;
                    var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                    segmentRef.ValueRW.Buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
            }
        }
    }
}
