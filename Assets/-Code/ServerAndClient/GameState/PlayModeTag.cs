using Unity.Entities;

namespace ServerAndClient.GameState
{
    public struct PlayModeTag : IComponentData {}
    public struct PlayModeCleanupTag : ICleanupComponentData {}
}
