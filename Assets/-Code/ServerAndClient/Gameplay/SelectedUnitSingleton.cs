using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct SelectedUnitSingleton : IComponentData
    {
        public Entity Selected;

        public static implicit operator Entity (SelectedUnitSingleton value) => value.Selected;
    }
}
