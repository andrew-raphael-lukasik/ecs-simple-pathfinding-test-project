using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    public struct FloorCoord : ICleanupComponentData
    {
        public static FixedString64Bytes DebugName {get;} = nameof(FloorCoord);

        public uint2 Value;

        public static implicit operator uint2 (FloorCoord value) => value.Value;
        public static implicit operator FloorCoord  (uint2 value) => new FloorCoord{Value = value};
    }

    public struct IsFloor : IComponentData, IEnableableComponent {}
    public struct IsFloorCoordValid : IComponentData, IEnableableComponent {}
}
