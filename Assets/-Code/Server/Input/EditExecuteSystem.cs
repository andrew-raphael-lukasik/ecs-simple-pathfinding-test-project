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

namespace Server.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]// early simulation phase is best for input execution
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct EditExecuteSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(EditExecuteSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT>();
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();

            // EXECUTE action (right mouse button click)
            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
                {
                    int dstIndex = GameGrid.ToIndex(dstCoord, mapSettings.Size);
                    var mapDataRef = SystemAPI.GetSingletonRW<GeneratedMapData>();
                    var floorsRef = SystemAPI.GetSingletonRW<FloorsSingleton>();
                    var unitsRef = SystemAPI.GetSingletonRW<UnitsSingleton>();
                    Entity srcFloor = SystemAPI.GetSingleton<SelectedFloorSingleton>();
                    Entity srcUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();

                    if (!(srcFloor!=Entity.Null && SystemAPI.Exists(srcFloor)))
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
                    // Assert.IsTrue(SystemAPI.Exists(srcFloor), $"{DebugName} can't execute swap action without a src floor entity - it is a source of srcCoord");
                    Assert.IsTrue(floors[dstIndex]!=Entity.Null, $"{DebugName} can't execute swap action without a floors lookup having an entity at {dstCoord}");
                    #endif

                    uint2 srcCoord = SystemAPI.GetComponent<FloorCoord>(srcFloor);
                    int srcIndex = GameGrid.ToIndex(srcCoord, mapSettings.Size);
                    if (math.all(srcCoord==dstCoord)) return;// ignore, src and dst are the same
                    Entity dstFloor = floors[dstIndex];

                    #if UNITY_EDITOR || DEBUG
                    if (SystemAPI.Exists(srcFloor)) Assert.IsTrue(SystemAPI.HasComponent<FloorCoord>(srcFloor), $"Floor {srcFloor} has no {FloorCoord.DebugName}");
                    if (SystemAPI.Exists(srcUnit)) Assert.IsTrue(SystemAPI.HasComponent<UnitCoord>(srcUnit), $"Unit {srcUnit} has no {UnitCoord.DebugName}");
                    #endif

                    LocalToWorld srcLtw = SystemAPI.GetComponent<LocalToWorld>(srcFloor);
                    LocalToWorld dstLtw = SystemAPI.GetComponent<LocalToWorld>(dstFloor);

                    // swap floors:
                    {
                        {
                            var floorTypesRW = mapDataRef.ValueRW.FloorArray;
                            EFloorType srcType = floorTypesRW[srcIndex];
                            floorTypesRW[srcIndex] = floorTypesRW[dstIndex];;
                            floorTypesRW[dstIndex] = srcType;
                        }

                        SystemAPI.SetComponent(srcFloor, dstLtw);
                        SystemAPI.SetComponentEnabled<IsFloorCoordValid>(srcFloor, false);

                        SystemAPI.SetComponent(dstFloor, srcLtw);
                        SystemAPI.SetComponentEnabled<IsFloorCoordValid>(dstFloor, false);

                        SystemAPI.SetSingleton(new SelectedFloorSingleton{
                            Selected = Entity.Null
                        });
                    }

                    // swap units:
                    {
                        Entity dstUnit = units[dstIndex];

                        if (SystemAPI.Exists(srcUnit))
                        {
                            SystemAPI.SetComponent(srcUnit, dstLtw);
                            SystemAPI.SetComponentEnabled<IsUnitCoordValid>(srcUnit, false);
                        }

                        if (SystemAPI.Exists(dstUnit))
                        {
                            SystemAPI.SetComponent(dstUnit, srcLtw);
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
