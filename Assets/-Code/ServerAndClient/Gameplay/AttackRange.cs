using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct AttackRange : IComponentData
    {
        public ushort Value;

        public static implicit operator ushort (AttackRange value) => value.Value;
        public static implicit operator AttackRange  (ushort value) => new AttackRange{Value = value};
    }
}
