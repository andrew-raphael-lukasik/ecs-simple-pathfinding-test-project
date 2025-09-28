using Unity.Entities;
using UnityEngine;

namespace ServerAndClient.MonoBehaviours
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Edit State GameObject")]
    public class EditStateGameObject : MonoBehaviour
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
                entityManager.AddComponent<IsEditStateOnlyGameObject>(_entity);
                entityManager.AddComponentObject(_entity, gameObject);
            }
        }

        void OnDestroy()
        {
            if (_world!=null && _world.IsCreated)
            {
                var entityManager = _world.EntityManager;
                if (entityManager.Exists(_entity))
                    entityManager.DestroyEntity(_entity);
            }
        }
    }
}
