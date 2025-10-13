using UnityEngine;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct GameObjectCleanupSystem : ISystem
    {
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (gameobjects, entity) in SystemAPI
                .Query< DynamicBuffer<GameObjectCleanup> >()
                .WithAbsent<Simulate>()
                .WithEntityAccess()
            )
            {
                foreach (GameObject go in gameobjects)
                    GameObject.Destroy(go);

                ecb.RemoveComponent<GameObjectCleanup>(entity);
            }
        }
    }
}
