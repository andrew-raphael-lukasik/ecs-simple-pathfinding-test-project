using UnityEngine;
using Unity.Entities;

using ServerAndClient.Input;
using ServerAndClient;

namespace Client.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        PlayerInputActions _actions;

        protected override void OnCreate()
        {
            _actions = new PlayerInputActions();
            _actions.UI.Enable();
            _actions.Player.Enable();

            EntityManager.CreateSingleton<PlayerInputSingleton>();
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
            byte attack = _actions.Player.Attack.IsPressed() ? (byte) 1 : (byte) 0;
            byte attackStart = _actions.Player.Attack.WasPressedThisFrame() ? (byte) 1 : (byte) 0;
            Ray ray = default;
            {
                var camera = Camera.main;
                if (camera!=null)
                    ray = camera.ScreenPointToRay(point);
            }

            SystemAPI.SetSingleton(new PlayerInputSingleton{
                PointerRay = ray,
                Move = move,
                Look = look,
                Attack = attack,
                AttackStart = attackStart,
            });
        }
    }
}
