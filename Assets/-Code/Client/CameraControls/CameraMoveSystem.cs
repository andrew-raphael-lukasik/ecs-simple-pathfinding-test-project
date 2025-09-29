using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Rendering;

using ServerAndClient.Input;
using ServerAndClient.Gameplay;
using ServerAndClient;

namespace Client.CameraControls
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
            state.RequireForUpdate<IsCameraTarget>();
            state.RequireForUpdate<PlayerInputData>();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            float2 move;
            {
                var playerInput = SystemAPI.GetSingleton<PlayerInputData>();
                move = playerInput.Move;
            }

            Bounds bounds;
            if (SystemAPI.TryGetSingleton<MapSettingsData>(out var mapSettings))
            {
                float3 start = mapSettings.Offset;
                float3 size = new float3(mapSettings.Size.x, 0, mapSettings.Size.y) * MapSettingsData.CellSize;
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

            Entity targetEntity = SystemAPI.GetSingletonEntity<IsCameraTarget>();
            Entity cameraEntity = SystemAPI.GetSingletonEntity<IsMainCamera>();
            
            var transform = state.EntityManager.GetComponentObject<Transform>(targetEntity);
            var cameraTransform = state.EntityManager.GetComponentObject<Camera>(cameraEntity).transform;

            Vector3 right = cameraTransform.right;
            right = math.normalizesafe(new float3(right.x, 0, right.z));

            Vector3 forward = cameraTransform.forward;
            forward = math.normalizesafe(new float3(forward.x, 0, forward.z));

            Vector3 newPosition = transform.position + right*move.x + forward*move.y;;
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
}
