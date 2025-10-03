using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using Server.Simulation;

namespace EditorOnly.Debugging
{
    public class FloorsSingletonDebugger : GridDebugger
    {

        [MenuItem("Game/Floor Singleton Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<FloorsSingletonDebugger>();
            window.titleContent = new GUIContent("Floor Singleton Debugger");
        }

        Dictionary<uint2, string> _lookup = new ();

        protected override bool CreateItem(uint2 coord, out VisualElement ve)
        {
            if (_lookup.TryGetValue(coord, out string text))
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
                    label.text = text;
                    label.tooltip = $"Entity {text}\nCoord: [{coord.x}, {coord.y}]";
                }
                ve = label;
                return true;
            }

            ve = null;
            return false;
        }

        protected override bool Initialize(EntityManager em, out string errorMessage)
        {
            var singletonQuery = em.CreateEntityQuery(typeof(FloorsSingleton));
            if (singletonQuery.CalculateEntityCount()==0)
            {
                errorMessage = $"no {nameof(FloorsSingleton)}";
                return false;
            }
            var singletonRef = singletonQuery.GetSingletonRW<FloorsSingleton>();
            singletonRef.ValueRW.Dependency.Complete();
            System.Text.StringBuilder sb = new ();
            _lookup.Clear();
            foreach (var kv in singletonRef.ValueRO.Lookup)
            {
                sb.Clear();
                sb.AppendFormat($"({0}:{1})", kv.Value.Index, kv.Value.Version);
                _lookup.Add(kv.Key, sb.ToString());
            }

            var lookup = singletonRef.ValueRO.Lookup;
            if (lookup.Count==0)
            {
                errorMessage = $"{nameof(FloorsSingleton)} is empty";
                return false;
            }

            errorMessage = $"{nameof(FloorsSingleton)} has {lookup.Count} entries";
            return true;
        }

    }
}
