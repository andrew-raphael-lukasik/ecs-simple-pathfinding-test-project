using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

namespace ServerAndClient.Gameplay
{
    public struct GeneratedMapData : IComponentData
    {
        public JobHandle Dependency;
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
