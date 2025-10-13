using UnityEngine;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Input;
using Client.UIToolkit;

namespace Client.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]// presentation phase is best for input collection
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
            var uiActions = _actions.UI;
            var playerActions = _actions.Player;

            Vector2 point = uiActions.Point.ReadValue<Vector2>();
            Vector2 move = playerActions.Move.ReadValue<Vector2>();
            Vector2 look = playerActions.Look.ReadValue<Vector2>();
            byte select = playerActions.Select.IsPressed() ? (byte) 1 : (byte) 0;
            byte selectStart = playerActions.Select.WasPressedThisFrame() ? (byte) 1 : (byte) 0;
            byte execute = playerActions.Execute.IsPressed() ? (byte) 1 : (byte) 0;
            byte executeStart = playerActions.Execute.WasPressedThisFrame() ? (byte) 1 : (byte) 0;
            byte isPointerOverUI = BaseUIController.IsPointerOverUI(point) ? (byte) 1 : (byte) 0;
            Ray ray = default;
            {
                var camera = Camera.main;
                if (camera!=null)
                    ray = camera.ScreenPointToRay(point);
            }

            SystemAPI.SetSingleton(new PlayerInputSingleton{
                PointerRay = ray,
                IsPointerOverUI = isPointerOverUI,
                Move = move,
                Look = look,
                Select = select,
                SelectStart = selectStart,
                Execute = execute,
                ExecuteStart = executeStart,
            });
        }
    }
}
