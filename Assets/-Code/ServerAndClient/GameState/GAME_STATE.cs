using Unity.Entities;

namespace ServerAndClient.GameState
{
    public struct IsGameState : IComponentData {}

    public struct IS_EDIT_GAME_STATE : IComponentData {}
    public struct IS_EDIT_GAME_STATE_CLEANUP : ICleanupComponentData {}
    public struct EDIT_STATE_START_EVENT : IComponentData {}

    public struct IS_PLAY_GAME_STATE : IComponentData {}
    public struct IS_PLAY_GAME_STATE_CLEANUP : ICleanupComponentData {}
    public struct PLAY_STATE_START_EVENT : IComponentData {}
}
