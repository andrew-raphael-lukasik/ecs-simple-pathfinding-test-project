using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct SelectedFloorPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SelectedFloorSingleton>();

            var lineMat = Resources.Load<Material>("game-selection-lines");
            Segments.Core.Create(out _segments, lineMat);
            state.EntityManager.AddComponent<IsEditStateOnly>(_segments);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.Exists(_segments)) state.EntityManager.DestroyEntity(_segments);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedFloor = SystemAPI.GetSingleton<SelectedFloorSingleton>();
            if (selectedFloor!=Entity.Null && em.Exists(selectedFloor))
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, em);

                var aabb = GetTotalRenderBounds(em, selectedFloor);
                buffer.Length += 12;

                Segments.Plot.Box(
                    segments: buffer.AsArray().Slice(buffer.Length - 12, 12),
                    size: aabb.Size * 1.1f,
                    pos: aabb.Center,
                    rot: quaternion.identity
                );
            }
            else if(buffer.Length!=0)
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, em);
            }
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
