using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Navigation
{
    public struct PathfindingPreviewQuery : IComponentData
    {
        public uint2 Src, Dst;
    }

    public struct PathfindingPreviewQueryResult : IComponentData
    {
        public byte Success;
        public NativeArray<uint2> Path;
    }
}
