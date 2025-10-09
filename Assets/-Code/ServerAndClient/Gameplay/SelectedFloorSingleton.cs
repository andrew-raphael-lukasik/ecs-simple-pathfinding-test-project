using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct SelectedFloorSingleton : IComponentData
    {
        public Entity Selected;

        public static implicit operator Entity (SelectedFloorSingleton value) => value.Selected;
    }
}
