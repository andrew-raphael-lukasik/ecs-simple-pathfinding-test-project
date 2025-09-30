using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Input
{
    public struct PlayerInputSingleton : IComponentData
    {
        public float2 Move;
        public float2 Look;
        public byte Attack;
        public byte AttackStart;

        public Ray PointerRay;
    }
}
