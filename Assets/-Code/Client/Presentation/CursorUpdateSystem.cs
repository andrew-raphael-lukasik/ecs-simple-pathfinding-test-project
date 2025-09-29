using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient.Input;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct CursorUpdateSystem : ISystem
    {
        NativeReference<float3> _positionRef;

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            _positionRef = new (Allocator.Persistent);
            state.RequireForUpdate<PointerPositionData>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (_positionRef.IsCreated) _positionRef.Dispose();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera!=null)
            {
                var pointerData = SystemAPI.GetSingleton<PointerPositionData>();
                var ray = camera.ScreenPointToRay(pointerData.Value);

                _positionRef.Value = new float3(0, 0, 0);

                if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
                {
                    var mapSettings = SystemAPI.GetSingleton<MapSettingsData>();

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
            public MapSettingsData MapSettings;
            [ReadOnly] public NativeArray<float3> PositionArray;
            [WriteOnly] public NativeReference<float3> PositionRef;
            void IJob.Execute()
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(RayValue, out float dist))
                {
                    float3 hit = RayValue.origin + RayValue.direction * dist;
                    float3 localPos = hit - (float3) MapSettings.Offset;
                    int2 coord = (int2)(new float2(localPos.x, localPos.z) / new float2(MapSettingsData.CellSize, MapSettingsData.CellSize));
                    int i = coord.y * MapSettings.Size.x + coord.x;
                    if (i<PositionArray.Length)
                    {
                        PositionRef.Value = PositionArray[i];
                    }
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
