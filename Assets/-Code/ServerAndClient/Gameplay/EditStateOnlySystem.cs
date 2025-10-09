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

            state.Dependency = new DisablePlayEntitiesJob{
                ECB = ecb,
            }.Schedule(state.Dependency);

            state.Dependency = new EnableEditEntitiesJob{
                ECB = ecb,
            }.Schedule(state.Dependency);
        }

        [WithPresent(typeof(Simulate), typeof(IsPlayStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct DisablePlayEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute (in Entity entity)
            {
                ECB.SetComponentEnabled<Simulate>(entity, false);
            }
        }

        [WithPresent(typeof(Simulate), typeof(IsEditStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct EnableEditEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute (in Entity entity)
            {
                ECB.SetComponentEnabled<Simulate>(entity, true);
            }
        }
    }

    public struct IsEditStateOnly : IComponentData {}

}
