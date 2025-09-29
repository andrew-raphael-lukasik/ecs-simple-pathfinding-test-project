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
        MapSettingsData _mapSettings;
        Entity _mapSettingsEntity;
        EntityQuery _queryMapSettings;

        void OnEnable()
        {
            var root = _UIDocument.rootVisualElement;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                _queryMapSettings = entityManager.CreateEntityQuery(ComponentType.ReadWrite<MapSettingsData>());
                _queryMapSettings.TryGetSingleton(out _mapSettings);
            }

            root.For<Button>("enter-play-mode", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to play");
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = world.EntityManager;
                    
                    _mapSettingsEntity = _queryMapSettings.GetSingletonEntity();
                    entityManager.SetComponentData(_mapSettingsEntity, _mapSettings);
                    // _queryMapSettings.SetSingleton(_mapSettings);
                    entityManager.AddComponent<GenerateMapEntitiesRequest>(_mapSettingsEntity);
                    
                    entityManager.CreateSingleton(new GameState.ChangeRequest{
                        State = EGameState.PLAY
                    });
                };
            });
            root.For<IntegerField>("settings-seed", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.Seed);
                field.RegisterValueChangedCallback((e) => {
                    uint newValueSafe = (uint) Mathf.Max(e.newValue, 1);
                    _mapSettings.Seed = newValueSafe;
                    field.SetValueWithoutNotify((int) newValueSafe);
                });
            });
            root.For<Vector2IntField>("settings-map-size", (field) => {
                field.SetValueWithoutNotify(_mapSettings.Size);
                field.RegisterValueChangedCallback((e) => {
                    Vector2Int newValueSafe = Vector2Int.Min(Vector2Int.Max(e.newValue, new Vector2Int(1, 1)), new Vector2Int(MapSettingsData.Size_MAX, MapSettingsData.Size_MAX));
                    _mapSettings.Size = newValueSafe;
                    field.SetValueWithoutNotify(newValueSafe);
                });
            });
            root.For<IntegerField>("settings-num-player-units", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.NumPlayerUnits);
                field.RegisterValueChangedCallback((e) => {
                    ushort newValueSafe = (ushort) Mathf.Clamp(e.newValue, 1, MapSettingsData.NumPlayerUnits_MAX);
                    _mapSettings.NumPlayerUnits = newValueSafe;
                    field.SetValueWithoutNotify(newValueSafe);
                });
            });
            root.For<IntegerField>("settings-num-enemy-units", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.NumEnemyUnits);
                field.RegisterValueChangedCallback((e) => {
                    ushort newValueSafe = (ushort) Mathf.Clamp(e.newValue, 1, MapSettingsData.NumEnemyUnits_MAX);
                    _mapSettings.NumEnemyUnits = newValueSafe;
                    field.SetValueWithoutNotify(newValueSafe);
                });
            });
        }
    }
}
