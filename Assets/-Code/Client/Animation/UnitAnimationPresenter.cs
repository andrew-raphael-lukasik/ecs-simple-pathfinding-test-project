
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

        public static int
            id_move_speed,
            id_is_crounching,
            id_event_pistol_attack,
            id_event_melee_attack,
            id_event_interact,
            id_event_hit,
            id_event_death,
            id_event_revived;
    }
}
