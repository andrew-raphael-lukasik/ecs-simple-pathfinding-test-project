using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public class EditStateUIController : MonoBehaviour
    {
        [SerializeField] UIDocument _UIDocument;

        void OnEnable()
        {
            var root = _UIDocument.rootVisualElement;

            root.For<Button>("enter-play-mode", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to play");
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = world.EntityManager;
                    entityManager.CreateSingleton(new GameState.ChangeRequest{
                        State = EGameState.PLAY
                    });
                };
            });
        }
    }
}
