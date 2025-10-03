using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient.Input;
using ServerAndClient.Gameplay;

namespace ServerAndClient
{
    public static class GameGrid
    {


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex (uint2 coord, uint2 mapSize)
        {
            coord = math.min(coord, mapSize-1);// clamp to map size
            return (int)(coord.y * mapSize.x + coord.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex (int x, int y, uint2 mapSize) => ToIndex(new uint2((uint)x, (uint)y), mapSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex (uint x, uint y, uint2 mapSize) => ToIndex(new uint2(x, y), mapSize);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 ToCoord (int i, uint2 mapSize)
        {
            uint x = (uint)(i%mapSize.x);
            uint y = (uint)(i/mapSize.x);
            uint2 coord = new uint2(x, y);
            coord = math.min(coord, mapSize-1);// clamp to map size
            return coord;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 ToCoord (float3 point, float3 mapOrigin, uint2 mapSize)
        {
            float3 localPoint = point - mapOrigin;
            uint2 coord = (uint2)(new float2(localPoint.x, localPoint.z) / new float2(MapSettingsSingleton.CellSize, MapSettingsSingleton.CellSize));
            coord = math.min(coord, mapSize-1);// clamp to map size
            return coord;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raycast (Ray ray, float3 mapOrigin, uint2 mapSize, out uint2 coord)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                float3 hit = ray.origin + ray.direction * dist;
                coord = ToCoord(point: hit, mapOrigin: mapOrigin, mapSize: mapSize);
                return true;
            }
            else
            {
                coord = new uint2(uint.MaxValue, uint.MaxValue);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raycast (PlayerInputSingleton playerInput, MapSettingsSingleton mapSettings, out uint2 coord)
        {
            return Raycast(
                ray: playerInput.PointerRay,
                mapOrigin: mapSettings.Origin,
                mapSize: mapSettings.Size,
                out coord
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raycast (Ray ray, float3 mapOrigin, uint2 mapSize, out int i)
        {
            bool success = Raycast(
                ray: ray,
                mapOrigin: mapOrigin,
                mapSize: mapSize,
                out uint2 coord
            );
            i = success
                ? (int)(coord.y * mapSize.x + coord.x)
                : int.MaxValue;
            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raycast (PlayerInputSingleton playerInput, MapSettingsSingleton mapSettings, out int i)
        {
            return Raycast(
                ray: playerInput.PointerRay,
                mapOrigin: mapSettings.Origin,
                mapSize: mapSettings.Size,
                out i
            );
        }


        [Unity.Burst.BurstCompile]
        public struct RaycastJob : IJob
        {
            public Ray RayValue;
            public float3 MapOrigin;
            public uint2 MapSize;
            [ReadOnly] public NativeArray<float3> PositionArray;
            [WriteOnly] public NativeReference<float3> PositionRef;
            [WriteOnly] public NativeReference<bool> RayHitRef;
            void IJob.Execute()
            {
                if (Raycast(ray: RayValue, mapOrigin: MapOrigin, mapSize: MapSize, out int i))
                {
                    PositionRef.Value = PositionArray[i];
                    RayHitRef.Value = true;
                }
                else
                {
                    PositionRef.Value = 0;
                    RayHitRef.Value = false;
                }
            }
        }


    }
}
