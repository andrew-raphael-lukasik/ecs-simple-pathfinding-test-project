using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct GeneratedMapData : IComponentData
    {
        public NativeArray<float3> PositionArray;
        public NativeArray<EFloorType> FloorArray;
    }

    public enum EFloorType : byte
    {
        Traversable,
        Obstacle,
        Cover
    }
}
