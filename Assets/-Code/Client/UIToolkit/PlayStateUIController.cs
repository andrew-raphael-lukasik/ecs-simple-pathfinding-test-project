using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.UIToolkit
{
    [RequireComponent(typeof(UIDocumentLocalization))]
    public class PlayStateUIController : MonoBehaviour
    {
        [SerializeField] UIDocument _UIDocument;

        void OnEnable()
        {
            GetComponent<UIDocumentLocalization>().onCompleted += Bind;
        }

        void Bind(VisualElement root)
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
