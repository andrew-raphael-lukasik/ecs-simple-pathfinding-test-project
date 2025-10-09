using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct PlayStateOnlySystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbpw = ecb.AsParallelWriter();

            state.Dependency = new DisableEditEntitiesJob{
                ECBPW = ecbpw,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new EnablePlayEntitiesJob{
                ECBPW = ecbpw,
            }.ScheduleParallel(state.Dependency);
        }

        [WithPresent(typeof(IsEditStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct DisableEditEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.SetEnabled(index, entity, false);
            }
        }

        [WithPresent(typeof(IsPlayStateOnly))]
        [WithOptions(EntityQueryOptions.IncludeDisabledEntities)]
        [Unity.Burst.BurstCompile]
        partial struct EnablePlayEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.SetEnabled(index, entity, true);
            }
        }
    }

    public struct IsPlayStateOnly : IComponentData {}

}
