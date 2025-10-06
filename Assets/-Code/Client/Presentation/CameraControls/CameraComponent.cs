using UnityEngine;
using Unity.Entities;

[assembly: RegisterUnityEngineComponentType(typeof(Camera))]

namespace Client.Presentation.CameraControls
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Game/Hybrid/Camera")]
    public class CameraComponent : MonoBehaviour
    {
        Entity _entity;

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var em = world.EntityManager;
                _entity = em.CreateEntity();
                em.AddComponent<IsMainCamera>(_entity);
                em.AddComponentObject(_entity, GetComponent<Camera>());
            }
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var em = world.EntityManager;
                em.DestroyEntity(_entity);
            }
        }
    }

    public struct IsMainCamera : IComponentData {}
}
