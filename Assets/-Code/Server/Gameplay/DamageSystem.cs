using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Presentation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Damage>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (healthRef, damageBuf, animControlsRef, entity) in SystemAPI
                .Query< RefRW<Health>, DynamicBuffer<Damage>, RefRW<UnitAnimationControls> >()
                .WithEntityAccess()
            )
            {
                uint damageDealtTotal = 0;
                foreach (var damage in damageBuf)
                {
                    uint damageDealt = (uint) math.min((int) damage.Amount, (int) healthRef.ValueRW.Value);;
                    damageDealtTotal += damageDealt;

                    healthRef.ValueRW.Value -= (ushort) damageDealt;
                    animControlsRef.ValueRW.EventHit = 1;

                    UnityEngine.Debug.Log($"({entity.Index}:{entity.Version}) attacked by ({damage.Instigator.Index}:{damage.Instigator.Version}), damage: {damage.Amount} ({damage.TypeMask}), health changed to {healthRef.ValueRO.Value}");
                }
                damageBuf.Clear();

                if (damageDealtTotal>0)
                {
                    var prefabsRef = SystemAPI.GetSingletonRW<PrefabSystem.Prefabs>();
                    prefabsRef.ValueRW.Dependency.Complete();
                    if (prefabsRef.ValueRW.Lookup.TryGetValue("pistol-hit-fx", out Entity prefab))
                    {
                        Entity instance = ecb.Instantiate(prefab);

                        var ltw = SystemAPI.GetComponentRO<LocalToWorld>(entity);
                        if (SystemAPI.HasComponent<LocalTransform>(prefab))
                        {
                            ecb.SetComponent(instance, new LocalTransform{
                                Position = ltw.ValueRO.Position + new float3(0, 1, 0),
                                Rotation = ltw.ValueRO.Rotation,
                                Scale = 1,
                            });
                        }
                        else ecb.SetComponent(instance, ltw.ValueRO);
                    }
                }

                if (healthRef.ValueRO==0)
                {
                    animControlsRef.ValueRW.EventDeath = 1;
                    ecb.RemoveComponent<IsUnit>(entity);
                    ecb.RemoveComponent<Health>(entity);
                    ecb.RemoveComponent<Damage>(entity);
                    ecb.RemoveComponent<InMoveRange>(entity);
                    ecb.RemoveComponent<InAttackRange>(entity);
                    ecb.RemoveComponent<TargettingEnemy>(entity);

                    UnityEngine.Debug.Log($"({entity.Index}:{entity.Version})'s health is 0, destroying");
                }
            }
        }
    }
}
