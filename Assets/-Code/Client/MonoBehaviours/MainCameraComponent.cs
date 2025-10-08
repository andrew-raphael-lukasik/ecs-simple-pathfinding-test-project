using UnityEngine;
using Unity.Entities;

[assembly: RegisterUnityEngineComponentType(typeof(Camera))]

namespace Client.Presentation.MonoBehaviours
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Game/Camera/Main Camera")]
    public class MainCameraComponent : MonoBehaviour
    {
        public static Camera MainCamera;
        Entity _entity;

        void OnEnable()
        {
            UnityEngine.Assertions.Assert.IsNull(MainCamera);
            MainCamera = GetComponent<Camera>();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world!=null && world.IsCreated)
            {
                var em = world.EntityManager;
                _entity = em.CreateEntity();
                em.AddComponent<IsMainCamera>(_entity);
                em.AddComponentObject(_entity, MainCamera);
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

            MainCamera = null;
        }
    }
}
