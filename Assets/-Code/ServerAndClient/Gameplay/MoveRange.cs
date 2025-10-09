using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct MoveRange : IComponentData
    {
        public ushort Value;

        public static implicit operator ushort (MoveRange value) => value.Value;
        public static implicit operator MoveRange  (ushort value) => new MoveRange{Value = value};
    }
}
