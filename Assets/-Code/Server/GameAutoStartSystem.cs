using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

using ServerAndClient.GameState;

namespace Server.GameState
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameAutoStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateSingleton<IS_EDIT_GAME_STATE>("EDIT MODE");
            state.EntityManager.AddComponent<IsGameState>(entity);
            
            Debug.Log($"EDIT MODE entity created automatically {entity}");
        }
    }
}
