using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct GeneratedMapData : IComponentData
    {
        public NativeArray<float3> PositionArray;
        public NativeArray<EMapCell> CellArray;
    }

    public enum EMapCell : byte
    {
        Traversable,
        Obstacle,
        Cover
    }
}
