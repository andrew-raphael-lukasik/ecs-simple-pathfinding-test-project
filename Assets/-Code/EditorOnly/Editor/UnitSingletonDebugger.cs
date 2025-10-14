using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Debugging
{
    public class UnitSingletonDebugger : GridDebugger
    {

        [MenuItem("Game/Unit Singleton Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitSingletonDebugger>();
            window.titleContent = new GUIContent("Unit Singleton Debugger");
        }

        Entity[] _entities;
        Entity _selected;
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
                    if (entity==_selected)
                    {
                        style.backgroundColor = Color.blue;
                    }
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
            var singletonQuery = em.CreateEntityQuery(typeof(UnitsSingleton));
            if (singletonQuery.CalculateEntityCount()==0)
            {
                errorMessage = $"no {nameof(UnitsSingleton)}";
                return false;
            }
            var singletonRef = singletonQuery.GetSingletonRW<UnitsSingleton>();

            var lookup = singletonRef.ValueRO.Lookup;
            if (lookup.Length==0)
            {
                errorMessage = $"{nameof(UnitsSingleton)} is empty";
                return false;
            }

            var selectedUnitQuery = em.CreateEntityQuery(typeof(SelectedUnitSingleton));
            _selected = selectedUnitQuery.CalculateEntityCount()!=0
                ? selectedUnitQuery.GetSingleton<SelectedUnitSingleton>()
                : Entity.Null;

            _entityManager = em;
            _entities = lookup.ToArray();
            errorMessage = $"{nameof(UnitsSingleton)} has {lookup.Length} entries";
            return true;
        }

    }
}
