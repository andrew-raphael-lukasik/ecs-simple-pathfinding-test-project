using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Server.Gameplay
{
    /// <summary> Consumed by <seealso cref="GameStartSystem"/>. </summary>
    public struct GameStartSettings : IComponentData
    {
        public Vector2Int MapSize;
        public Vector3 MapOffset;
        public uint Seed;

        public static FixedString64Bytes DebugName {get;} = nameof(GameStartSettings);
    }
}
