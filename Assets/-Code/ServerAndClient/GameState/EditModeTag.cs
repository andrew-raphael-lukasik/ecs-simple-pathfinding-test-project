using Unity.Entities;

namespace ServerAndClient.GameState
{
    public struct EditModeTag : IComponentData {}
    public struct EditModeCleanupTag : ICleanupComponentData {}
}
