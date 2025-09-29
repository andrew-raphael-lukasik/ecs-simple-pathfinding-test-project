using Unity.Entities;
using Unity.Collections;

namespace Server.Gameplay
{
    /// <summary> Triggers procedural map generation. Consumed by <seealso cref="MapCreationSystem"/>. </summary>
    public struct GenerateMapEntitiesRequest : IComponentData
    {
        public GameStartSettings Settings;

        public static FixedString64Bytes DebugName {get;} = nameof(GenerateMapEntitiesRequest);
    }
}
