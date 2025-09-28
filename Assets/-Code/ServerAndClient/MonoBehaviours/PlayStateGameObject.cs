using Unity.Entities;
using UnityEngine;

namespace ServerAndClient.MonoBehaviours
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Play State GameObject")]
    public class PlayStateGameObject : MonoBehaviour
    {
        World _world;
        Entity _entity;

        void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world!=null)
            {
                var entityManager = _world.EntityManager;
                _entity = entityManager.CreateEntity();
                entityManager.AddComponent<IsPlayStateOnlyGameObject>(_entity);
                entityManager.AddComponentObject(_entity, gameObject);
            }
        }

        void OnDestroy()
        {
            if (_world.IsCreated)
            {
                var entityManager = _world.EntityManager;
                if (entityManager.Exists(_entity))
                    entityManager.DestroyEntity(_entity);
            }
        }
    }
}
