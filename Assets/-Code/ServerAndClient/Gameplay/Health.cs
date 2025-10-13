using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct Health : IComponentData
    {
        public ushort Value;

        public static implicit operator ushort (Health value) => value.Value;
        public static implicit operator Health  (ushort value) => new Health{Value = value};
    }
}
