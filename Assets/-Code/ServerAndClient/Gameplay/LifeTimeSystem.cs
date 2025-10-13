using Unity.Entities;

namespace ServerAndClient.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct LifeTimeSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LifeTime>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (lifeTimeRef, entity) in SystemAPI.Query< RefRW<LifeTime> >().WithEntityAccess())
            {
                lifeTimeRef.ValueRW.Value -= SystemAPI.Time.DeltaTime;

                if (lifeTimeRef.ValueRO.Value<=0)
                    ecb.DestroyEntity(entity);
            }
        }
    }
}
