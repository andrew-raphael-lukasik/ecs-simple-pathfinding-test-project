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
    public partial struct EditModeStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EditModeTag>()
                .WithAbsent<EditModeCleanupTag>()
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

        [WithAll(typeof(EditModeTag))]
        [WithAbsent(typeof(EditModeCleanupTag))]
        [Unity.Burst.BurstCompile]
        partial struct ProcessJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBufferPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int entityIndexInQuery)
            {
                if(entityIndexInQuery==0)
                {
                    commandBufferPW.AddComponent<EditModeCleanupTag>(entityIndexInQuery, entity);
                    
                    // create "edit mode started" event
                    commandBufferPW.AddComponent<EditModeStartedEventTag>(entityIndexInQuery, entity);
                    // Entity e = commandBufferPW.CreateEntity(entityIndexInQuery);
                    // commandBufferPW.AddComponent<EditModeStartedEventTag>(entityIndexInQuery, e);

                    Debug.Log($"EDIT MODE START begining... {entity}");
                }
                else
                {
                    Debug.Log($"Multiple EditModeData detected, destroying... {entity}");
                    commandBufferPW.DestroyEntity(entityIndexInQuery, entity);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EditModeStartSystem))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct EditModeStartEndsSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EditModeStartedEventTag>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            // var commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency= new StopEventJob{
                commandBufferPW = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();
            commandBuffer.Playback(state.EntityManager);
        }

        [WithAll(typeof(EditModeStartedEventTag))]
        [Unity.Burst.BurstCompile]
        partial struct StopEventJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBufferPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int entityIndexInQuery)
            {
                commandBufferPW.RemoveComponent<EditModeStartedEventTag>(-entityIndexInQuery, entity);
                Debug.Log($"EDIT MODE START ending... {entity}");
            }
        }
    }
}
