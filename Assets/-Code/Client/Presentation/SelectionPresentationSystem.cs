using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct SelectionPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var lineMat = Resources.Load<Material>("game-selection-lines");
            Segments.Core.Create(out _segments, lineMat);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.Exists(_segments)) state.EntityManager.DestroyEntity(_segments);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var segments = Segments.Core.GetSegment(_segments, state.EntityManager);
            segments.Dependency.AsReadOnly().Value.Complete();
            bool preExistingLines = segments.Buffer.Length!=0;
            segments.Buffer.Length = 0;

            var selectedFloor = SystemAPI.GetSingleton<SelectedFloorSingleton>();
            if (selectedFloor.Selected!=Entity.Null && state.EntityManager.Exists(selectedFloor.Selected))
            {
                var aabb = GetTotalRenderBounds(state.EntityManager, selectedFloor.Selected);
                
                segments.Buffer.Length += 12;
                
                Segments.Plot.Box(
                    segments: segments.Buffer.AsArray().Slice(segments.Buffer.Length - 12, 12),
                    size: aabb.Size * 1.1f,
                    pos: aabb.Center,
                    rot: quaternion.identity
                );

                #if UNITY_EDITOR
                Debug.DrawRay(aabb.Center, Vector3.up, Color.cyan);
                #endif
            }

            var selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit.Selected!=Entity.Null && state.EntityManager.Exists(selectedUnit.Selected))
            {
                var aabb = GetTotalRenderBounds(state.EntityManager, selectedUnit.Selected);
                
                segments.Buffer.Length += 12;
                
                Segments.Plot.Box(
                    segments: segments.Buffer.AsArray().Slice(segments.Buffer.Length - 12, 12),
                    size: aabb.Size * new float3(1.4f, 1, 1.4f) + new float3(1, 0, 1)*((float) math.sin(SystemAPI.Time.ElapsedTime*5f)*0.1f),
                    pos: aabb.Center + new float3(0, 1, 0),
                    rot: quaternion.identity
                );

                #if UNITY_EDITOR
                Debug.DrawRay(aabb.Center, Vector3.up, Color.cyan);
                #endif
            }

            if (segments.Buffer.Length!=0 || preExistingLines)
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
        }

        AABB GetTotalRenderBounds(EntityManager entityManager, Entity entity)
        {
            Bounds bounds = default;
            if (entityManager.HasComponent<LinkedEntityGroup>(entity))
            {
                var list = entityManager.GetBuffer<LinkedEntityGroup>(entity);

                foreach (var item in list)
                if (entityManager.HasComponent<WorldRenderBounds>(item.Value))
                {
                    bounds = entityManager.GetComponentData<WorldRenderBounds>(item.Value).Value.ToBounds();
                    if (bounds.center!=Vector3.zero && bounds.extents!=Vector3.zero) break;
                }
                
                foreach (var item in list)
                if (entityManager.HasComponent<WorldRenderBounds>(item.Value))
                {
                    Bounds b = entityManager.GetComponentData<WorldRenderBounds>(item.Value).Value.ToBounds();
                    if (!(b.center==Vector3.zero && b.extents==Vector3.zero))
                        bounds.Encapsulate(b);
                }
            }
            else if (entityManager.HasComponent<WorldRenderBounds>(entity))
            {
                bounds = entityManager.GetComponentData<WorldRenderBounds>(entity).Value.ToBounds();
            }
            else if (entityManager.HasComponent<LocalToWorld>(entity))
            {
                bounds = new Bounds(entityManager.GetComponentData<LocalToWorld>(entity).Position, new Vector3(1, 1, 1));
            }
            return bounds.ToAABB();
        }
    }
}
