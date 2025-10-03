using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Server.Simulation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitEntitiesSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(UnitEntitiesSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<UnitCoord>();

            state.EntityManager.CreateSingleton(new UnitsSingleton{
                Lookup = new (32*32, Allocator.Persistent),
            });
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW<UnitsSingleton>(out var singleton))
            {
                if (singleton.ValueRW.Lookup.IsCreated) singleton.ValueRW.Lookup.Dispose();
            }
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var units = SystemAPI.GetSingletonRW<UnitsSingleton>();
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

            // update entities that moved:
            state.Dependency = new UnitMovedJob{
                MapSize = mapSettings.Size,
                MapOrigin = mapSettings.Origin,
                Units = units.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            // remove destroyed entities:
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            state.Dependency = new RemoveDestroyedUnitJob{
                Units = units.ValueRW.Lookup,
                ECB = ecb,
            }.Schedule(state.Dependency);

            units.ValueRW.Dependency = state.Dependency;
        }

        [WithChangeFilter(typeof(LocalToWorld))]
        [Unity.Burst.BurstCompile]
        partial struct UnitMovedJob : IJobEntity
        {
            public static FixedString64Bytes DebugName {get;} = nameof(UnitMovedJob);
            public uint2 MapSize;
            public float3 MapOrigin;
            public NativeHashMap<uint2, Entity> Units;
            public void Execute(ref UnitCoord coord, in LocalToWorld ltw, in Entity entity)
            {
                uint2 newCoord = GameGrid.ToCoord(ltw.Position, MapOrigin, MapSize);
                if (math.any(coord!=newCoord))// coord changed
                {
                    uint2 prevCoord = coord;
                    coord.Value = newCoord;

                    if (Units.TryGetValue(prevCoord, out Entity entityAtPrevCoord) && entityAtPrevCoord==entity)
                    {
                        Units.Remove(prevCoord);
                    }

                    if (Units.TryGetValue(newCoord, out Entity entityAtNewCoord))// potentially detaching a different entity
                    {
                        Units[newCoord] = entity;
                    }
                    else
                    {
                        Units.Add(newCoord, entity);
                    }
                }
                else if (!Units.ContainsKey(coord))// adding detached entity
                {
                    Units.Add(coord, entity);
                }
            }
        }

        [WithAbsent(typeof(Simulate))]
        [Unity.Burst.BurstCompile]
        partial struct RemoveDestroyedUnitJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<uint2, Entity> Units;
            public void Execute(in UnitCoord coord, in Entity entity)
            {
                if (Units.TryGetValue(coord, out Entity entityAtCoordNow) && entityAtCoordNow==entity)
                {
                    Units.Remove(coord);
                }
                ECB.RemoveComponent<UnitCoord>(entity);
            }
        }
    }

    public struct UnitsSingleton : IComponentData
    {
        public NativeHashMap<uint2, Entity> Lookup;
        public JobHandle Dependency;
    }

}
