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
    public partial struct EditGameStateBeginsLogSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EDIT_STATE_START_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            Debug.Log($"EDIT MODE START EVENT detected");
            state.EntityManager.AddComponent<IsEditModeActive>(state.SystemHandle);
        }

        public struct IsEditModeActive : IComponentData {}
    }
}
