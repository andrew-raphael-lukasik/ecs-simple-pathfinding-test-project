using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient.Input;
using ServerAndClient.Gameplay;
using ServerAndClient;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct CameraTargetMoveSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsCameraLookAtTarget>();
            state.RequireForUpdate<PlayerInputSingleton>();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            float2 move;
            {
                const float k_move_speed = 16f;
                var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
                move = playerInput.Move * SystemAPI.Time.DeltaTime * k_move_speed;
            }

            Bounds bounds;
            if (SystemAPI.TryGetSingleton<MapSettingsSingleton>(out var mapSettings))
            {
                float3 start = mapSettings.Origin;
                float3 size = new float3(mapSettings.Size.x, 0, mapSettings.Size.y) * MapSettingsSingleton.CellSize;
                float3 extents = size * 0.5f;
                bounds = new Bounds{
                    center = start + extents,
                    extents = extents,
                };
            }
            else
            {
                bounds = new Bounds{
                    center = Vector3.zero,
                    extents = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                };
            }

            Entity targetEntity = SystemAPI.GetSingletonEntity<IsCameraLookAtTarget>();
            Entity cameraEntity = SystemAPI.GetSingletonEntity<IsMainCamera>();
            
            var transform = state.EntityManager.GetComponentObject<Transform>(targetEntity);
            var cameraTransform = state.EntityManager.GetComponentObject<Camera>(cameraEntity).transform;

            Vector3 right = cameraTransform.right;
            right = math.normalizesafe(new float3(right.x, 0, right.z));

            Vector3 forward = cameraTransform.forward;
            forward = math.normalizesafe(new float3(forward.x, 0, forward.z));

            Vector3 newPosition = transform.position + right*move.x + forward*move.y;
            if (bounds.Contains(newPosition))
            {
                transform.position = newPosition;
            }
            else
            {
                transform.position = bounds.ClosestPoint(newPosition);
            }
        }
    }

    public struct IsMainCamera : IComponentData {}
    public struct IsCameraLookAtTarget : IComponentData {}

}
