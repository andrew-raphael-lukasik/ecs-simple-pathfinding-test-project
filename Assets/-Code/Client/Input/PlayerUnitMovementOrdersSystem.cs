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
                uint2 srcCoord = state.EntityManager.GetComponentData<UnitCoord>(selectedUnit.Selected);

                state.EntityManager.AddComponentData(selectedUnit.Selected, new CalculatePathRequest{
                    Src = srcCoord,
                    Dst = dstCoord,
                });
            }
        }

    }
}
