using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.UIToolkit
{
    public class PlayStateUIController : BaseUIController
    {
        EntityManager _em;

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
                _em = world.EntityManager;
        }

        protected override void Bind(VisualElement root)
        {
            root.For<Button>("enter-edit-mode-button", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to edit");
                    _em.CreateSingleton(new GameState.ChangeRequest{
                        State = EGameState.EDIT
                    });
                };
            });
        }

    }
}
