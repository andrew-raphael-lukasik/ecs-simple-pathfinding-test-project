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
            var em = state.EntityManager;
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();

            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            if (selectedUnit!=Entity.Null && em.Exists(selectedUnit))
            if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
            {
                bool clickedOnPathDestination = false;
                if (SystemAPI.HasComponent<PathfindingQueryResult>(selectedUnit))
                {
                    var pathResult = SystemAPI.GetComponent<PathfindingQueryResult>(selectedUnit);
                    if (pathResult.Success==1)
                    {
                        uint2 pathEnd = pathResult.Path[pathResult.Path.Length-1];
                        clickedOnPathDestination = math.all(dstCoord==pathEnd);
                    }
                }

                // @TODO: replace with player input messages and leave decision making to server-side code

                if (clickedOnPathDestination)
                {
                    em.AddComponent<MovingAlongThePath>(selectedUnit);
                }
                else if(!SystemAPI.HasComponent<MovingAlongThePath>(selectedUnit))
                {
                    uint2 srcCoord = em.GetComponentData<UnitCoord>(selectedUnit);
                    em.AddComponentData(selectedUnit, new PathfindingQuery{
                        Src = srcCoord,
                        Dst = dstCoord,
                    });
                }
            }
        }

    }
}
