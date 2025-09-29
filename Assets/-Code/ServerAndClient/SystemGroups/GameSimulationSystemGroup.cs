using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.All)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameSimulationSystemGroup : ComponentSystemGroup {}
}
