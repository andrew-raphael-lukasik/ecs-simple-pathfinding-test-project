using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient.Gameplay;
using ServerAndClient;

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

            // generate map:
            {
                var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
                
                var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
                state.Dependency = JobHandle.CombineDependencies(state.Dependency, prefabs.Dependency);

                state.Dependency = new MapCreationJob{
                    Settings        = singleton.Settings,
                    CommandBuffer   = commandBuffer,
                    LookupPrefabs   = prefabs.Registry,
                }.Schedule(state.Dependency);

                var ecbss = SystemAPI.GetSingleton<EndInitializationECBSystem.Singleton>();
                ecbss.Append(commandBuffer, state.Dependency);

                prefabs.Dependency = state.Dependency;
                SystemAPI.SetSingleton(prefabs);
            }
            
            // draw map boundaries
            {
                Vector2Int size = singleton.Settings.MapSize;
                Vector3 offset = singleton.Settings.MapOffset;
                int _ = 0;
                _segment.Buffer.Length = 12;
                state.Dependency = new Segments.Plot.BoxJob(
                    segments:   _segment.Buffer.AsArray(),
                    index:      ref _,
                    size:       new float3(size.x, 0, size.y),
                    pos:        offset + new Vector3(size.x, 0, size.y)/2,
                    rot:        quaternion.identity
                ).Schedule(state.Dependency);

                _segment.Dependency.Value = JobHandle.CombineDependencies(_segment.Dependency.Value, state.Dependency);
                Segments.Core.SetSegmentChanged(_segmentEntity, state.EntityManager);
            }

            state.EntityManager.DestroyEntity(singletonEntity);
        }
    }

    partial struct MapCreationJob : IJob
    {
        public GameStartSettings Settings;
        public EntityCommandBuffer CommandBuffer;
        [ReadOnly] public NativeHashMap<FixedString64Bytes, Entity> LookupPrefabs;
        void IJob.Execute()
        {
            int2 size = new int2(Settings.MapSize.x, Settings.MapSize.y);
            float3 origin = Settings.MapOffset;
            const string key = "cube";
            if (LookupPrefabs.TryGetValue(key, out Entity prefab))
            {
                
                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    Entity e = CommandBuffer.Instantiate(prefab);
                    float elev = noise.srnoise(new float2(x, y)*0.1f);

                    CommandBuffer.RemoveComponent<LocalTransform>(e);
                    CommandBuffer.SetComponent(e, new LocalToWorld{
                        Value = float4x4.TRS(origin + new float3(x, elev, y) + new float3(0.5f, 0, 0.5f), quaternion.identity, 1)
                    });
                }

                Debug.Log($"map generation completed");
            }
            else
            {
                Debug.LogError($"prefab '{key}' not found! Can't complete map generation");
            }
        }
    }
}
