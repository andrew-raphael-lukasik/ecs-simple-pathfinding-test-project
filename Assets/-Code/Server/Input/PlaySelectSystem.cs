using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using ServerAndClient.Navigation;
using Server.Gameplay;

namespace Server.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]// early simulation phase is best for input execution
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlaySelectSystem : ISystem
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

            // SELECT action (left mouse button click)
            if (playerInput.SelectStart==1 && playerInput.IsPointerOverUI==0)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
                {
                    var unitsRef = SystemAPI.GetSingletonRW<UnitsSingleton>();
                    var units = unitsRef.ValueRW.Lookup;

                    int index = GameGrid.ToIndex(dstCoord, mapSettings.Size);
                    Entity entity = units[index];
                    if (entity!=Entity.Null)
                    {
                        #if UNITY_EDITOR || DEBUG
                        UnityEngine.Assertions.Assert.IsTrue(SystemAPI.HasComponent<UnitCoord>(entity), $"Unit {entity} has no {UnitCoord.DebugName}");
                        #endif

                        SystemAPI.SetSingleton(new SelectedUnitSingleton{
                            Selected = entity
                        });

                        #if UNITY_EDITOR || DEBUG
                        if (entity!=Entity.Null) Debug.Log($"Unit ({entity.Index}:{entity.Version}) selected at {dstCoord}");
                        else Debug.Log($"Unit unselected at {dstCoord}");
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

                Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
                if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit) && SystemAPI.GetComponent<TargettingEnemy>(selectedUnit)!=Entity.Null)
                {
                    var targettingEnemyRW = SystemAPI.GetComponentRW<TargettingEnemy>(selectedUnit);
                    targettingEnemyRW.ValueRW = Entity.Null;
                }

                if (selectedUnit!=Entity.Null)
                {
                    if (SystemAPI.HasComponent<PathfindingPreviewQueryResult>(selectedUnit))
                    {
                        var results = SystemAPI.GetComponentRW<PathfindingPreviewQueryResult>(selectedUnit);
                        if (results.ValueRW.Path.IsCreated) results.ValueRW.Path.Dispose();

                        state.EntityManager.RemoveComponent<PathfindingPreviewQueryResult>(selectedUnit);
                    }

                    if (SystemAPI.HasComponent<PathfindingQueryResult>(selectedUnit))
                    {
                        var results = SystemAPI.GetComponentRW<PathfindingQueryResult>(selectedUnit);
                        if (results.ValueRW.Path.IsCreated) results.ValueRW.Path.Dispose();

                        state.EntityManager.RemoveComponent<PathfindingQueryResult>(selectedUnit);
                    }
                }
            }
        }
    }
}
