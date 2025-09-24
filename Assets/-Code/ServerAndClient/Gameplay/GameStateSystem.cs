using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ServerAndClient.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
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

            state.RequireForUpdate<GameStateChangeRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            // remove active events:
            state.EntityManager.RemoveComponent<GameState.EDIT_STARTED_EVENT>(state.SystemHandle);
            state.EntityManager.RemoveComponent<GameState.EDIT_ENDED_EVENT>(state.SystemHandle);
            state.EntityManager.RemoveComponent<GameState.PLAY_STARTED_EVENT>(state.SystemHandle);
            state.EntityManager.RemoveComponent<GameState.PLAY_ENDED_EVENT>(state.SystemHandle);

            // look for game state changes:
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            EGameState requestedMode = EGameState.UNDEFINED;
            int requestCounter = 0;
            foreach (var (request, entity) in SystemAPI.Query< RefRO<GameStateChangeRequest> >().WithEntityAccess())
            {
                if(requestCounter++==0)
                {
                    requestedMode = request.ValueRO.State;
                    commandBuffer.DestroyEntity(entity);
                }
                else
                {
                    Debug.LogError($"Multiple GameStateChangeRequest detected! This will result in undefined behavior - fix asap.");
                    Debug.DebugBreak();
                }
            }

            switch (requestedMode)
            {
                case EGameState.EDIT:
                    state.EntityManager.AddComponent<GameState.EDIT_STARTED_EVENT>(state.SystemHandle);
                    Debug.Log($"{nameof(GameState.EDIT_STARTED_EVENT)} created");
                    break;
                case EGameState.PLAY:
                    state.EntityManager.AddComponent<GameState.PLAY_STARTED_EVENT>(state.SystemHandle);
                    Debug.Log($"{nameof(GameState.PLAY_STARTED_EVENT)} created");
                    break;
                default:
                    throw new System.NotImplementedException($"{requestedMode}");
            }

            if (commandBuffer.ShouldPlayback)
                commandBuffer.Playback(state.EntityManager);
        }
    }
}
