using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GamePresentationSystemGroup : ComponentSystemGroup {}
}
