using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Presentation;

namespace Client.Animation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitAnimationControllerSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitAnimationPresenter>();
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (controls, presenter, ltw) in SystemAPI
                .Query< RefRO<UnitAnimationControls> , RefRW<UnitAnimationPresenter>, LocalToWorld>()
            )
            {
                var animator = presenter.ValueRW.Animator.Value;
                animator.SetFloat(UnitAnimationPresenter.id_speed, controls.ValueRO.Speed);

                var transform = presenter.ValueRW.Transform.Value;
                transform.position = ltw.Position;
                transform.rotation = math.mul(ltw.Rotation, presenter.ValueRW.Rotation);
            }
        }
    }
}
