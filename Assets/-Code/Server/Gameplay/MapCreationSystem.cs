using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct MapCreationSystem : ISystem
    {
        Entity _segmentEntity;
        Segments.Segment _segment;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            Segments.Core.Create(out _segmentEntity);
            _segment = Segments.Core.GetSegment(_segmentEntity);

            state.RequireForUpdate<CreateMapRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Entity singletonEntity = SystemAPI.GetSingletonEntity<CreateMapRequest>();
            var singleton = SystemAPI.GetSingleton<CreateMapRequest>();
            Debug.Log($"{state.DebugName}: {CreateMapRequest.DebugName} found, creating a map...");
            {
                state.Dependency = new MapCreationJob{
                    Settings = singleton.Settings,
                    SegmentBuffer = _segment.Buffer,
                }.Schedule(state.Dependency);

                _segment.Dependency.Value = JobHandle.CombineDependencies(_segment.Dependency.Value, state.Dependency);
                Segments.Core.SetSegmentChanged(_segmentEntity, state.EntityManager);
            }
            state.EntityManager.DestroyEntity(singletonEntity);
        }
    }

    partial struct MapCreationJob : IJob
    {
        public GameStartSettings Settings;
        public NativeList<float3x2> SegmentBuffer;
        void IJob.Execute()
        {
            Vector2Int size = Settings.MapSize;
            Vector3 offset = Settings.MapOffset;
            
            SegmentBuffer.Length = 12;
            Segments.Plot.Box(SegmentBuffer.AsArray().Slice(), size: new float3(size.x, 0, size.y), pos: offset, quaternion.identity);
        }
    }
}
