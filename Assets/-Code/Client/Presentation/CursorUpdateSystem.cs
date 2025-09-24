using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

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
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PointerPositionData>();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera!=null)
            {
                var pointerData = SystemAPI.GetSingleton<PointerPositionData>();
                var ray = camera.ScreenPointToRay(pointerData.Value);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float dist))
                {
                    new SetCursorPositionJob{
                        Position = ray.origin + ray.direction * dist,
                    }.ScheduleParallel();
                }
            }
        }

        [WithAny(typeof(IsEditModeCursor), typeof(IsPlayModeCursor))]
        [Unity.Burst.BurstCompile]
        partial struct SetCursorPositionJob : IJobEntity
        {
            public float3 Position;
            public void Execute(ref LocalToWorld transform)
            {
                transform.Value.c3 = new float4(Position, 1);
            }
        }
    }
}
