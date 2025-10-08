using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    /// <summary> Triggers game start. Consumed by <seealso cref="GameStartSystem"/>. </summary>
    public struct StartTheGameData : IComponentData
    {
        public static FixedString64Bytes DebugName {get;} = nameof(StartTheGameData);

        public MapSettingsSingleton MapSettings;
    }
}
