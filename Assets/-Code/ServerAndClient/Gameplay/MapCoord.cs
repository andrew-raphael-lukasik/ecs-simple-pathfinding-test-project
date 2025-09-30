using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct MapCoord : ICleanupComponentData
    {
        public uint2 Value;

        public static implicit operator uint2 (MapCoord value) => value.Value;
        public static implicit operator MapCoord  (uint2 value) => new MapCoord{Value = value};
    }
}
