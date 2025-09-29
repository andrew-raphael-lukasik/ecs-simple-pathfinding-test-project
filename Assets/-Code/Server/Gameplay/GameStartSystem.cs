using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStartSettings>();
            Debug.Log($"{state.DebugName}: waiting for {GameStartSettings.DebugName}...");
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Entity requestEntity = SystemAPI.GetSingletonEntity<GameStartSettings>();
            var request = SystemAPI.GetSingleton<GameStartSettings>();

            if (!SystemAPI.HasSingleton<GenerateMapEntitiesRequest>())
            {
                state.EntityManager.CreateSingleton(new GameState.ChangeRequest{
                    State = EGameState.EDIT
                });
                state.EntityManager.CreateSingleton(new GenerateMapEntitiesRequest{
                    Settings = request
                });

                Debug.Log($"{state.DebugName}: {GameStartSettings.DebugName} found, starting the game...");

                state.EntityManager.DestroyEntity(requestEntity);
            }
            else
            {
                Debug.LogError($"{state.DebugName}: {GameStartSettings.DebugName} found but {GenerateMapEntitiesRequest.DebugName} already exists, destroying new request...");
                state.EntityManager.DestroyEntity(requestEntity);
            }
        }
    }
}
