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
        Entity _solution_owner;
        GameNavigation.MoveReachJob _solution_job;
        JobHandle _solution_dependency;

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
            _solution_dependency.Complete();
            _solution_job.Dispose();
            if (_reachable.IsCreated) _reachable.Dispose();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            if (_solution_owner!=Entity.Null && em.Exists(_solution_owner))
            if (_solution_dependency.IsCompleted)
            {
                _solution_dependency.Complete();
                _solution_job.Dispose();

                var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                var buffer = segmentRef.ValueRW.Buffer;
                bool preExistingLines = buffer.Length!=0;
                buffer.Length = 0;

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

                if (buffer.Length!=0 || preExistingLines)
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (_solution_owner!=selectedUnit)
            {
                if (selectedUnit!=Entity.Null && em.Exists(selectedUnit))
                {
                    ushort moveRange = em.GetComponentData<MoveRange>(selectedUnit);
                    uint2 coord = em.GetComponentData<UnitCoord>(selectedUnit);

                    _solution_job = new GameNavigation.MoveReachJob(
                        start: coord,
                        range: moveRange,
                        moveCost: mapData.FloorArray,
                        mapSize: mapSettings.Size,
                        reachable: _reachable
                    );
                    _solution_dependency = _solution_job.Schedule(state.Dependency);
                    _solution_owner = selectedUnit;
                }
                else
                {
                    _solution_owner = Entity.Null;
                    var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
                    segmentRef.ValueRW.Buffer.Length = 0;
                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
            }
        }
    }
}
