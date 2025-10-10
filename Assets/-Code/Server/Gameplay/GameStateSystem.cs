using UnityEngine;
using Unity.Entities;
using Unity.Collections;

using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStateSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new GameState{
                State = EGameState.UNDEFINED
            });

            state.RequireForUpdate<GameState.ChangeRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            // look for game state changes:
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            EGameState requestedMode = EGameState.UNDEFINED;
            int requestCounter = 0;
            foreach (var (request, entity) in SystemAPI.Query< RefRO<GameState.ChangeRequest> >().WithEntityAccess())
            {
                if(requestCounter++==0)
                {
                    requestedMode = request.ValueRO.State;
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    Debug.LogError($"{state.DebugName}: Multiple GameState.ChangeRequest detected! This will result in undefined behavior - fix asap.");
                    Debug.DebugBreak();
                }
            }

            // update singleton:
            SystemAPI.SetSingleton(new GameState{
                State = requestedMode
            });
            Debug.Log($"{state.DebugName}: Game state changed to: {requestedMode}");

            // raise state start event:
            switch (requestedMode)
            {
                case EGameState.EDIT:
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.PLAY>(out Entity entity))
                    {
                        em.DestroyEntity(entity);
                    }

                    if (!SystemAPI.HasSingleton<GameState.EDIT>()) em.CreateSingleton<GameState.EDIT>();
                    if (!SystemAPI.HasSingleton<GameState.EDIT_STARTED_EVENT>()) em.CreateSingleton<GameState.EDIT_STARTED_EVENT>();
                    Debug.Log($"{state.DebugName}: {GameState.EDIT.DebugName} & {GameState.EDIT_STARTED_EVENT.DebugName} created");
                }
                break;
                case EGameState.PLAY:
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.EDIT>(out Entity entity))
                    {
                        em.DestroyEntity(entity);
                    }
                    if (!SystemAPI.HasSingleton<GameState.PLAY>()) em.CreateSingleton<GameState.PLAY>();
                    if (!SystemAPI.HasSingleton<GameState.PLAY_STARTED_EVENT>()) em.CreateSingleton<GameState.PLAY_STARTED_EVENT>();
                    Debug.Log($"{state.DebugName}: {GameState.PLAY.DebugName} & {GameState.PLAY_STARTED_EVENT.DebugName} created");
                }
                break;
                default: throw new System.NotImplementedException($"{requestedMode}");
            }

            if (ecb.ShouldPlayback) ecb.Playback(em);
        }

        [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Editor)]
        [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
        [UpdateBefore(typeof(GameStateSystem))]
        [Unity.Burst.BurstCompile]
        public partial struct EventRemovalSystem : ISystem
        {
            [Unity.Burst.BurstCompile]
            void ISystem.OnUpdate(ref SystemState state)
            {
                // end active events:
                var em = state.EntityManager;
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.EDIT_STARTED_EVENT>(out Entity singleton))
                    {
                        em.DestroyEntity(singleton);
                        Debug.Log($"{state.DebugName}: {GameState.EDIT_STARTED_EVENT.DebugName} has ended.");
                    }
                }
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.EDIT_ENDED_EVENT>(out Entity singleton))
                    {
                        em.DestroyEntity(singleton);
                        Debug.Log($"{state.DebugName}: {GameState.EDIT_ENDED_EVENT.DebugName} has ended.");
                    }
                }
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.PLAY_STARTED_EVENT>(out Entity singleton))
                    {
                        em.DestroyEntity(singleton);
                        Debug.Log($"{state.DebugName}: {GameState.PLAY_STARTED_EVENT.DebugName} has ended.");
                    }
                }
                {
                    if (SystemAPI.TryGetSingletonEntity<GameState.PLAY_ENDED_EVENT>(out Entity singleton))
                    {
                        em.DestroyEntity(singleton);
                        Debug.Log($"{state.DebugName}: {GameState.PLAY_ENDED_EVENT.DebugName} has ended.");
                    }
                }
            }
        }

    }
}
