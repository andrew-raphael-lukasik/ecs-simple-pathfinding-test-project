using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Gameplay;

using Assert = UnityEngine.Assertions.Assert;

namespace Server.GameEdit
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct EditStateMapChangeSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(EditStateMapChangeSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<GameState.EDIT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
                {
                    int dstIndex = GameGrid.ToIndex(dstCoord, mapSettings.Size);
                    var em = state.EntityManager;
                    var mapDataRef = SystemAPI.GetSingletonRW<GeneratedMapData>();
                    var floorsRef = SystemAPI.GetSingletonRW<FloorsSingleton>();
                    var unitsRef = SystemAPI.GetSingletonRW<UnitsSingleton>();
                    Entity srcFloor = SystemAPI.GetSingleton<SelectedFloorSingleton>().Selected;
                    Entity srcUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>().Selected;

                    if (!(srcFloor!=Entity.Null && em.Exists(srcFloor)))
                    {
                        #if UNITY_EDITOR || DEBUG
                        Debug.Log($"{DebugName}: no src floor entity, swap won't happen");
                        #endif

                        return;
                    }

                    JobHandle.CompleteAll(
                        ref mapDataRef.ValueRW.Dependency,
                        ref floorsRef.ValueRW.Dependency,
                        ref unitsRef.ValueRW.Dependency
                    );

                    var floors = floorsRef.ValueRO.Lookup;
                    var units = unitsRef.ValueRO.Lookup;

                    #if UNITY_EDITOR || DEBUG
                    // Assert.IsTrue(em.Exists(srcFloor), $"{DebugName} can't execute swap action without a src floor entity - it is a source of srcCoord");
                    Assert.IsTrue(floors[dstIndex]!=Entity.Null, $"{DebugName} can't execute swap action without a floors lookup having an entity at {dstCoord}");
                    #endif

                    uint2 srcCoord = em.GetComponentData<FloorCoord>(srcFloor);
                    int srcIndex = GameGrid.ToIndex(srcCoord, mapSettings.Size);
                    if (math.all(srcCoord==dstCoord)) return;// ignore, src and dst are the same
                    Entity dstFloor = floors[dstIndex];

                    #if UNITY_EDITOR || DEBUG
                    if (em.Exists(srcFloor)) Assert.IsTrue(em.HasComponent<FloorCoord>(srcFloor), $"Floor {srcFloor} has no {FloorCoord.DebugName}");
                    if (em.Exists(srcUnit)) Assert.IsTrue(em.HasComponent<UnitCoord>(srcUnit), $"Unit {srcUnit} has no {UnitCoord.DebugName}");
                    #endif

                    LocalToWorld srcLtw = em.GetComponentData<LocalToWorld>(srcFloor);
                    LocalToWorld dstLtw = em.GetComponentData<LocalToWorld>(dstFloor);

                    // swap floors:
                    {
                        {
                            var floorTypesRW = mapDataRef.ValueRW.FloorArray;
                            EFloorType srcType = floorTypesRW[srcIndex];
                            floorTypesRW[srcIndex] = floorTypesRW[dstIndex];;
                            floorTypesRW[dstIndex] = srcType;
                        }

                        em.SetComponentData(srcFloor, dstLtw);
                        SystemAPI.SetComponentEnabled<IsFloorCoordValid>(srcFloor, false);

                        em.SetComponentData(dstFloor, srcLtw);
                        SystemAPI.SetComponentEnabled<IsFloorCoordValid>(dstFloor, false);

                        SystemAPI.SetSingleton(new SelectedFloorSingleton{
                            Selected = Entity.Null
                        });
                    }

                    // swap units:
                    {
                        Entity dstUnit = units[dstIndex];

                        if (em.Exists(srcUnit))
                        {
                            em.SetComponentData(srcUnit, dstLtw);
                            SystemAPI.SetComponentEnabled<IsUnitCoordValid>(srcUnit, false);
                        }

                        if (em.Exists(dstUnit))
                        {
                            em.SetComponentData(dstUnit, srcLtw);
                            SystemAPI.SetComponentEnabled<IsUnitCoordValid>(dstUnit, false);
                        }

                        SystemAPI.SetSingleton(new SelectedUnitSingleton{
                            Selected = Entity.Null
                        });
                    }
                }
            }
        }

    }
}
