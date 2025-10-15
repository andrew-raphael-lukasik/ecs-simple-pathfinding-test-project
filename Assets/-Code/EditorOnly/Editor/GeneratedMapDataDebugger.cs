using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient.Gameplay;

namespace EditorOnly.Debugging
{
    public class GeneratedMapDataDebugger : GridDebugger
    {

        [MenuItem("Game/Generated Map Data Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<GeneratedMapDataDebugger>();
            window.titleContent = new GUIContent("Generated Map Data Debugger");
        }

        EFloorType[] _data;
        System.Text.StringBuilder sb = new ();

        protected override bool CreateItem(int index, uint2 coord, out VisualElement ve)
        {
            EFloorType floorType = _data[index];
            {
                var label = new Label();
                {
                    var style = label.style;
                    style.flexGrow = 1;
                    style.unityTextAlign = TextAnchor.MiddleCenter;
                    style.overflow = Overflow.Hidden;
                    // style.unityTextAutoSize = new StyleTextAutoSize(new TextAutoSize(TextAutoSizeMode.BestFit, new Length(8), new Length(128)));
                    style.color = Color.cyan;
                }
                {
                    sb.Clear();
                    sb.Append(floorType);
                    label.text = sb.ToString();

                    sb.Clear();
                    sb.AppendFormat("EFloorType: {0}", floorType);
                    sb.AppendFormat("\nCoord: [{0}, {1}]", coord.x, coord.y);
                    label.tooltip = sb.ToString();
                }
                ve = label;
                return true;
            }

            ve = null;
            return false;
        }

        protected override bool Initialize(EntityManager em, MapSettingsSingleton mapSettings, out string errorMessage)
        {
            var singletonQuery = em.CreateEntityQuery(typeof(GeneratedMapData));
            if (singletonQuery.CalculateEntityCount()==0)
            {
                errorMessage = $"no {nameof(GeneratedMapData)}";
                return false;
            }
            var singletonRef = singletonQuery.GetSingletonRW<GeneratedMapData>();

            var arr = singletonRef.ValueRO.FloorArray;
            if (arr.Length==0)
            {
                errorMessage = $"{nameof(GeneratedMapData)} is empty";
                return false;
            }

            _data = arr.ToArray();
            errorMessage = $"{nameof(GeneratedMapData)} has {arr.Length} entries";
            return true;
        }

    }
}
