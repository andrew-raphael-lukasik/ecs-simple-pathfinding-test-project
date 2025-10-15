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
    public partial struct SelectedUnitPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var matLoadOp = Addressables.LoadAssetAsync<Material>("game-selection-lines.mat");

            state.RequireForUpdate<SelectedUnitSingleton>();

            Segments.Core.Create(out _segments, matLoadOp.WaitForCompletion());
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
            var bufferRW = segmentRef.ValueRW.Buffer;

            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit))
            {
                bufferRW.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);

                AABB aabb = SystemAPI.GetComponent<WorldRenderBounds>(selectedUnit).Value;
                
                bufferRW.Length += 12;
                Segments.Plot.Box(
                    segments: bufferRW.AsArray().Slice(bufferRW.Length - 12, 12),
                    size: aabb.Size * new float3(1.4f, 1, 1.4f) + new float3(1, 0, 1)*((float) math.sin(SystemAPI.Time.ElapsedTime*5f)*0.1f),
                    pos: aabb.Center,
                    rot: quaternion.identity
                );
            }
            else if(bufferRW.Length!=0)
            {
                bufferRW.Length = 0;
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
            }
        }
    }
}
