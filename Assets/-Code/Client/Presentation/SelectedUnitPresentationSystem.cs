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
    public partial struct SelectedUnitPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SelectedUnitSingleton>();

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
            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit))
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

                AABB aabb;
                {
                    Bounds bounds = default;
                    if (state.EntityManager.HasComponent<LinkedEntityGroup>(selectedUnit))
                    {
                        var list = SystemAPI.GetBuffer<LinkedEntityGroup>(selectedUnit);

                        foreach (var item in list)
                        if (SystemAPI.HasComponent<WorldRenderBounds>(item.Value))
                        {
                            bounds = SystemAPI.GetComponent<WorldRenderBounds>(item.Value).Value.ToBounds();
                            if (bounds.center!=Vector3.zero && bounds.extents!=Vector3.zero) break;
                        }
                        
                        foreach (var item in list)
                        if (SystemAPI.HasComponent<WorldRenderBounds>(item.Value))
                        {
                            Bounds b = SystemAPI.GetComponent<WorldRenderBounds>(item.Value).Value.ToBounds();
                            if (!(b.center==Vector3.zero && b.extents==Vector3.zero))
                                bounds.Encapsulate(b);
                        }
                    }
                    else if (SystemAPI.HasComponent<WorldRenderBounds>(selectedUnit))
                    {
                        bounds = SystemAPI.GetComponent<WorldRenderBounds>(selectedUnit).Value.ToBounds();
                    }
                    else if (SystemAPI.HasComponent<LocalToWorld>(selectedUnit))
                    {
                        bounds = new Bounds(SystemAPI.GetComponent<LocalToWorld>(selectedUnit).Position, new Vector3(1, 1, 1));
                    }
                    aabb = bounds.ToAABB();
                }
                aabb.Extents = new float3(.5f, aabb.Extents.y, .5f);
                buffer.Length += 12;

                Segments.Plot.Box(
                    segments: buffer.AsArray().Slice(buffer.Length - 12, 12),
                    size: aabb.Size * new float3(1.4f, 1, 1.4f) + new float3(1, 0, 1)*((float) math.sin(SystemAPI.Time.ElapsedTime*5f)*0.1f),
                    pos: aabb.Center + new float3(0, 1, 0),
                    rot: quaternion.identity
                );
            }
            else if(buffer.Length!=0)
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }
        }
    }
}
