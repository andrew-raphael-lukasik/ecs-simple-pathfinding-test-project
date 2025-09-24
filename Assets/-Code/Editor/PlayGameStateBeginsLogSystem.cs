using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.GameState;

namespace Editor.GameState
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlayGameStateBeginsLogSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IS_PLAY_GAME_STATE>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasComponent<IsPlayModeActive>(state.SystemHandle))
            {
                Debug.Log($"PLAY MODE start detected");
                state.EntityManager.AddComponent<IsPlayModeActive>(state.SystemHandle);
            }
        }

        public struct IsPlayModeActive : IComponentData {}
    }
}
