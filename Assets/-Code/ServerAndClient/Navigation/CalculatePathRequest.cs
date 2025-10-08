using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Navigation
{
    public struct CalculatePathRequest : IComponentData
    {
        public uint2 Src, Dst;
    }
}
