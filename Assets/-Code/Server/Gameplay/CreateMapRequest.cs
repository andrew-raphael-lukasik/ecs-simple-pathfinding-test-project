using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Server.Gameplay
{
    /// <summary> Consumed by <seealso cref="MapCreationSystem"/>. </summary>
    public struct CreateMapRequest : IComponentData
    {
        public GameStartSettings Settings;

        public static FixedString64Bytes DebugName {get;} = nameof(CreateMapRequest);
    }
}
