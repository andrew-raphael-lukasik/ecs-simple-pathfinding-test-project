using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct MovingAlongThePath : IComponentData
    {
        public int Index;
        public float TimeUntilNextCoord;
    }
}
