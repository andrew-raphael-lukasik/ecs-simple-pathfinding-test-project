using UnityEngine;
using Unity.Entities;

using ServerAndClient.Input;

namespace Client.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputActionsSystem : SystemBase
    {
        PlayerInputActions _actions;

        protected override void OnCreate()
        {
            _actions = new PlayerInputActions();
            _actions.UI.Enable();

            EntityManager.CreateSingleton<PointerPositionData>();
        }

        protected override void OnDestroy()
        {
            _actions.Disable();
            _actions.Dispose();
        }

        protected override void OnUpdate()
        {
            Vector2 point = _actions.UI.Point.ReadValue<Vector2>();

            SystemAPI.SetSingleton(new PointerPositionData{
                Value = point
            });
        }
    }
}
