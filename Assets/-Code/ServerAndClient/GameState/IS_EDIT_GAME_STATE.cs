using Unity.Entities;

namespace ServerAndClient.GameState
{
    public struct IS_EDIT_GAME_STATE : IComponentData {}
    public struct IsEditGameStateCleanup : ICleanupComponentData {}
}
