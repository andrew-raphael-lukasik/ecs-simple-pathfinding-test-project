using Unity.Entities;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    /// <summary> <seealso cref="GameState"/> singleton maintained by <seealso cref="GameStateSystem"/>. </summary>
    public struct GameState : IComponentData
    {
        #region fields

        public EGameState State;

        #endregion
        #region nested types


        /// <summary> Consumed by <seealso cref="GameStateSystem"/>. </summary>
        public struct ChangeRequest : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(ChangeRequest);

            public EGameState State;
        }

        public struct EDIT : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(EDIT);
        }

        public struct PLAY : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(PLAY);
        }


        /// <summary> <seealso cref="GameStateSystem"/> emits all these events. They exist for a single update cycle. </summary>
        /// <remarks> Other systems use them to trigger gameplay stage-dependant logic. </remarks>
        public struct EDIT_STARTED_EVENT : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(EDIT_STARTED_EVENT);
        }
        public struct EDIT_ENDED_EVENT : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(EDIT_ENDED_EVENT);
        }

        public struct PLAY_STARTED_EVENT : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(PLAY_STARTED_EVENT);
        }
        public struct PLAY_ENDED_EVENT : IComponentData
        {
            public static FixedString64Bytes DebugName {get;} = nameof(PLAY_ENDED_EVENT);
        }


        #endregion
    }

    public enum EGameState : byte
    {
        UNDEFINED,
        EDIT,
        PLAY
    }
}
