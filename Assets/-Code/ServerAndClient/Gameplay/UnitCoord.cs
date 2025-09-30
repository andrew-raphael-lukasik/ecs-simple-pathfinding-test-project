using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct UnitCoord : ICleanupComponentData
    {
        public uint2 Value;

        public static implicit operator uint2 (UnitCoord value) => value.Value;
        public static implicit operator UnitCoord  (uint2 value) => new UnitCoord{Value = value};
    }
}
