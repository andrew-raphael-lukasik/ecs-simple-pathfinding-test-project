using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct SelectedUnitSingleton : IComponentData
    {
        public Entity Selected;
    }
}
