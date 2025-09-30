using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All & ~WorldSystemFilterFlags.BakingSystem & ~WorldSystemFilterFlags.ProcessAfterLoad)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameInitializationSystemGroup : ComponentSystemGroup {}
}
