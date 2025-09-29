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
        EntityQuery _queryGameSettings;
        uint _seed, _numPlayerUnits, _numEnemyUnits;
        Vector2Int _mapSize;

        void OnEnable()
        {
            var root = _UIDocument.rootVisualElement;

            // var world = World.DefaultGameObjectInjectionWorld;
            // if (world!=null && world.IsCreated)
            // {
            //     var entityManager = world.EntityManager;
            //     _queryGameSettings = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GameStartSettings>());
            //     _queryGameSettings.TryGetSingleton(out _gameStartSettings);
            // }

            root.For<Button>("enter-play-mode", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to play");
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = world.EntityManager;
                    // _queryGameSettings.SetSingleton(_gameStartSettings);
                    entityManager.CreateSingleton(new GameState.ChangeRequest{
                        State = EGameState.PLAY
                    });
                };
            });
            root.For<IntegerField>("settings-seed", (field) => {
                field.SetValueWithoutNotify((int)_seed);
                field.RegisterValueChangedCallback((e) => {
                    _seed = (uint) Mathf.Max(e.newValue, 1);
                });
            });
            root.For<Vector2IntField>("settings-map-size", (field) => {
                field.SetValueWithoutNotify(_mapSize);
                field.RegisterValueChangedCallback((e) => {
                    _mapSize = e.newValue;
                });
            });
            root.For<IntegerField>("settings-num-player-units", (field) => {
                field.SetValueWithoutNotify((int)_numPlayerUnits);
                field.RegisterValueChangedCallback((e) => {
                    _numPlayerUnits = (ushort) Mathf.Max(e.newValue, 1);
                });
            });
            root.For<IntegerField>("settings-num-enemy-units", (field) => {
                field.SetValueWithoutNotify((int)_numEnemyUnits);
                field.RegisterValueChangedCallback((e) => {
                    _numEnemyUnits = (ushort) Mathf.Max(e.newValue, 1);
                });
            });
        }
    }
}
