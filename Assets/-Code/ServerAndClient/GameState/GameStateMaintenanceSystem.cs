using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ServerAndClient.GameState
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct GameStateMaintenanceSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IS_GAME_STATE>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            // remove active events:
            state.EntityManager.RemoveComponent<EDIT_STATE_START_EVENT>(state.SystemHandle);
            state.EntityManager.RemoveComponent<PLAY_STATE_START_EVENT>(state.SystemHandle);

            // look for game state changes:
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            {
                byte createEvent = 0;
                int counter = 0;
                foreach (var (_, entity) in SystemAPI.Query< RefRO<IS_EDIT_GAME_STATE> >().WithAbsent<IS_EDIT_GAME_STATE_CLEANUP>().WithEntityAccess())
                {
                    if(counter++==0)
                    {
                        commandBuffer.AddComponent<IS_EDIT_GAME_STATE_CLEANUP>(entity);
                        createEvent = 1;
                        Debug.Log($"EDIT MODE begins {entity}");
                    }
                    else
                    {
                        Debug.Log($"Multiple EDIT MODE entities detected, destroying {entity}");
                        commandBuffer.DestroyEntity(entity);
                    }
                }
                
                // create EDIT MODE EVENT
                if (createEvent==1)
                {
                    state.EntityManager.AddComponent<EDIT_STATE_START_EVENT>(state.SystemHandle);
                }
            }
            {
                byte createEvent = 0;
                int counter = 0;
                foreach (var (_, entity) in SystemAPI.Query< RefRO<IS_PLAY_GAME_STATE> >().WithAbsent<IS_PLAY_GAME_STATE_CLEANUP>().WithEntityAccess())
                {
                    if(counter++==0)
                    {
                        commandBuffer.AddComponent<IS_PLAY_GAME_STATE_CLEANUP>(entity);
                        createEvent = 1;
                        Debug.Log($"PLAY MODE begins {entity}");
                    }
                    else
                    {
                        Debug.Log($"Multiple PLAY MODE entities detected, destroying {entity}");
                        commandBuffer.DestroyEntity(entity);
                    }
                }

                // create PLAY MODE EVENT
                if (createEvent==1)
                {
                    state.EntityManager.AddComponent<PLAY_STATE_START_EVENT>(state.SystemHandle);
                }
            }

            if (commandBuffer.ShouldPlayback)
                commandBuffer.Playback(state.EntityManager);
        }
    }
}
