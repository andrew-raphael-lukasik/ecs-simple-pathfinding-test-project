using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All & ~WorldSystemFilterFlags.BakingSystem & ~WorldSystemFilterFlags.ProcessAfterLoad)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GamePresentationSystemGroup : ComponentSystemGroup {}
}
