using UnityEngine;
using Unity.Entities;

using ServerAndClient.Input;
using ServerAndClient;

namespace Client.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    public partial class PlayerInputActionsSystem : SystemBase
    {
        PlayerInputActions _actions;

        protected override void OnCreate()
        {
            _actions = new PlayerInputActions();
            _actions.UI.Enable();
            _actions.Player.Enable();

            EntityManager.CreateSingleton<PointerPositionData>();
            EntityManager.CreateSingleton<PlayerInputData>();
        }

        protected override void OnDestroy()
        {
            _actions.Disable();
            _actions.Dispose();
        }

        protected override void OnUpdate()
        {
            Vector2 point = _actions.UI.Point.ReadValue<Vector2>();
            Vector2 move = _actions.Player.Move.ReadValue<Vector2>();
            Vector2 look = _actions.Player.Look.ReadValue<Vector2>();
            bool attack = _actions.Player.Attack.ReadValue<float>()!=0;

            SystemAPI.SetSingleton(new PointerPositionData{
                Value = point
            });
            SystemAPI.SetSingleton(new PlayerInputData{
                Move = move,
                Look = look,
                Attack = attack,
            });
        }
    }
}
