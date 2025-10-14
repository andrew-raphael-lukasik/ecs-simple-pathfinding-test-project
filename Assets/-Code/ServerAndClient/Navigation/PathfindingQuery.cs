using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ServerAndClient.Navigation
{
    public struct PathfindingQuery : IComponentData
    {
        public uint2 Src, Dst;
    }

    public struct PathfindingQueryResult : IComponentData
    {
        public byte Success;
        public NativeArray<uint2> Path;
    }
}
