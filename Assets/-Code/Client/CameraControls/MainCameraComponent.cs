using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Client.CameraControls
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Game/Hybrid/Main Camera")]
    public class MainCameraTransformComponent : MonoBehaviour
    {
        Entity _entity;

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                _entity = entityManager.CreateEntity();
                entityManager.AddComponent<IsMainCamera>(_entity);
                entityManager.AddComponentObject(_entity, GetComponent<Camera>());
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

    public struct IsMainCamera : IComponentData {}
}
