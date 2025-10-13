using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct Damage : IBufferElementData
    {
        public ushort Amount;
        public EDamageType Types;
        public Entity Instigator;
    }

    [System.Flags]
    public enum EDamageType : byte
    {
        Emotional,
        Kinetic,

        Melee,
        Ranged,
    }

}
