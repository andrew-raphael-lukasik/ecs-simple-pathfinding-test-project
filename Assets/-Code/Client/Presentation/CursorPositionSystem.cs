using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using Client.Input;
using ServerAndClient.Input;
using ServerAndClient.Gameplay;
using ServerAndClient;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct CursorPositionSystem : ISystem
    {
        NativeReference<float3> _positionRef;

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            _positionRef = new (Allocator.Persistent);
            state.RequireForUpdate<PlayerInputSingleton>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (_positionRef.IsCreated) _positionRef.Dispose();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            var ray = playerInput.PointerRay;

            if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

                state.Dependency = new RaycastGridJob{
                    RayValue = ray,
                    MapSettings = mapSettings,
                    PositionArray = mapData.PositionArray,
                    PositionRef = _positionRef,
                }.Schedule(state.Dependency);

                state.Dependency = new SetCursorPositionJob{
                    PositionRef = _positionRef,
                }.ScheduleParallel(state.Dependency);
            }
            else
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float dist))
                {
                    _positionRef.Value = ray.origin + ray.direction * dist;
                    state.Dependency = new SetCursorPositionJob{
                        PositionRef = _positionRef,
                    }.ScheduleParallel(state.Dependency);
                }
            }

            // foreach (UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
            // {
            //     sceneView.camera.ScreenPointToRay();
            // }
        }

        [Unity.Burst.BurstCompile]
        partial struct RaycastGridJob : IJob
        {
            public Ray RayValue;
            public MapSettingsSingleton MapSettings;
            [ReadOnly] public NativeArray<float3> PositionArray;
            [WriteOnly] public NativeReference<float3> PositionRef;
            void IJob.Execute()
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(RayValue, out float dist))
                {
                    float3 hit = RayValue.origin + RayValue.direction * dist;
                    float3 localPos = hit - (float3) MapSettings.Origin;

                    uint2 coord = (uint2)(new float2(localPos.x, localPos.z) / new float2(MapSettingsSingleton.CellSize, MapSettingsSingleton.CellSize));
                    coord = math.min(coord, MapSettings.Size-1);// clamp to map size

                    int i = (int)(coord.y * MapSettings.Size.x + coord.x);
                    PositionRef.Value = PositionArray[i];
                }
            }
        }

        [WithAny(typeof(IsEditModeCursor), typeof(IsPlayModeCursor))]
        [Unity.Burst.BurstCompile]
        partial struct SetCursorPositionJob : IJobEntity
        {
            [ReadOnly] public NativeReference<float3> PositionRef;
            public void Execute(ref LocalToWorld transform)
            {
                transform.Value.c3 = new float4(PositionRef.AsReadOnly().Value, 1);

                Debug.DrawRay(PositionRef.AsReadOnly().Value, Vector3.up, Color.cyan);
            }
        }

    }
}
