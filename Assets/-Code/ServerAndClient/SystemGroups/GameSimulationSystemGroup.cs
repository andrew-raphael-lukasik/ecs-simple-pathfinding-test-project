using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameSimulationSystemGroup : ComponentSystemGroup {}
}
