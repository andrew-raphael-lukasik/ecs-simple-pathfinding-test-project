using Unity.Entities;

namespace ServerAndClient.Presentation
{
    public struct UnitAnimationControls : IComponentData
    {
        public float Speed;
        public byte
            IsAiming,
            IsCrouching,
            IsDead,
            EventPistolAttack,
            EventHit;
    }
}
