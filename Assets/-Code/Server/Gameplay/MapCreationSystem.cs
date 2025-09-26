using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient.Gameplay;
using ServerAndClient;

using Random = Unity.Mathematics.Random;

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

            state.RequireForUpdate<PrefabSystem.Prefabs>();
            state.RequireForUpdate<CreateMapRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Entity singletonEntity = SystemAPI.GetSingletonEntity<CreateMapRequest>();
            var singleton = SystemAPI.GetSingleton<CreateMapRequest>();
            Debug.Log($"{DebugName}: {CreateMapRequest.DebugName} found, creating a map...");

            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // clear existing map:
            state.Dependency = new DestroyExistingMapJob{
                ECBPW = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);

            // regenerate map:
            {
                var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
                state.Dependency = JobHandle.CombineDependencies(state.Dependency, prefabs.Dependency);
                FixedString64Bytes cube_prefab_key = "cube";
                if (prefabs.Lookup.TryGetValue(cube_prefab_key, out Entity cubePrefab))
                {
                    if (state.EntityManager.Exists(cubePrefab))
                    {
                        state.Dependency = new MapCreationJob{
                            Settings        = singleton.Settings,
                            ECB             = ecb,
                            CubePrefab      = cubePrefab
                        }.Schedule(state.Dependency);

                        prefabs.Dependency = state.Dependency;
                        SystemAPI.SetSingleton(prefabs);
                    }
                    else
                    {
                        Debug.LogError($"Prefab '{cube_prefab_key}' {cubePrefab} does not exist anymore - can't complete map generation");
                    }
                }
                else
                {
                    Debug.LogError($"Prefab '{cube_prefab_key}' not found! Can't complete map generation");
                }
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

        [Unity.Burst.BurstCompile]
        partial struct MapCreationJob : IJob
        {
            public GameStartSettings Settings;
            public EntityCommandBuffer ECB;
            public Entity CubePrefab;
            void IJob.Execute()
            {
                int2 size = new int2(Settings.MapSize.x, Settings.MapSize.y);
                float3 origin = Settings.MapOffset;
                Random rnd = new (Settings.Seed);
                float2 elevOrigin = rnd.NextFloat2();

                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    float elev = noise.srnoise(elevOrigin + new float2(x, y)*0.1f);

                    Entity e = ECB.Instantiate(CubePrefab);
                    ECB.AddComponent<IsGeneratedMapContent>(e);
                    ECB.RemoveComponent<LocalTransform>(e);
                    ECB.SetComponent(e, new LocalToWorld{
                        Value = float4x4.TRS(origin + new float3(x, elev, y) + new float3(0.5f, 0, 0.5f), quaternion.identity, 1)
                    });
                }

                Debug.Log($"Map generation completed");
            }
        }

        [WithAll(typeof(IsGeneratedMapContent))]
        [Unity.Burst.BurstCompile]
        partial struct DestroyExistingMapJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int indexInQuery)
            {
                ECBPW.DestroyEntity(indexInQuery, entity);
            }
        }

        struct IsGeneratedMapContent : IComponentData {}

        public static FixedString64Bytes DebugName {get;} = nameof(MapCreationSystem);
    }
}
