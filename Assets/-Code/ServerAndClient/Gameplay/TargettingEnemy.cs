using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct TargettingEnemy : IComponentData
    {
        public Entity Value;

        public static implicit operator Entity (TargettingEnemy value) => value.Value;
        public static implicit operator TargettingEnemy  (Entity value) => new TargettingEnemy{Value = value};
    }
}
