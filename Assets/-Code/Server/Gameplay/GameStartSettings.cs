using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Server.Gameplay
{
    /// <summary> Triggers game start. Consumed by <seealso cref="GameStartSystem"/>. </summary>
    public struct GameStartSettings : IComponentData
    {
        public Vector2Int MapSize;
        public Vector3 MapOffset;
        public int NumPlayerUnits;
        public int NumEnemyUnits;
        public uint Seed;

        public static FixedString64Bytes DebugName {get;} = nameof(GameStartSettings);
    }
}
