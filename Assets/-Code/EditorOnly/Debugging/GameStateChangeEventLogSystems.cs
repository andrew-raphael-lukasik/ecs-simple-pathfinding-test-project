using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace EditorOnly.Debugging
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct EditGameStateBeginsLogSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Debug.Log($"{GameState.EDIT_STARTED_EVENT.DebugName} detected");
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlayGameStateBeginsLogSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Debug.Log($"{GameState.PLAY_STARTED_EVENT.DebugName} detected");
        }
    }
}
