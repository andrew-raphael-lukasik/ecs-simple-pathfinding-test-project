using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct UnitMoveData : IComponentData
    {
        public ushort MoveRange;
    }
}
