using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace ServerAndClient.Gameplay
{
    public struct MapSettingsData : IComponentData
    {
        public Vector2Int Size;
        public const int Size_MAX = 256;

        public Vector3 Offset;
        
        public uint NumPlayerUnits;
        public const uint NumPlayerUnits_MAX = 32;
        
        public uint NumEnemyUnits;
        public const uint NumEnemyUnits_MAX = 32;
        
        public uint Seed;

        public static FixedString64Bytes DebugName {get;} = nameof(MapSettingsData);
    }
}
