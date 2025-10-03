using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    public struct MapSettingsSingleton : IComponentData
    {
        public uint2 Size;
        public float3 Origin;
        public uint NumPlayerUnits;
        public uint NumEnemyUnits;
        public uint Seed;

        public const float CellSize = 1f;
        public const uint Size_MAX = 256;
        public const uint NumPlayerUnits_MAX = 32;
        public const uint NumEnemyUnits_MAX = 32;
    }
}
