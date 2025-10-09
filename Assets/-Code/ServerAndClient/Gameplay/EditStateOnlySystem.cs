using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct EditStateOnlySystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbpw = ecb.AsParallelWriter();

            state.Dependency = new DisablePlayEntitiesJob{
                ECBPW = ecbpw,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new EnableEditEntitiesJob{
                ECBPW = ecbpw,
            }.ScheduleParallel(state.Dependency);
        }

        [WithPresent(typeof(IsPlayStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct DisablePlayEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.SetEnabled(index, entity, false);
            }
        }

        [WithPresent(typeof(IsEditStateOnly))]
        [WithOptions(EntityQueryOptions.IncludeDisabledEntities)]
        [Unity.Burst.BurstCompile]
        partial struct EnableEditEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECBPW;
            public void Execute(in Entity entity, [EntityIndexInQuery] int index)
            {
                ECBPW.SetEnabled(index, entity, true);
            }
        }
    }

    public struct IsEditStateOnly : IComponentData {}

}
