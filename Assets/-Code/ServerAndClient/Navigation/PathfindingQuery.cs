using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Navigation
{
    public struct PathfindingQuery : IComponentData
    {
        public uint2 Src, Dst;
    }
}
