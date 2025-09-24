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
    public partial struct EditGameStateSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<IS_EDIT_GAME_STATE>()
                .WithAbsent<IsEditGameStateCleanup>()
                .Build(ref state);
            
            state.RequireForUpdate(query);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            // var commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ProcessJob{
                commandBufferPW = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            var eiecbss = SystemAPI.GetSingleton<EndInitializationECBSystem.Singleton>();
            eiecbss.Append(commandBuffer, state.Dependency);
        }

        [WithAll(typeof(IS_EDIT_GAME_STATE))]
        [WithAbsent(typeof(IsEditGameStateCleanup))]
        [Unity.Burst.BurstCompile]
        partial struct ProcessJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBufferPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int entityIndexInQuery)
            {
                if(entityIndexInQuery==0)
                {
                    commandBufferPW.AddComponent<IsEditGameStateCleanup>(entityIndexInQuery, entity);
                    Debug.Log($"EDIT MODE begining... {entity}");
                }
                else
                {
                    Debug.Log($"Multiple EditModeData detected, destroying... {entity}");
                    commandBufferPW.DestroyEntity(entityIndexInQuery, entity);
                }
            }
        }
    }
}
