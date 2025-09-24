using UnityEngine;
using Unity.Entities;

using ServerAndClient.Gameplay;

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
            state.EntityManager.CreateSingleton(new GameStateChangeRequest{
                State = EGameState.EDIT
            });
            Debug.Log($"initial {nameof(GameStateChangeRequest)} created automatically");
        }
    }
}
