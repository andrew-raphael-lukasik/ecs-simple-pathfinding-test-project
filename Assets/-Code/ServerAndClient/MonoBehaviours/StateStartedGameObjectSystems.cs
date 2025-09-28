using UnityEngine;
using Unity.Entities;

using ServerAndClient.Gameplay;

namespace ServerAndClient.MonoBehaviours
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct DisableAllStateStartedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(false);
            }
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(false);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct EditStateStartedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsEditStateOnlyGameObject>();
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(true);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct EditStateEndedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsEditStateOnlyGameObject>();
            state.RequireForUpdate<GameState.EDIT_ENDED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(false);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct PlayStateStartedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsPlayStateOnlyGameObject>();
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(true);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct PlayStateEndedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsPlayStateOnlyGameObject>();
            state.RequireForUpdate<GameState.PLAY_ENDED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                var go = state.EntityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(false);
            }
        }
    }
}
