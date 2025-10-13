using UnityEngine;
using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    struct AnimatorPrefab : IComponentData
    {
        public UnityObjectRef<GameObject> Prefab;
        public Quaternion PrefabRotation;
    }
}
