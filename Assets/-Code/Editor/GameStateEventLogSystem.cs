using UnityEngine;
using Unity.Entities;

using ServerAndClient.GameState;

namespace Editor.GameState
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStateEventLogSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var _ in SystemAPI.Query< RefRO<EditModeStartedEventTag> >())
            {
                Debug.Log($"EDIT MODE START event detected");
            }
        }
    }
}
