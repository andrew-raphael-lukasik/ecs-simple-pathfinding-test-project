using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient;
using ServerAndClient.Gameplay;
using Client.Presentation.CameraControls;

namespace Client.UIToolkit
{
    public class EditStateUIController : BaseUIController
    {
        EntityManager _em;

        #region map settings
        MapSettingsSingleton _mapSettings;
        Entity _mapSettingsEntity;
        EntityQuery _queryMapSettings;
        #endregion
        
        #region selected unit
        EntityQuery _selectedUnitQuery;
        VisualElement _selectedUnitUi;
        #endregion

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _em = world.EntityManager;
            _selectedUnitQuery = _em.CreateEntityQuery(typeof(SelectedUnitSingleton));
        } 

        void Update()
        {
            if (_selectedUnitUi!=null)
            {
                var style = _selectedUnitUi.style;
                if (
                        _selectedUnitQuery.TryGetSingleton<SelectedUnitSingleton>(out var selectedUnitSingleton)
                    &&  selectedUnitSingleton.Selected!=Entity.Null
                    &&  _em.Exists(selectedUnitSingleton.Selected)
                    &&  _em.HasComponent<LocalToWorld>(selectedUnitSingleton.Selected)
                )
                {
                    var ltw = _em.GetComponentData<LocalToWorld>(selectedUnitSingleton.Selected);
                    Vector2 guiPoint = RuntimePanelUtils.CameraTransformWorldToPanel(_selectedUnitUi.panel, ltw.Position, MainCameraComponent.MainCamera);
                    style.left = guiPoint.x + 30;
                    style.top = guiPoint.y;

                    if (style.visibility==Visibility.Hidden) style.visibility = Visibility.Visible;
                }
                else if (style.visibility==Visibility.Visible) style.visibility = Visibility.Hidden;
            }
        }

        protected override void Bind(VisualElement root)
        {
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

            Label unitMoveRangeSliderLabel = root.Find<Label>("selected-unit-move-range-display");
            SliderInt unitMoveRangeSlider = root.For<SliderInt>("selected-unit-move-range", (slider) => {
                slider.RegisterValueChangedCallback((e) => {
                    var selectedUnitSingleton = _selectedUnitQuery.GetSingleton<SelectedUnitSingleton>();
                    UnityEngine.Assertions.Assert.IsTrue(_em.Exists(selectedUnitSingleton.Selected));

                    _em.SetComponentData(selectedUnitSingleton.Selected, new UnitMoveData{
                        MoveRange = (ushort) e.newValue
                    });

                    unitMoveRangeSliderLabel.text = e.newValue.ToString();
                });
            });

            Label unitAttackRangeSliderLabel = root.Find<Label>("selected-unit-attack-range-display");
            SliderInt unitAttackRangeSlider = root.For<SliderInt>("selected-unit-attack-range", (slider) => {
                slider.RegisterValueChangedCallback((e) => {
                    var selectedUnitSingleton = _selectedUnitQuery.GetSingleton<SelectedUnitSingleton>();
                    UnityEngine.Assertions.Assert.IsTrue(_em.Exists(selectedUnitSingleton.Selected));

                    _em.SetComponentData(selectedUnitSingleton.Selected, new UnitAttackData{
                        AttackRange = (ushort) e.newValue
                    });

                    unitAttackRangeSliderLabel.text = e.newValue.ToString();
                });
            });

            _selectedUnitUi = root.For<VisualElement>("selected-unit-view-root", (panel) => {
                panel.style.visibility = Visibility.Hidden;

                panel.RegisterCallback<GeometryChangedEvent>((e) => {
                    _selectedUnitQuery.TryGetSingleton<SelectedUnitSingleton>(out var selectedUnitSingleton);
                    if (selectedUnitSingleton.Selected!=Entity.Null && _em.Exists(selectedUnitSingleton.Selected))
                    {
                        var unitMove = _em.GetComponentData<UnitMoveData>(selectedUnitSingleton.Selected);
                        var unitAttack = _em.GetComponentData<UnitAttackData>(selectedUnitSingleton.Selected);

                        unitMoveRangeSlider.SetValueWithoutNotify(unitMove.MoveRange);
                        unitAttackRangeSlider.SetValueWithoutNotify(unitAttack.AttackRange);
                        unitMoveRangeSliderLabel.text = unitMove.MoveRange.ToString();
                        unitAttackRangeSliderLabel.text = unitAttack.AttackRange.ToString();
                    }
                    else
                    {
                        unitMoveRangeSliderLabel.text = "?";
                        unitAttackRangeSliderLabel.text = "?";
                    }
                });
            });
        }

        void GenerateMapAnew ()
        {
            _mapSettingsEntity = _queryMapSettings.GetSingletonEntity();
            _em.SetComponentData(_mapSettingsEntity, _mapSettings);
            _em.AddComponent<GenerateMapEntitiesRequest>(_mapSettingsEntity);
        }

        void StartGame ()
        {
            _em.CreateSingleton(new GameState.ChangeRequest{
                State = EGameState.PLAY
            });
        }

    }
}
