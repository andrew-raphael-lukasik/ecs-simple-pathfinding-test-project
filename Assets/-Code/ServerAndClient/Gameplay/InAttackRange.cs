using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct InAttackRange : IComponentData
    {
        public NativeHashSet<uint2> Coords;
    }
}
