using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Input
{
    public struct PlayerInputData : IComponentData
    {
        public float2 Move;
        public float2 Look;
        public bool Attack;
    }
}
