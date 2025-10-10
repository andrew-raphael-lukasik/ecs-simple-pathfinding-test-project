using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitInitializationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsUnitUninitialized>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<IsUnitUninitialized>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new UnitCoord{
                    Value = new uint2(uint.MaxValue, uint.MaxValue)
                });
                ecb.AddComponent<IsUnitCoordValid>(entity);
                ecb.SetComponentEnabled<IsUnitCoordValid>(entity, false);

                ecb.AddComponent(entity, new InMoveRange{
                    Coords = new (32, Allocator.Persistent),
                });
                ecb.AddComponent(entity, new InAttackRange{
                    Coords = new (32, Allocator.Persistent),
                });
                ecb.AddComponent(entity, new TargettingEnemy{
                    Value = Entity.Null,
                });

                ecb.RemoveComponent<IsUnitUninitialized>(entity);
            }
        }
    }

    public struct IsUnitUninitialized : IComponentData {}

}
