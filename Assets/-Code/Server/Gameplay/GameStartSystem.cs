using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;
using ServerAndClient;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StartTheGameData>();
            Debug.Log($"{state.DebugName}: waiting for {StartTheGameData.DebugName}...");
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Entity requestEntity = SystemAPI.GetSingletonEntity<StartTheGameData>();
            var request = SystemAPI.GetSingleton<StartTheGameData>();

            if (!SystemAPI.HasSingleton<GenerateMapEntitiesRequest>())
            {
                state.EntityManager.CreateSingleton(new GameState.ChangeRequest{
                    State = EGameState.EDIT
                });

                Entity mapSettingsSingleton;
                if (!SystemAPI.TryGetSingletonEntity<MapSettingsSingleton>(out mapSettingsSingleton))
                {
                    mapSettingsSingleton = state.EntityManager.CreateSingleton<MapSettingsSingleton>(request.MapSettings);
                }
                state.EntityManager.AddComponent<GenerateMapEntitiesRequest>(mapSettingsSingleton);

                Debug.Log($"{state.DebugName}: {StartTheGameData.DebugName} found, starting the game...");

                state.EntityManager.DestroyEntity(requestEntity);
            }
            else
            {
                Debug.LogError($"{state.DebugName}: {StartTheGameData.DebugName} found but {GenerateMapEntitiesRequest.DebugName} already exists, destroying new request...");
                state.EntityManager.DestroyEntity(requestEntity);
            }
        }
    }
}
