using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct FloorCoord : ICleanupComponentData
    {
        public uint2 Value;

        public static implicit operator uint2 (FloorCoord value) => value.Value;
        public static implicit operator FloorCoord  (uint2 value) => new FloorCoord{Value = value};
    }
}
