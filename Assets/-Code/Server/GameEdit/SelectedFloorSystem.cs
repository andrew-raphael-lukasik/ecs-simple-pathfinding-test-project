using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Gameplay;

namespace Server.GameEdit
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct SelectedFloorSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<FloorsSingleton>();
            state.RequireForUpdate<GameState.EDIT>();

            state.EntityManager.CreateSingleton(new SelectedFloorSingleton{
                Selected = Entity.Null
            });
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            if (playerInput.SelectStart==1 && playerInput.IsPointerOverUI==0)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 coord))
                {
                    int index = GameGrid.ToIndex(coord, mapSettings.Size);
                    var floorsRef = SystemAPI.GetSingletonRW<FloorsSingleton>();
                    var floors = floorsRef.ValueRW.Lookup;

                    Entity entity = floors[index];
                    if (entity!=Entity.Null)
                    {
                        #if UNITY_EDITOR || DEBUG
                        UnityEngine.Assertions.Assert.IsTrue(state.EntityManager.HasComponent<FloorCoord>(entity), $"Floor {entity} has no {FloorCoord.DebugName}");
                        #endif

                        SystemAPI.SetSingleton(new SelectedFloorSingleton{
                            Selected = entity
                        });

                        #if UNITY_EDITOR || DEBUG
                        if (entity!=Entity.Null) Debug.Log($"Floor ({entity.Index}:{entity.Version}) selected at {coord}");
                        else Debug.Log($"Floor unselected at {coord}");
                        #endif
                    }
                    else
                    {
                        SystemAPI.SetSingleton(new SelectedUnitSingleton{
                            Selected = Entity.Null
                        });
                        // #if UNITY_EDITOR || DEBUG
                        // Debug.Log($"No floor at {coord}");
                        // #endif
                    }
                }
            }
        }
    }
}
