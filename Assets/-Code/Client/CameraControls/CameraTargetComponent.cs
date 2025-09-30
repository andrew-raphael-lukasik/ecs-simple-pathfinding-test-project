using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Client.CameraControls
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Hybrid/Camera Target Transform")]
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
