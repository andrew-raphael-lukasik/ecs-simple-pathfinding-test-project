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

            state.Dependency = new DisableEditEntitiesJob{
                ECB = ecb,
            }.Schedule(state.Dependency);

            state.Dependency = new EnablePlayEntitiesJob{
                ECB = ecb,
            }.Schedule(state.Dependency);
        }

        [WithPresent(typeof(Simulate), typeof(IsEditStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct DisableEditEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute (in Entity entity)
            {
                ECB.SetComponentEnabled<Simulate>(entity, false);
            }
        }

        [WithPresent(typeof(Simulate), typeof(IsPlayStateOnly))]
        [Unity.Burst.BurstCompile]
        partial struct EnablePlayEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute (in Entity entity)
            {
                ECB.SetComponentEnabled<Simulate>(entity, true);
            }
        }
    }

    public struct IsPlayStateOnly : IComponentData {}

}
