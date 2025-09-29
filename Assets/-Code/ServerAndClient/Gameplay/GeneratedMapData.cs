using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct GeneratedMapData : IComponentData
    {
        public NativeArray<float3> Position;
        public NativeArray<EMapCell> Cell;
    }

    public enum EMapCell : byte
    {
        Traversable,
        Obstacle,
        Cover
    }
}
