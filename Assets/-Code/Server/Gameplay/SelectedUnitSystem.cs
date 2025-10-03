using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Simulation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct SelectedUnitSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<UnitsSingleton>();

            state.EntityManager.CreateSingleton(new SelectedUnitSingleton{
                Selected = Entity.Null
            });
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            if (playerInput.SelectStart==1)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 coord))
                {
                    var unitsRef = SystemAPI.GetSingletonRW<UnitsSingleton>();
                    var units = unitsRef.ValueRW.Lookup;

                    int index = GameGrid.ToIndex(coord, mapSettings.Size);
                    Entity entity = units[index];
                    if (entity!=Entity.Null)
                    {
                        #if UNITY_EDITOR || DEBUG
                        UnityEngine.Assertions.Assert.IsTrue(state.EntityManager.HasComponent<UnitCoord>(entity), $"Unit {entity} has no {UnitCoord.DebugName}");
                        #endif

                        SystemAPI.SetSingleton(new SelectedUnitSingleton{
                            Selected = entity
                        });

                        #if UNITY_EDITOR || DEBUG
                        if (entity!=Entity.Null) Debug.Log($"Unit ({entity.Index}:{entity.Version}) selected at {coord}");
                        else Debug.Log($"Unit unselected at {coord}");
                        #endif
                    }
                    else
                    {
                        SystemAPI.SetSingleton(new SelectedUnitSingleton{
                            Selected = Entity.Null
                        });
                        // #if UNITY_EDITOR || DEBUG
                        // Debug.Log($"No unit at {coord}");
                        // #endif
                    }
                }
            }
        }
    }
}
