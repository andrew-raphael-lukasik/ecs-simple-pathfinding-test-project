using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct UnitAttackData : IComponentData
    {
        public ushort AttackRange;
    }
}
