
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Client.Animation
{
    public struct UnitAnimationPresenter : IComponentData
    {
        public UnityObjectRef<Animator> Animator;
        public UnityObjectRef<Transform> Transform;
        public quaternion Rotation;

        public static int id_motion_speed, id_speed, id_grounded;
    }
}
