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
        NativeReference<bool> _rayHitRef;

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            _positionRef = new (Allocator.Persistent);
            _rayHitRef = new (Allocator.Persistent);
            state.RequireForUpdate<PlayerInputSingleton>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (_positionRef.IsCreated) _positionRef.Dispose();
            if (_rayHitRef.IsCreated) _rayHitRef.Dispose();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            var ray = playerInput.PointerRay;

            if (playerInput.IsPointerOverUI==1) return;

            if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

                state.Dependency = new GameGrid.RaycastJob{
                    RayValue = ray,
                    MapOrigin = mapSettings.Origin,
                    MapSize = mapSettings.Size,
                    PositionArray = mapData.PositionArray,
                    PositionRef = _positionRef,
                    RayHitRef = _rayHitRef,
                }.Schedule(state.Dependency);

                state.Dependency = new SetCursorPositionJob{
                    ElapsedTime = SystemAPI.Time.ElapsedTime,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    DestinationRef = _positionRef,
                }.ScheduleParallel(state.Dependency);
            }
            else
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float dist))
                {
                    _positionRef.Value = ray.origin + ray.direction * dist;
                    state.Dependency = new SetCursorPositionJob{
                        ElapsedTime = SystemAPI.Time.ElapsedTime,
                        DeltaTime = SystemAPI.Time.DeltaTime,
                        DestinationRef = _positionRef,
                    }.ScheduleParallel(state.Dependency);
                }
            }
        }

        [WithAny(typeof(IsEditModeCursor), typeof(IsPlayModeCursor))]
        [Unity.Burst.BurstCompile]
        partial struct SetCursorPositionJob : IJobEntity
        {
            public double ElapsedTime;
            public float DeltaTime;
            [ReadOnly] public NativeReference<float3> DestinationRef;
            public void Execute(ref LocalToWorld ltw)
            {
                // animated scale:
                float scale = 0.7f + (Easing.InOutElastic((float)math.sin(ElapsedTime*20f))*0.05f);
                ltw.Value.c0 = new float4(scale, 0, 0, 0);
                ltw.Value.c1 = new float4(0, 0.05f, 0, 0);
                ltw.Value.c2 = new float4(0, 0, scale, 0);

                // position:
                ltw.Value.c3 = new float4(DestinationRef.AsReadOnly().Value + new float3(0, 0.05f, 0), 1);

                #if UNITY_EDITOR
                Debug.DrawRay(DestinationRef.AsReadOnly().Value, Vector3.up, Color.cyan);
                #endif
            }
        }

    }
}
