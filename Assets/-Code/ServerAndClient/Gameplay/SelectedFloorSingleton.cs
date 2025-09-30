using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct SelectedFloorSingleton : IComponentData
    {
        public Entity Selected;
    }
}
