using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace ServerAndClient.MonoBehaviours
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation| WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct SingleStateStartedGameObjectInitializationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<IsEditStateOnlyGameObject, IsPlayStateOnlyGameObject>()
                .WithAbsent<IsGameObjectEntityInitialized>()
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
                ecb.AddComponent<IsGameObjectEntityInitialized>(entity);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
            foreach (var (_, entity) in SystemAPI.Query<IsPlayStateOnlyGameObject>().WithEntityAccess())
            {
                GameObject go = entityManager.GetComponentObject<GameObject>(entity);
                go.SetActive(gameState==EGameState.PLAY);
                ecb.AddComponent<IsGameObjectEntityInitialized>(entity);
                Debug.Log($"{go.name} -> SetActive({go.activeSelf}), gameState: {gameState}");
            }
        }

        struct IsGameObjectEntityInitialized : IComponentData {}
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation| WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
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

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation| WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
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
