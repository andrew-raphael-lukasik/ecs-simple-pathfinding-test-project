using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace ServerAndClient.MonoBehaviours
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct SingletateStartedGameObjectInitializationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<IsEditStateOnlyGameObject, IsPlayStateOnlyGameObject>()
                .WithAbsent<IsInitialized>()
                .Build(ref state);
            state.RequireForUpdate(query);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            EGameState gameState = EGameState.UNDEFINED;
            if(SystemAPI.TryGetSingleton<GameState>(out var singleton))
            {
                gameState = singleton.State;
            }

            var entityManager = state.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.EDIT);
                ecb.AddComponent<IsInitialized>(entity);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.PLAY);
                ecb.AddComponent<IsInitialized>(entity);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
        }

        struct IsInitialized : IComponentData {}
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct EditStateStartedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            EGameState gameState = EGameState.UNDEFINED;
            if(SystemAPI.TryGetSingleton<GameState>(out var singleton))
            {
                gameState = singleton.State;
            }

            var entityManager = state.EntityManager;
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.EDIT);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.PLAY);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct PlayStateStartedGameObjectSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            EGameState gameState = EGameState.UNDEFINED;
            if(SystemAPI.TryGetSingleton<GameState>(out var singleton))
            {
                gameState = singleton.State;
            }

            var entityManager = state.EntityManager;
            foreach (var (_, entity) in SystemAPI.Query<IsEditStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.EDIT);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.PLAY);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
        }
    }

}
