using Unity.Entities;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameInitializationSystemGroup : ComponentSystemGroup {}
}
