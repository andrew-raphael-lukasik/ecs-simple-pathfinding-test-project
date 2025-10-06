using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.UIToolkit
{
    public class PlayStateUIController : BaseUIController
    {
        protected override void Bind(VisualElement root)
        {
            root.For<Button>("enter-edit-mode-button", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to edit");
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = world.EntityManager;
                    
                    entityManager.CreateSingleton(new GameState.ChangeRequest{
                        State = EGameState.EDIT
                    });
                };
            });
        }
    }
}
