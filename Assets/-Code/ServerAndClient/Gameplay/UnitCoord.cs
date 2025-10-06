using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    public struct UnitCoord : ICleanupComponentData
    {
        public static FixedString64Bytes DebugName {get;} = nameof(UnitCoord);

        public uint2 Value;

        public static implicit operator uint2 (UnitCoord value) => value.Value;
        public static implicit operator UnitCoord  (uint2 value) => new UnitCoord{Value = value};
    }

    public struct IsUnit : IComponentData, IEnableableComponent {}
    public struct IsUnitCoordValid : IComponentData, IEnableableComponent {}
}
