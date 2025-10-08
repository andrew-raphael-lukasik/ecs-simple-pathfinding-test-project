using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using ServerAndClient.Navigation;

namespace Client.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlayerUnitMovementOrdersSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<SelectedUnitSingleton>();
            state.RequireForUpdate<GameState.PLAY>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            var selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();

            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            if (selectedUnit.Selected!=Entity.Null && state.EntityManager.Exists(selectedUnit.Selected))
            if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
            {
                bool clickedOnPathDestination = false;
                if (SystemAPI.HasComponent<PathfindingQueryResult>(selectedUnit.Selected))
                {
                    var pathResult = SystemAPI.GetComponent<PathfindingQueryResult>(selectedUnit.Selected);
                    if (pathResult.Success==1)
                    {
                        uint2 pathEnd = pathResult.Path[pathResult.Path.Length-1];
                        clickedOnPathDestination = math.all(dstCoord==pathEnd);
                    }
                }

                if (clickedOnPathDestination)
                {
                    state.EntityManager.AddComponent<MovingAlongThePath>(selectedUnit.Selected);
                }
                else if(!SystemAPI.HasComponent<MovingAlongThePath>(selectedUnit.Selected))
                {
                    uint2 srcCoord = state.EntityManager.GetComponentData<UnitCoord>(selectedUnit.Selected);
                    state.EntityManager.AddComponentData(selectedUnit.Selected, new PathfindingQuery{
                        Src = srcCoord,
                        Dst = dstCoord,
                    });
                }
            }
        }

    }
}
