using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStateSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<GameState>(state.SystemHandle);
            SystemAPI.SetSingleton(new GameState{
                State = EGameState.UNDEFINED
            });

            state.RequireForUpdate<GameState.ChangeRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            // look for game state changes:
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            EGameState requestedMode = EGameState.UNDEFINED;
            int requestCounter = 0;
            foreach (var (request, entity) in SystemAPI.Query< RefRO<GameState.ChangeRequest> >().WithEntityAccess())
            {
                if(requestCounter++==0)
                {
                    requestedMode = request.ValueRO.State;
                    commandBuffer.DestroyEntity(entity);
                }
                else
                {
                    Debug.LogError($"{state.DebugName}: Multiple GameState.ChangeRequest detected! This will result in undefined behavior - fix asap.");
                    Debug.DebugBreak();
                }
            }

            switch (requestedMode)
            {
                case EGameState.EDIT:
                    state.EntityManager.AddComponent<GameState.EDIT_STARTED_EVENT>(state.SystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.EDIT_STARTED_EVENT.DebugName} created");
                    break;
                case EGameState.PLAY:
                    state.EntityManager.AddComponent<GameState.PLAY_STARTED_EVENT>(state.SystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.PLAY_STARTED_EVENT.DebugName} created");
                    break;
                default:
                    throw new System.NotImplementedException($"{requestedMode}");
            }

            if (commandBuffer.ShouldPlayback)
                commandBuffer.Playback(state.EntityManager);
        }

        [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
        [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
        [UpdateBefore(typeof(GameStateSystem))]
        [Unity.Burst.BurstCompile]
        public partial struct EventRemovalSystem : ISystem
        {
            [Unity.Burst.BurstCompile]
            void ISystem.OnUpdate(ref SystemState state)
            {
                var entityManager = state.EntityManager;
                var gameStateSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<GameStateSystem>();
                if (SystemAPI.HasComponent<GameState.EDIT_STARTED_EVENT>(gameStateSystemHandle))
                {
                    entityManager.RemoveComponent<GameState.EDIT_STARTED_EVENT>(gameStateSystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.EDIT_STARTED_EVENT.DebugName} has ended.");
                }
                if (SystemAPI.HasComponent<GameState.EDIT_ENDED_EVENT>(gameStateSystemHandle))
                {
                    entityManager.RemoveComponent<GameState.EDIT_ENDED_EVENT>(gameStateSystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.EDIT_ENDED_EVENT.DebugName} has ended.");
                }
                if (SystemAPI.HasComponent<GameState.PLAY_STARTED_EVENT>(gameStateSystemHandle))
                {
                    entityManager.RemoveComponent<GameState.PLAY_STARTED_EVENT>(gameStateSystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.PLAY_STARTED_EVENT.DebugName} has ended.");
                }
                if (SystemAPI.HasComponent<GameState.PLAY_ENDED_EVENT>(gameStateSystemHandle))
                {
                    entityManager.RemoveComponent<GameState.PLAY_ENDED_EVENT>(gameStateSystemHandle);
                    Debug.Log($"{state.DebugName}: {GameState.PLAY_ENDED_EVENT.DebugName} has ended.");
                }
            }
        }

    }
}
