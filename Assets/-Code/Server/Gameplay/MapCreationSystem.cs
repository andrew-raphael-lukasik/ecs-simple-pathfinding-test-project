using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient.Gameplay;
using ServerAndClient;

using Random = Unity.Mathematics.Random;
using Assert = UnityEngine.Assertions.Assert;

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
            Debug.Log($"{DebugName}: {CreateMapRequest.DebugName} {singletonEntity} found, creating a map...");
            var request = SystemAPI.GetSingleton<CreateMapRequest>();
            var settings = request.Settings;
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // destroy request:
            state.EntityManager.DestroyEntity(singletonEntity);

            // clear existing map:
            {
                state.Dependency = new DestroyExistingMapCellEntitiesJob{
                    ECBPW = ecb.AsParallelWriter(),
                }.Schedule(state.Dependency);
            }
            if (SystemAPI.TryGetSingletonEntity<GeneratedMapData>(out Entity dataEntity))
            {
                var data = SystemAPI.GetComponent<GeneratedMapData>(dataEntity);
                
                new DeallocateNativeArrayJob<EMapCell>(data.Cell)
                    .Schedule(state.Dependency);
                new DeallocateNativeArrayJob<float>(data.Elevation)
                    .Schedule(state.Dependency);
                
                state.EntityManager.DestroyEntity(dataEntity);
            }

            // regenerate map:
            {
                int numMapCells = settings.MapSize.x * settings.MapSize.y;
                Entity mapDataEntity = state.EntityManager.CreateSingleton(new GeneratedMapData{
                    Elevation = new (numMapCells, Allocator.Persistent),
                    Cell = new (numMapCells, Allocator.Persistent),
                });
                var mapContent = state.EntityManager.AddBuffer<CellContentData>(mapDataEntity);
                var mapData = SystemAPI.GetSingleton<GeneratedMapData>();
                var mapContant = state.EntityManager.AddBuffer<CellContentData>(mapDataEntity);

                var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
                state.Dependency = JobHandle.CombineDependencies(state.Dependency, prefabs.Dependency);
                
                Entity prefabTraversable;
                {
                    FixedString64Bytes key = "cell-traversable";
                    if (!prefabs.Lookup.TryGetValue(key, out prefabTraversable))
                    {
                        Debug.LogError($"Prefab '{key}' not found! Can't complete map generation");
                        goto map_generation_canceled;
                    }
                    if (!state.EntityManager.Exists(prefabTraversable))
                    {
                        Debug.LogError($"Prefab '{key}' {prefabTraversable} does not exist anymore - can't complete map generation");
                        goto map_generation_canceled;
                    }
                }
                Assert.IsTrue(state.EntityManager.Exists(prefabTraversable));

                Entity prefabObstacle;
                {
                    FixedString64Bytes key = "cell-obstacle";
                    if (!prefabs.Lookup.TryGetValue(key, out prefabObstacle))
                    {
                        Debug.LogError($"Prefab '{key}' not found! Can't complete map generation");
                        goto map_generation_canceled;
                    }
                    if (!state.EntityManager.Exists(prefabObstacle))
                    {
                        Debug.LogError($"Prefab '{key}' {prefabObstacle} does not exist anymore - can't complete map generation");
                        goto map_generation_canceled;
                    }
                }
                Assert.IsTrue(state.EntityManager.Exists(prefabObstacle));

                Entity prefabCover;
                {
                    FixedString64Bytes key = "cell-cover";
                    if (!prefabs.Lookup.TryGetValue(key, out prefabCover))
                    {
                        Debug.LogError($"Prefab '{key}' not found! Can't complete map generation");
                        goto map_generation_canceled;
                    }
                    if (!state.EntityManager.Exists(prefabCover))
                    {
                        Debug.LogError($"Prefab '{key}' {prefabCover} does not exist anymore - can't complete map generation");
                        goto map_generation_canceled;
                    }
                }
                Assert.IsTrue(state.EntityManager.Exists(prefabCover));

                state.Dependency = new GenerateMapDataJob{
                    Settings = settings,
                    ElevationData = mapData.Elevation,
                    CellData = mapData.Cell,
                }.Schedule(state.Dependency);

                state.Dependency = new InstantiatePrefabsJob{
                    Settings = settings,
                    ECB = ecb,
                    PrefabTraversable = prefabTraversable,
                    PrefabCover = prefabCover,
                    PrefabObstacle = prefabObstacle,
                    ElevationData = mapData.Elevation,
                    CellData = mapData.Cell,
                    ContentData = mapContant.Reinterpret<Entity>(),
                }.Schedule(state.Dependency);

                prefabs.Dependency = state.Dependency;
                SystemAPI.SetSingleton(prefabs);
            }
            map_generation_canceled:

            // draw map boundaries
            {
                Vector2Int size = settings.MapSize;
                Vector3 offset = settings.MapOffset;
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
        }

        [Unity.Burst.BurstCompile]
        partial struct GenerateMapDataJob : IJob
        {
            public GameStartSettings Settings;
            [WriteOnly] public NativeArray<float> ElevationData;
            public NativeArray<EMapCell> CellData;
            void IJob.Execute()
            {
                Assert.IsTrue(Settings.MapSize.x>0);
                Assert.IsTrue(Settings.MapSize.y>0);
                Assert.IsTrue(Settings.Seed>0);

                int2 size = new int2(Settings.MapSize.x, Settings.MapSize.y);
                Random rnd = new (math.max(Settings.Seed, 1));
                float2 elevOrigin = rnd.NextFloat2();

                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    int i = y * Settings.MapSize.x + x;

                    float elev = noise.srnoise(elevOrigin + new float2(x, y)*0.1f);
                    ElevationData[i] = elev;
                }

                int mapCellArea = size.x * size.y;
                for (int i = 0; i < mapCellArea; i++)
                {
                    CellData[i] = EMapCell.Traversable;
                }

                {
                    int min = mapCellArea / 10;
                    int dst = rnd.NextInt(min, min*2);
                    int instances = 0;
                    int attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int i = rnd.NextInt(0, mapCellArea);
                        if (CellData[i]==EMapCell.Traversable)
                        {
                            CellData[i] = EMapCell.Obstacle;
                            instances++;
                        }
                    }
                }

                {
                    int min = mapCellArea / 20;
                    int dst = rnd.NextInt(min, min*2);
                    int instances = 0;
                    int attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int i = rnd.NextInt(0, mapCellArea);
                        if (CellData[i]==EMapCell.Traversable)
                        {
                            CellData[i] = EMapCell.Cover;
                            instances++;
                        }
                    }
                }
            }
        }

        [Unity.Burst.BurstCompile]
        partial struct InstantiatePrefabsJob : IJob
        {
            public GameStartSettings Settings;
            public EntityCommandBuffer ECB;
            public Entity PrefabTraversable, PrefabCover, PrefabObstacle;
            [ReadOnly] public NativeArray<float> ElevationData;
            [ReadOnly] public NativeArray<EMapCell> CellData;
            [WriteOnly] public DynamicBuffer<Entity> ContentData;
            void IJob.Execute()
            {
                int2 size = new int2(Settings.MapSize.x, Settings.MapSize.y);
                float3 origin = Settings.MapOffset;

                ContentData.Length = size.x * size.y;

                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    int i = y * Settings.MapSize.x + x;

                    Entity prefab;
                    switch (CellData[i])
                    {
                        case EMapCell.Traversable: prefab = PrefabTraversable; break;
                        case EMapCell.Obstacle: prefab = PrefabObstacle; break;
                        case EMapCell.Cover: prefab = PrefabCover; break;
                        default: throw new System.NotImplementedException($"implement: {CellData[i]}");
                    }

                    Entity e = ECB.Instantiate(prefab);
                    ContentData[i] = e;

                    ECB.AddComponent<IsCellEntity>(e);
                    ECB.AddComponent(e, new CellCoords{
                        X = (ushort)x,
                        Y = (ushort)y,
                    });

                    Random rnd = new ((uint)i+1);
                    // float azimuth = rnd.NextInt(0, 4) * math.PIHALF;
                    float azimuth = x * math.PIHALF + y * math.PIHALF;
                    float elevation = ElevationData[i];

                    ECB.SetComponent(e, new LocalToWorld{
                        Value = float4x4.TRS(
                            origin + new float3(x, elevation, y) + new float3(0.5f, 0, 0.5f),
                            quaternion.RotateY(azimuth),
                            1
                        )
                    });
                    ECB.RemoveComponent<LocalTransform>(e);
                }

                Debug.Log($"Map generation completed");
            }
        }

        [WithAll(typeof(IsCellEntity))]
        [Unity.Burst.BurstCompile]
        partial struct DestroyExistingMapCellEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.DestroyEntity(index, entity);
            }
        }

        public struct GeneratedMapData : IComponentData
        {
            public NativeArray<float> Elevation;
            public NativeArray<EMapCell> Cell;
        }

        [InternalBufferCapacity(0)]
        public struct CellContentData : IBufferElementData
        {
            public Entity Value;
        }

        public enum EMapCell : byte
        {
            Traversable,
            Obstacle,
            Cover
        }

        struct IsCellEntity : IComponentData {}
        struct CellCoords : IComponentData
        {
            public ushort X, Y;
        }

        public static FixedString64Bytes DebugName {get;} = nameof(MapCreationSystem);
    }
}
