using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    public struct MapSettingsData : IComponentData
    {
        public Vector2Int Size;
        public Vector3 Offset;
        public uint NumPlayerUnits;
        public uint NumEnemyUnits;
        public uint Seed;

        public const int Size_MAX = 256;
        public const uint NumPlayerUnits_MAX = 32;
        public const uint NumEnemyUnits_MAX = 32;

        public static FixedString64Bytes DebugName {get;} = nameof(MapSettingsData);
    }
}
