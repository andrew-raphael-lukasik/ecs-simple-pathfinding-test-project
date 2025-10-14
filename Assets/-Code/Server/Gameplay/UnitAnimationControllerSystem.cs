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
            foreach (var (controlsRef, presenter, ltw) in SystemAPI
                .Query< RefRW<UnitAnimationControls> , RefRW<UnitAnimationPresenter>, LocalToWorld>()
            )
            {
                var animator = presenter.ValueRW.Animator.Value;
                {
                    var controlsRO = controlsRef.ValueRO;

                    animator.SetFloat(UnitAnimationPresenter.id_move_speed, controlsRO.Speed);
                    animator.SetBool(UnitAnimationPresenter.id_is_aiming, controlsRO.IsAiming==1);
                    animator.SetBool(UnitAnimationPresenter.id_is_crouching, controlsRO.IsCrouching==1);

                    if (controlsRO.EventPistolAttack==1)
                    {
                        animator.SetTrigger(UnitAnimationPresenter.id_event_pistol_attack);
                        controlsRef.ValueRW.EventPistolAttack = 0;
                    }

                    if (controlsRO.EventHit==1)
                    {
                        animator.SetTrigger(UnitAnimationPresenter.id_event_hit);
                        controlsRef.ValueRW.EventHit = 0;
                    }

                    if (controlsRO.EventDeath==1)
                    {
                        animator.SetTrigger(UnitAnimationPresenter.id_event_death);
                        controlsRef.ValueRW.EventDeath = 0;
                    }
                }

                var transform = presenter.ValueRW.Transform.Value;
                transform.position = ltw.Position;
                transform.rotation = math.mul(ltw.Rotation, presenter.ValueRW.Rotation);
            }
        }
    }
}
