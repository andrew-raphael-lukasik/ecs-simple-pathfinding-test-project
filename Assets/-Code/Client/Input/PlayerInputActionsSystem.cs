using UnityEngine;
using Unity.Entities;

using ServerAndClient.Input;

namespace Client.Input
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct PlayerInputActionsSystem : ISystem
    {
        PlayerInputActions _actions;
        void ISystem.OnCreate(ref SystemState state)
        {
            _actions = new PlayerInputActions();
            _actions.UI.Enable();

            state.EntityManager.CreateSingleton<PointerPositionData>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            _actions.Disable();
            _actions.Dispose();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            Vector2 point = _actions.UI.Point.ReadValue<Vector2>();

            SystemAPI.SetSingleton(new PointerPositionData{
                Value = point
            });
        }
    }
}
