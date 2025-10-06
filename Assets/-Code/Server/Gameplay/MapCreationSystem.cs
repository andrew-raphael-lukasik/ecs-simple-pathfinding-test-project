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
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct MapCreationSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(MapCreationSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabSystem.Prefabs>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GenerateMapEntitiesRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var prefabsRef = SystemAPI.GetSingletonRW<PrefabSystem.Prefabs>();
            prefabsRef.ValueRW.Dependency.Complete();
            var lookupRO = prefabsRef.ValueRO.Lookup;
            if (
                    !GetPrefabSafe("floor-traversable", lookupRO, state.EntityManager, out Entity prefabTraversable)
                ||  !GetPrefabSafe("floor-obstacle", lookupRO, state.EntityManager, out Entity prefabObstacle)
                ||  !GetPrefabSafe("floor-cover", lookupRO, state.EntityManager, out Entity prefabCover)
                ||  !GetPrefabSafe("player-unit", lookupRO, state.EntityManager, out Entity prefabPlayer)
                ||  !GetPrefabSafe("enemy-unit", lookupRO, state.EntityManager, out Entity prefabEnemy)
            )
            {
                return;
            }

            Entity mapSettingsSingleton = SystemAPI.GetSingletonEntity<MapSettingsSingleton>();
            var mapSettings = SystemAPI.GetComponent<MapSettingsSingleton>(mapSettingsSingleton);
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            Debug.Log($"{DebugName}: {GenerateMapEntitiesRequest.DebugName} {mapSettingsSingleton} found, creating a map. Map size: {mapSettings.Size}");

            // destroy request:
            state.EntityManager.RemoveComponent<GenerateMapEntitiesRequest>(mapSettingsSingleton);

            // clear existing map:
            {
                new DestroyExistingMapEntitiesJob{
                    ECBPW = ecb.AsParallelWriter(),
                }.Schedule(state.Dependency).Complete();

                if (SystemAPI.TryGetSingletonEntity<GeneratedMapData>(out Entity mapDataEntity))
                {
                    var mapData = SystemAPI.GetComponent<GeneratedMapData>(mapDataEntity);
                    
                    new DeallocateNativeArrayJob<EFloorType>(mapData.FloorArray)
                        .Schedule(state.Dependency);
                    new DeallocateNativeArrayJob<float3>(mapData.PositionArray)
                        .Schedule(state.Dependency);

                    state.EntityManager.DestroyEntity(mapDataEntity);
                }
            }

            // regenerate map:
            {
                int numMapCells = (int)(mapSettings.Size.x * mapSettings.Size.y);
                Entity mapDataEntity = state.EntityManager.CreateSingleton(new GeneratedMapData{
                    PositionArray = new (numMapCells, Allocator.Persistent),
                    FloorArray = new (numMapCells, Allocator.Persistent),
                });
                var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

                state.Dependency = new GenerateMapDataJob{
                    MapSettings = mapSettings,
                    PositionArray = mapData.PositionArray,
                    FloorArray = mapData.FloorArray,
                }.Schedule(state.Dependency);

                state.Dependency = new InstantiateMapCellsJob{
                    MapSettings = mapSettings,
                    ECB = ecb,
                    PrefabTraversable = prefabTraversable,
                    PrefabCover = prefabCover,
                    PrefabObstacle = prefabObstacle,
                    PositionArray = mapData.PositionArray,
                    FloorArray = mapData.FloorArray,
                }.Schedule(state.Dependency);

                state.Dependency = new InstantiateUnitsJob{
                    MapSettings = mapSettings,
                    ECB = ecb,
                    PrefabPlayer = prefabPlayer,
                    PrefabEnemy = prefabEnemy,
                    PositionArray = mapData.PositionArray,
                    FloorArray = mapData.FloorArray,
                }.Schedule(state.Dependency);
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
            public MapSettingsSingleton MapSettings;
            [WriteOnly] public NativeArray<float3> PositionArray;
            public NativeArray<EFloorType> FloorArray;
            void IJob.Execute()
            {
                Assert.IsTrue(MapSettings.Size.x>0);
                Assert.IsTrue(MapSettings.Size.y>0);
                Assert.IsTrue(MapSettings.Seed>0);

                uint2 size = MapSettings.Size;
                float3 origin = MapSettings.Origin;
                Random rnd = new (math.max(MapSettings.Seed, 1));
                float2 elevOrigin = rnd.NextFloat2();

                for (uint y = 0; y < size.y; y++)
                for (uint x = 0; x < size.x; x++)
                {
                    int i = GameGrid.ToIndex(x, y, size);

                    float elevation = noise.srnoise(elevOrigin + new float2(x, y)*0.1f);
                    float3 pos = origin + new float3(x, elevation, y)*MapSettingsSingleton.CellSize + new float3(0.5f, 0, 0.5f)*MapSettingsSingleton.CellSize;
                    PositionArray[i] = pos;
                }

                uint mapCellArea = size.x * size.y;
                for (int i = 0; i < mapCellArea; i++)
                {
                    FloorArray[i] = EFloorType.Traversable;
                }

                {
                    uint min = mapCellArea / 10;
                    uint dst = rnd.NextUInt(min, min*2);
                    uint instances = 0;
                    uint attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int i = (int) rnd.NextUInt(0, mapCellArea);
                        if (FloorArray[i]==EFloorType.Traversable)
                        {
                            FloorArray[i] = EFloorType.Obstacle;
                            instances++;
                        }
                    }
                }

                {
                    uint min = mapCellArea / 20;
                    uint dst = rnd.NextUInt(min, min*2);
                    uint instances = 0;
                    uint attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        int i = (int) rnd.NextUInt(0, mapCellArea);
                        if (FloorArray[i]==EFloorType.Traversable)
                        {
                            FloorArray[i] = EFloorType.Cover;
                            instances++;
                        }
                    }
                }
            }
        }

        [Unity.Burst.BurstCompile]
        partial struct InstantiateMapCellsJob : IJob
        {
            public MapSettingsSingleton MapSettings;
            public EntityCommandBuffer ECB;
            public Entity PrefabTraversable, PrefabCover, PrefabObstacle;
            [ReadOnly] public NativeArray<float3> PositionArray;
            [ReadOnly] public NativeArray<EFloorType> FloorArray;
            void IJob.Execute()
            {
                uint2 size = MapSettings.Size;

                for (uint y = 0; y < size.y; y++)
                for (uint x = 0; x < size.x; x++)
                {
                    uint2 coord = new uint2(x, y);
                    int i = GameGrid.ToIndex(coord, size);

                    Entity prefab;
                    switch (FloorArray[i])
                    {
                        case EFloorType.Traversable: prefab = PrefabTraversable; break;
                        case EFloorType.Obstacle: prefab = PrefabObstacle; break;
                        case EFloorType.Cover: prefab = PrefabCover; break;
                        default: throw new System.NotImplementedException($"implement: {FloorArray[i]}");
                    }

                    Entity e = ECB.Instantiate(prefab);
                    ECB.AddComponent(e, new FloorCoord{
                        Value = coord
                    });

                    float azimuth = x * math.PIHALF + y * math.PIHALF;
                    float3 pos = PositionArray[i];

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
            public MapSettingsSingleton MapSettings;
            public EntityCommandBuffer ECB;
            public Entity PrefabPlayer, PrefabEnemy;
            [ReadOnly] public NativeArray<float3> PositionArray;
            [ReadOnly] public NativeArray<EFloorType> FloorArray;
            void IJob.Execute()
            {
                Assert.IsTrue(MapSettings.Size.x>0);
                Assert.IsTrue(MapSettings.Size.y>0);
                Assert.IsTrue(MapSettings.Seed>0);

                uint2 size = MapSettings.Size;
                float3 origin = MapSettings.Origin;
                uint mapCellArea = size.x * size.y;
                Random rnd = new (math.max(MapSettings.Seed, 1));

                NativeHashSet<uint2> takenUnitCoords = new ((int)(MapSettings.NumPlayerUnits + MapSettings.NumEnemyUnits), Allocator.Temp);
                // instantiate player units:
                {
                    uint dst = MapSettings.NumPlayerUnits;
                    uint instances = 0;
                    uint attempts = 0;
                    for (; instances < dst && attempts<mapCellArea*2; attempts++)
                    {
                        uint2 coord = rnd.NextUInt2(0, size);
                        int i = GameGrid.ToIndex(coord, size);
                        if (FloorArray[i]==EFloorType.Traversable)
                        if (!takenUnitCoords.Contains(coord))
                        {
                            Entity e = ECB.Instantiate(PrefabPlayer);
                            ECB.AddComponent(e, new UnitCoord{
                                Value = coord,
                            });

                            float azimuth = coord.x * math.PIHALF + coord.y * math.PIHALF;
                            float3 pos = PositionArray[i];
                            ECB.SetComponent(e, new LocalTransform{
                                Position = pos,
                                Rotation = quaternion.RotateY(azimuth),
                                Scale = 1,
                            });

                            takenUnitCoords.Add(coord);
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
                        uint2 coord = rnd.NextUInt2(0, size);
                        int i = GameGrid.ToIndex(coord, size);
                        if (FloorArray[i]==EFloorType.Traversable)
                        if (!takenUnitCoords.Contains(coord))
                        {
                            Entity e = ECB.Instantiate(PrefabEnemy);
                            ECB.AddComponent(e, new UnitCoord{
                                Value = coord
                            });

                            float azimuth = coord.x * math.PIHALF + coord.y * math.PIHALF;
                            float3 pos = PositionArray[i];
                            ECB.SetComponent(e, new LocalTransform{
                                Position = pos,
                                Rotation = quaternion.RotateY(azimuth),
                                Scale = 1,
                            });

                            takenUnitCoords.Add(coord);
                            instances++;
                        }
                    }
                }

                Debug.Log($"Units instantiated completed");
            }
        }

        [WithAny(typeof(FloorCoord), typeof(UnitCoord))]
        [Unity.Burst.BurstCompile]
        partial struct DestroyExistingMapEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.DestroyEntity(index, entity);
            }
        }

    }
}
