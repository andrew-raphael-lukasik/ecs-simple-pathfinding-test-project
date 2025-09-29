using Unity.Entities;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    /// <summary> Triggers procedural map generation. Consumed by <seealso cref="MapCreationSystem"/>. </summary>
    public struct GenerateMapEntitiesRequest : IComponentData
    {
        public static FixedString64Bytes DebugName {get;} = nameof(GenerateMapEntitiesRequest);
    }
}
