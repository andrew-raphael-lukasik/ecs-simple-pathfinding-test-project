using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class PlayStateUIController : MonoBehaviour
    {
        [SerializeField] UIDocument _UIDocument;

        void OnEnable()
        {
            var root = _UIDocument.rootVisualElement;

            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            
            root.Find<Button>("enter-edit-mode").clicked += () => {
                Debug.Log("Button clicked -> requesting switch to edit");
                entityManager.CreateSingleton(new GameState.ChangeRequest{
                    State = EGameState.EDIT
                });
            };
        }
    }
}
