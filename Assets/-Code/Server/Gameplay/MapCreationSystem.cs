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
            _segment = Segments.Core.GetSegment(_segmentEntity, state.EntityManager);

            state.RequireForUpdate<PrefabSystem.Prefabs>();
            state.RequireForUpdate<GenerateMapEntitiesRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Entity mapSettingsSingleton = SystemAPI.GetSingletonEntity<MapSettingsData>();
            Debug.Log($"{DebugName}: {GenerateMapEntitiesRequest.DebugName} {mapSettingsSingleton} found, creating a map...");
            var request = SystemAPI.GetSingleton<GenerateMapEntitiesRequest>();
            var mapSettings = SystemAPI.GetComponent<MapSettingsData>(mapSettingsSingleton);
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // destroy request:
            state.EntityManager.RemoveComponent<GenerateMapEntitiesRequest>(mapSettingsSingleton);

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
                new DeallocateNativeArrayJob<float3>(data.Position)
                    .Schedule(state.Dependency);

                state.EntityManager.DestroyEntity(dataEntity);
            }

            // regenerate map:
            {
                int numMapCells = mapSettings.Size.x * mapSettings.Size.y;
                Entity mapDataEntity = state.EntityManager.CreateSingleton(new GeneratedMapData{
                    Position = new (numMapCells, Allocator.Persistent),
                    Cell = new (numMapCells, Allocator.Persistent),
                });
                var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

                var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
                state.Dependency = JobHandle.CombineDependencies(state.Dependency, prefabs.Dependency);

                if (!GetPrefabSafe("cell-traversable", prefabs.Lookup, state.EntityManager, out Entity prefabTraversable)) goto map_generation_canceled;
                if (!GetPrefabSafe("cell-obstacle", prefabs.Lookup, state.EntityManager, out Entity prefabObstacle)) goto map_generation_canceled;
                if (!GetPrefabSafe("cell-cover", prefabs.Lookup, state.EntityManager, out Entity prefabCover)) goto map_generation_canceled;
                if (!GetPrefabSafe("player-unit", prefabs.Lookup, state.EntityManager, out Entity prefabPlayer)) goto map_generation_canceled;
                if (!GetPrefabSafe("enemy-unit", prefabs.Lookup, state.EntityManager, out Entity prefabEnemy)) goto map_generation_canceled;

                state.Dependency = new GenerateMapDataJob{
                    Settings = mapSettings,
                    PositionData = mapData.Position,
                    CellData = mapData.Cell,
                }.Schedule(state.Dependency);

                state.Dependency = new InstantiateMapCellsJob{
                    MapSettings = mapSettings,
                    ECB = ecb,
                    PrefabTraversable = prefabTraversable,
                    PrefabCover = prefabCover,
                    PrefabObstacle = prefabObstacle,
                    PositionData = mapData.Position,
                    CellData = mapData.Cell,
                }.Schedule(state.Dependency);

                state.Dependency = new InstantiateUnitsJob{
                    MapSettings = mapSettings,
                    ECB = ecb,
                    PrefabPlayer = prefabPlayer,
                    PrefabEnemy = prefabEnemy,
                    PositionData = mapData.Position,
                    CellData = mapData.Cell,
                }.Schedule(state.Dependency);

                prefabs.Dependency = state.Dependency;
                SystemAPI.SetSingleton(prefabs);
            }
            map_generation_canceled:

            // draw map boundaries
            if (Application.isPlaying)// editor-only
            {
                Vector2Int size = mapSettings.Size;
                Vector3 offset = mapSettings.Offset;
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

        bool GetPrefabSafe(FixedString64Bytes key, NativeHashMap<FixedString64Bytes, Entity> lookup, EntityManager entityManager, out Entity prefab)
        {
            if (!lookup.TryGetValue(key, out prefab))
            {
                Debug.LogError($"Prefab '{key}' not found! Can't complete map generation");
                return false;
            }
            if (!entityManager.Exists(prefab))
            {
                Debug.LogError($"Prefab '{key}' {prefab} does not exist anymore - can't complete map generation");
                return false;
            }

            Assert.IsTrue(entityManager.Exists(prefab));
            return true;
        }

        [Unity.Burst.BurstCompile]
        partial struct GenerateMapDataJob : IJob
        {
            public MapSettingsData Settings;
            [WriteOnly] public NativeArray<float3> PositionData;
            public NativeArray<EMapCell> CellData;
            void IJob.Execute()
            {
                Assert.IsTrue(Settings.Size.x>0);
                Assert.IsTrue(Settings.Size.y>0);
                Assert.IsTrue(Settings.Seed>0);

                int2 size = new int2(Settings.Size.x, Settings.Size.y);
                float3 origin = Settings.Offset;
                Random rnd = new (math.max(Settings.Seed, 1));
                float2 elevOrigin = rnd.NextFloat2();

                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    int i = y * Settings.Size.x + x;

                    float elevation = noise.srnoise(elevOrigin + new float2(x, y)*0.1f);
                    float3 pos = origin + new float3(x, elevation, y) + new float3(0.5f, 0, 0.5f);
                    PositionData[i] = pos;
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
        partial struct InstantiateMapCellsJob : IJob
        {
            public MapSettingsData MapSettings;
            public EntityCommandBuffer ECB;
            public Entity PrefabTraversable, PrefabCover, PrefabObstacle;
            [ReadOnly] public NativeArray<float3> PositionData;
            [ReadOnly] public NativeArray<EMapCell> CellData;
            void IJob.Execute()
            {
                int2 size = new int2(MapSettings.Size.x, MapSettings.Size.y);

                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    int i = y * MapSettings.Size.x + x;

                    Entity prefab;
                    switch (CellData[i])
                    {
                        case EMapCell.Traversable: prefab = PrefabTraversable; break;
                        case EMapCell.Obstacle: prefab = PrefabObstacle; break;
                        case EMapCell.Cover: prefab = PrefabCover; break;
                        default: throw new System.NotImplementedException($"implement: {CellData[i]}");
                    }

                    Entity e = ECB.Instantiate(prefab);

                    ECB.AddComponent(e, new CellCoords{
                        X = (ushort)x,
                        Y = (ushort)y,
                    });

                    float azimuth = x * math.PIHALF + y * math.PIHALF;
                    float3 pos = PositionData[i];

                    ECB.SetComponent(e, new LocalToWorld{
                        Value = float4x4.TRS(pos, quaternion.RotateY(azimuth), 1)
                    });
                    ECB.RemoveComponent<LocalTransform>(e);
                }

                Debug.Log($"Map generation completed");
            }
        }

        [Unity.Burst.BurstCompile]
        partial struct InstantiateUnitsJob : IJob
        {
            public MapSettingsData MapSettings;
            public EntityCommandBuffer ECB;
            public Entity PrefabPlayer, PrefabEnemy;
            [ReadOnly] public NativeArray<float3> PositionData;
            [ReadOnly] public NativeArray<EMapCell> CellData;
            void IJob.Execute()
            {
                Assert.IsTrue(MapSettings.Size.x>0);
                Assert.IsTrue(MapSettings.Size.y>0);
                Assert.IsTrue(MapSettings.Seed>0);

                int2 size = new int2(MapSettings.Size.x, MapSettings.Size.y);
                float3 origin = MapSettings.Offset;
                int mapCellArea = size.x * size.y;
                Random rnd = new (math.max(MapSettings.Seed, 1));

                // instantiate player units:
                {
                    uint dst = MapSettings.NumPlayerUnits;
                    int instances = 0;
                    int attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int2 coord = rnd.NextInt2(0, size);
                        int i = coord.y * size.x + coord.x;
                        if (CellData[i]==EMapCell.Traversable)
                        {
                            Entity e = ECB.Instantiate(PrefabPlayer);
                            ECB.AddComponent(e, new CellCoords{
                                X = (ushort) coord.x,
                                Y = (ushort) coord.y,
                            });

                            float azimuth = coord.x * math.PIHALF + coord.y * math.PIHALF;
                            float3 pos = PositionData[i];
                            ECB.SetComponent(e, new LocalTransform{
                                Position = pos,
                                Rotation = quaternion.RotateY(azimuth),
                                Scale = 1,
                            });

                            instances++;
                        }
                    }
                }

                // instantiate enemy units:
                {
                    uint dst = MapSettings.NumEnemyUnits;
                    int instances = 0;
                    int attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int2 coord = rnd.NextInt2(0, size);
                        int i = coord.y * size.x + coord.x;
                        if (CellData[i]==EMapCell.Traversable)
                        {
                            Entity e = ECB.Instantiate(PrefabEnemy);
                            ECB.AddComponent(e, new CellCoords{
                                X = (ushort) coord.x,
                                Y = (ushort) coord.y,
                            });

                            float azimuth = coord.x * math.PIHALF + coord.y * math.PIHALF;
                            float3 pos = PositionData[i];
                            ECB.SetComponent(e, new LocalTransform{
                                Position = pos,
                                Rotation = quaternion.RotateY(azimuth),
                                Scale = 1,
                            });

                            instances++;
                        }
                    }
                }

                Debug.Log($"units instantiated completed");
            }
        }

        [WithAll(typeof(CellCoords))]
        [Unity.Burst.BurstCompile]
        partial struct DestroyExistingMapCellEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.DestroyEntity(index, entity);
            }
        }

        struct CellCoords : IComponentData
        {
            public ushort X, Y;
        }

        public static FixedString64Bytes DebugName {get;} = nameof(MapCreationSystem);
    }
}
