using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct GameState : IComponentData
    {
        public EGameState State;

        public struct EDIT_STARTED_EVENT : IComponentData {}
        public struct EDIT_ENDED_EVENT : IComponentData {}

        public struct PLAY_STARTED_EVENT : IComponentData {}
        public struct PLAY_ENDED_EVENT : IComponentData {}
    }

    public enum EGameState : byte
    {
        UNDEFINED,
        EDIT,
        PLAY
    }

    public struct GameStateChangeRequest : IComponentData
    {
        public EGameState State;
    }

    // public struct GameState.EDIT_STARTED_EVENT : IComponentData {}
    // public struct GameState.PLAY_STARTED_EVENT : IComponentData {}
}
