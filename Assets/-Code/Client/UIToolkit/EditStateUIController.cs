using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.UIToolkit
{
    [RequireComponent(typeof(UIDocumentLocalization))]
    public class EditStateUIController : MonoBehaviour
    {
        [SerializeField] UIDocument _UIDocument;
        MapSettingsSingleton _mapSettings;
        Entity _mapSettingsEntity;
        EntityQuery _queryMapSettings;

        void OnEnable()
        {
            GetComponent<UIDocumentLocalization>().onCompleted += Bind;
        }

        void Bind(VisualElement root)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var em = world.EntityManager;
                _queryMapSettings = em.CreateEntityQuery(ComponentType.ReadWrite<MapSettingsSingleton>());
                _queryMapSettings.TryGetSingleton(out _mapSettings);
            }

            root.For<Button>("enter-play-mode-button", (button) => {
                button.clicked += () => {
                    Debug.Log("Button clicked -> requesting switch to play");
                    StartGame();
                };
            });
            root.For<IntegerField>("settings-seed", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.Seed);
                field.RegisterValueChangedCallback((e) => {
                    uint newValueSafe = (uint) Mathf.Max(e.newValue, 1);
                    _mapSettings.Seed = newValueSafe;
                    field.SetValueWithoutNotify((int) newValueSafe);

                    Debug.Log("Seed value changed -> regenerating the map");
                    GenerateMapAnew();
                });
            });
            root.For<Vector2IntField>("settings-map-size", (field) => {
                field.SetValueWithoutNotify(new Vector2Int((int) _mapSettings.Size.x, (int) _mapSettings.Size.y));
                field.RegisterValueChangedCallback((e) => {
                    uint2 newValueSafe = math.clamp(new uint2((uint) e.newValue.x, (uint) e.newValue.y), new uint2(1, 1), new uint2(MapSettingsSingleton.Size_MAX, MapSettingsSingleton.Size_MAX));
                    _mapSettings.Size = new uint2(newValueSafe.x, newValueSafe.y);
                    field.SetValueWithoutNotify(new Vector2Int((int) newValueSafe.x, (int) newValueSafe.y));

                    Debug.Log("Size value changed -> regenerating the map");
                    GenerateMapAnew();
                });
            });
            root.For<IntegerField>("settings-num-player-units", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.NumPlayerUnits);
                field.RegisterValueChangedCallback((e) => {
                    ushort newValueSafe = (ushort) Mathf.Clamp(e.newValue, 1, MapSettingsSingleton.NumPlayerUnits_MAX);
                    _mapSettings.NumPlayerUnits = newValueSafe;
                    field.SetValueWithoutNotify(newValueSafe);

                    Debug.Log("num player units value changed -> regenerating the map");
                    GenerateMapAnew();
                });
            });
            root.For<IntegerField>("settings-num-enemy-units", (field) => {
                field.SetValueWithoutNotify((int)_mapSettings.NumEnemyUnits);
                field.RegisterValueChangedCallback((e) => {
                    ushort newValueSafe = (ushort) Mathf.Clamp(e.newValue, 1, MapSettingsSingleton.NumEnemyUnits_MAX);
                    _mapSettings.NumEnemyUnits = newValueSafe;
                    field.SetValueWithoutNotify(newValueSafe);

                    Debug.Log("num enemy units value changed -> regenerating the map");
                    GenerateMapAnew();
                });
            });
        }

        void GenerateMapAnew ()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _mapSettingsEntity = _queryMapSettings.GetSingletonEntity();
            em.SetComponentData(_mapSettingsEntity, _mapSettings);
            em.AddComponent<GenerateMapEntitiesRequest>(_mapSettingsEntity);
        }

        void StartGame ()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            em.CreateSingleton(new GameState.ChangeRequest{
                State = EGameState.PLAY
            });
        }

    }
}
