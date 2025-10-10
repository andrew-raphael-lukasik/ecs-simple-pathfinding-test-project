using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct FloorInitializationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsFloorUninitialized>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<IsFloorUninitialized>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new FloorCoord{
                    Value = new uint2(uint.MaxValue, uint.MaxValue)
                });
                ecb.AddComponent<IsFloorCoordValid>(entity);
                ecb.SetComponentEnabled<IsFloorCoordValid>(entity, false);

                ecb.RemoveComponent<IsFloorUninitialized>(entity);
            }
        }
    }

    public struct IsFloorUninitialized : IComponentData {}

}
