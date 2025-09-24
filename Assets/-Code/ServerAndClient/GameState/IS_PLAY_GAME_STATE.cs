using Unity.Entities;

namespace ServerAndClient.GameState
{
    public struct IS_PLAY_GAME_STATE : IComponentData {}
    public struct IsPlayGameStateCleanup : ICleanupComponentData {}
}
