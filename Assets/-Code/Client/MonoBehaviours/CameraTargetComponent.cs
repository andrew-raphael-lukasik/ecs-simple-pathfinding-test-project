using UnityEngine;
using Unity.Entities;

namespace Client.Presentation.MonoBehaviours
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Camera/Camera Target Transform")]
    public class CameraTargetComponent : MonoBehaviour
    {
        Entity _entity;

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                _entity = entityManager.CreateEntity();
                entityManager.AddComponent<IsCameraLookAtTarget>(_entity);
                entityManager.AddComponentObject(_entity, transform);
            }
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                entityManager.DestroyEntity(_entity);
            }
        }
    }
}
