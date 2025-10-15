using UnityEngine;
using UnityEngine.AddressableAssets;
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
            var matLoadOp = Addressables.LoadAssetAsync<Material>("game-selection-lines.mat");

            state.RequireForUpdate<SelectedFloorSingleton>();

            Segments.Core.Create(out _segments, matLoadOp.WaitForCompletion());
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
            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;

            Entity selectedFloor = SystemAPI.GetSingleton<SelectedFloorSingleton>();
            if (selectedFloor!=Entity.Null && SystemAPI.Exists(selectedFloor))
            {
                buffer.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

                AABB aabb;
                #region get_world_bounds
                {
                    Bounds bounds = default;
                    if (state.EntityManager.HasComponent<LinkedEntityGroup>(selectedFloor))
                    {
                        var list = SystemAPI.GetBuffer<LinkedEntityGroup>(selectedFloor);

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
                    else if (SystemAPI.HasComponent<WorldRenderBounds>(selectedFloor))
                    {
                        bounds = SystemAPI.GetComponent<WorldRenderBounds>(selectedFloor).Value.ToBounds();
                    }
                    else if (SystemAPI.HasComponent<LocalToWorld>(selectedFloor))
                    {
                        bounds = new Bounds(SystemAPI.GetComponent<LocalToWorld>(selectedFloor).Position, new Vector3(1, 1, 1));
                    }
                    aabb = bounds.ToAABB();
                }
                #endregion

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
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }
        }
    }
}
