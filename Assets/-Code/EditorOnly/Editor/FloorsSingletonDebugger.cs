using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;
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

        Entity[] _entities;
        System.Text.StringBuilder sb = new ();
        EntityManager _entityManager;

        protected override bool CreateItem(int index, uint2 coord, out VisualElement ve)
        {
            Entity entity = _entities[index];
            if (entity!=Entity.Null)
            {
                bool entityExists = _entityManager.Exists(entity);

                var label = new Label();
                {
                    var style = label.style;
                    style.flexGrow = 1;
                    style.unityTextAlign = TextAnchor.MiddleCenter;
                    style.overflow = Overflow.Hidden;
                    // style.unityTextAutoSize = new StyleTextAutoSize(new TextAutoSize(TextAutoSizeMode.BestFit, new Length(8), new Length(128)));
                    style.color = entityExists ? Color.cyan : Color.red;
                }
                {
                    sb.Clear();
                    sb.AppendFormat("({0}:{1})", entity.Index, entity.Version);
                    label.text = sb.ToString();

                    sb.Clear();
                    sb.AppendFormat("Entity ({0}:{1})", entity.Index, entity.Version);
                    if (!entityExists) sb.Append(" (does not exist)");
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
            var singletonQuery = em.CreateEntityQuery(typeof(FloorsSingleton));
            if (singletonQuery.CalculateEntityCount()==0)
            {
                errorMessage = $"no {nameof(FloorsSingleton)}";
                return false;
            }
            var singletonRef = singletonQuery.GetSingletonRW<FloorsSingleton>();
            singletonRef.ValueRW.Dependency.Complete();

            var lookup = singletonRef.ValueRO.Lookup;
            if (lookup.Length==0)
            {
                errorMessage = $"{nameof(FloorsSingleton)} is empty";
                return false;
            }

            _entityManager = em;
            _entities = lookup.ToArray();
            errorMessage = $"{nameof(FloorsSingleton)} has {lookup.Length} entries";
            return true;
        }

    }
}
