using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All & ~WorldSystemFilterFlags.BakingSystem & ~WorldSystemFilterFlags.ProcessAfterLoad)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameSimulationSystemGroup : ComponentSystemGroup {}
}
