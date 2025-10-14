using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

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
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputSingleton>();
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
                if (GameGrid.Raycast(ray, mapSettings.Origin, mapSettings.Size, out int i))
                {
                    float3 pos = mapData.PositionArray[i];
                    state.Dependency = new SetCursorLocalTransformJob{
                        ElapsedTime = SystemAPI.Time.ElapsedTime,
                        Position = pos,
                    }.ScheduleParallel(state.Dependency);
                }
            }
            else
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float dist))
                {
                    float3 pos = ray.origin + ray.direction * dist;
                    state.Dependency = new SetCursorLocalTransformJob{
                        ElapsedTime = SystemAPI.Time.ElapsedTime,
                        Position = pos,
                    }.ScheduleParallel(state.Dependency);
                }
            }
        }

        [WithPresent(typeof(IsCursor))]
        [Unity.Burst.BurstCompile]
        partial struct SetCursorLocalTransformJob : IJobEntity
        {
            public double ElapsedTime;
            public float3 Position;
            public void Execute(ref LocalTransform lt)
            {
                lt.Scale = 0.7f + (Easing.InOutElastic((float)math.sin(ElapsedTime*20f))*0.05f);
                lt.Position = Position + new float3(0, 0.05f, 0);

                #if UNITY_EDITOR
                Debug.DrawRay(Position, Vector3.up, Color.cyan);
                #endif
            }
        }
    }

    public struct IsCursor : IComponentData {}

}
