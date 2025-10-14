using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Presentation;
using Client.Animation;// @TODO: remove client-side code

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitInitializationSystem : ISystem
    {
        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsUnitUninitialized>();

            UnitAnimationPresenter.id_move_speed = Animator.StringToHash("move_speed");
            UnitAnimationPresenter.id_is_crouching = Animator.StringToHash("is_crouching");
            UnitAnimationPresenter.id_is_aiming = Animator.StringToHash("is_aiming");
            UnitAnimationPresenter.id_event_pistol_attack = Animator.StringToHash("event_pistol_attack");
            UnitAnimationPresenter.id_event_melee_attack = Animator.StringToHash("event_melee_attack");
            UnitAnimationPresenter.id_event_interact = Animator.StringToHash("event_interact");
            UnitAnimationPresenter.id_event_hit = Animator.StringToHash("event_hit");
            UnitAnimationPresenter.id_event_death = Animator.StringToHash("event_death");
            UnitAnimationPresenter.id_event_revived = Animator.StringToHash("event_revived");
        }

        // [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<IsUnitUninitialized>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new UnitCoord{
                    Value = new uint2(uint.MaxValue, uint.MaxValue)
                });
                ecb.AddComponent<IsUnitCoordValid>(entity);
                ecb.SetComponentEnabled<IsUnitCoordValid>(entity, false);

                ecb.AddComponent(entity, new Health{
                    Value = 100,
                });
                ecb.AddBuffer<Damage>(entity);

                ecb.AddComponent(entity, new InMoveRange{
                    Coords = new (32, Allocator.Persistent),
                });
                ecb.AddComponent(entity, new InAttackRange{
                    Coords = new (32, Allocator.Persistent),
                });
                ecb.AddComponent(entity, new TargettingEnemy{
                    Value = Entity.Null,
                });

                var animatorPrefab = SystemAPI.GetComponent<AnimatorPrefab>(entity);
                {
                    var go = GameObject.Instantiate<GameObject>(animatorPrefab.Prefab);

                    #if UNITY_EDITOR
                    go.name = $"Animator for ({entity.Index}:{entity.Version})";
                    #endif

                    var animator = go.GetComponent<Animator>();
                    ecb.AddComponent(entity, new UnitAnimationControls{
                        Speed = 0,
                    });
                    ecb.AddComponent(entity, new UnitAnimationPresenter{
                        Animator = animator,
                        Transform = animator.transform,
                        Rotation = animatorPrefab.PrefabRotation,
                    });
                    var gameObjects = ecb.AddBuffer<GameObjectCleanup>(entity);
                    gameObjects.Add(animator.gameObject);

                    animator.SetFloat(UnitAnimationPresenter.id_move_speed, 1.0f);
                }
                ecb.RemoveComponent<AnimatorPrefab>(entity);

                ecb.RemoveComponent<IsUnitUninitialized>(entity);
            }
        }
    }

    public struct IsUnitUninitialized : IComponentData {}

}
