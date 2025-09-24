using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    public struct GameState : IComponentData
    {
        #region fields

        public EGameState State;

        #endregion
        #region nested types


        public struct ChangeRequest : IComponentData
        {
            public EGameState State;
        }


        public struct EDIT_STARTED_EVENT : IComponentData {}
        public struct EDIT_ENDED_EVENT : IComponentData {}

        public struct PLAY_STARTED_EVENT : IComponentData {}
        public struct PLAY_ENDED_EVENT : IComponentData {}


        #endregion
    }

    public enum EGameState : byte
    {
        UNDEFINED,
        EDIT,
        PLAY
    }
}
