using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Input
{
    public struct PlayerInputSingleton : IComponentData
    {
        public float2 Move;
        public float2 Look;
        public byte Select;
        public byte SelectStart;
        public byte Execute;
        public byte ExecuteStart;

        public Ray PointerRay;
        public byte IsPointerOverUI;
    }
}
