using UnityEngine.InputSystem;
using Unity.Entities;

using ServerAndClient.Gameplay;

namespace Editor.Debugging
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct ChangeGameStateKeyboardShortcutSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var keyboard = Keyboard.current;
            if (keyboard!=null)
            if (keyboard.altKey.isPressed)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    state.EntityManager.CreateSingleton(new GameStateChangeRequest{
                        State = EGameState.EDIT
                    });
                }
                else if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    state.EntityManager.CreateSingleton(new GameStateChangeRequest{
                        State = EGameState.PLAY
                    });
                }
            }
        }
    }
}
