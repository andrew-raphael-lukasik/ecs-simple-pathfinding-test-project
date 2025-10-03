using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace EditorOnly.Debugging
{
    public abstract class GridDebugger : EditorWindow
    {
        protected abstract bool CreateItem(int index, uint2 coord, out VisualElement item);
        protected abstract bool Initialize(EntityManager em, MapSettingsSingleton mapSettings, out string errorMessage);

        public void CreateGUI()
        {
            var root = rootVisualElement;

            var top = new VisualElement();
            {
                var style = top.style;
                style.flexDirection = FlexDirection.Row;
            }
            {
                var worldName = new Label();
                {
                    var style = worldName.style;
                    style.flexGrow = 1;
                    style.unityTextAlign = TextAnchor.MiddleCenter;
                }
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    worldName.text = (world!=null && world.IsCreated) ? world.Name : "NO ACTIVE WORLD";
                }
                top.Add(worldName);

                var button = new Button();
                {
                    var style = button.style;
                    style.minWidth = 200f;
                }
                {
                    button.text = "Refresh";
                    button.clicked += () => {
                        root.Clear();
                        CreateGUI();
                    };
                };
                top.Add(button);
            }
            root.Add(top);

            if (Initialize(out var mapSettings, out string errorMessage))
            {
                var grid = new VisualElement();
                {
                    var style = grid.style;
                    style.flexGrow = 1;
                    style.flexDirection = FlexDirection.ColumnReverse;
                }
                {
                    uint2 size = mapSettings.Size;
                    for (uint y = 0; y < size.y; y++)
                    {
                        var column = new VisualElement();
                        {
                            var style = column.style;
                            style.flexGrow = 1;
                            style.flexDirection = FlexDirection.Row;
                            style.flexBasis = new StyleLength(new Length(100f/size.y, LengthUnit.Percent));
                            style.alignItems = Align.Stretch;
                        }
                        for (uint x = 0; x < size.x; x++)
                        {
                            int index = GameGrid.ToIndex(x, y, mapSettings.Size);

                            var cell = new VisualElement();
                            {
                                var style = cell.style;
                                style.flexGrow = 1;
                                style.flexBasis = new StyleLength(new Length(100f/size.x, LengthUnit.Percent));
                                style.flexDirection = FlexDirection.Column;
                                style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = 1;
                                style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = Color.black;
                                style.backgroundColor = new Color(((float)x/size.x)*0.25f, ((float)y/size.y)*0.25f, 0);
                            }
                            {
                                cell.tooltip = $"Coord: [{x}, {y}]";

                                uint2 coord = new uint2(x, y);
                                if (CreateItem(index, coord, out var item))
                                    cell.Add(item);
                            }
                            column.Add(cell);
                        }
                        grid.Add(column);
                    }
                }
                root.Add(grid);
            }
            else
            {
                var label = new Label();
                {
                    var style = label.style;
                    style.flexGrow = 1;
                    style.unityTextAlign = TextAnchor.MiddleCenter;
                }
                {
                    label.text = errorMessage;
                }
                root.Add(label);
            }
        }

        bool Initialize(out MapSettingsSingleton mapSettings, out string errorMessage)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world==null || !world.IsCreated)
            {
                mapSettings = default;
                errorMessage = "no active world";
                return false;
            }

            var em = world.EntityManager;
            var mapSettingsQuery = em.CreateEntityQuery(typeof(MapSettingsSingleton));
            if (mapSettingsQuery.CalculateEntityCount()==0)
            {
                mapSettings = default;
                errorMessage = $"no {nameof(MapSettingsSingleton)}";
                return false;
            }
            mapSettings = mapSettingsQuery.GetSingletonRW<MapSettingsSingleton>().ValueRO;

            if (!Initialize(em, mapSettings, out errorMessage))
            {
                return false;
            }

            errorMessage = null;// no error
            return true;
        }

    }
}
