using UnityEngine;
using Unity.Entities;

namespace ServerAndClient.Input
{
    public struct PointerPositionData : IComponentData
    {
        public Vector2 Value;
    }
}
