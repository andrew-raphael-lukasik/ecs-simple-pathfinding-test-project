using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameInitializationSystemGroup : ComponentSystemGroup {}
}
