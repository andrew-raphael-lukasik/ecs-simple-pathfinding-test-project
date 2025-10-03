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
    public partial struct FloorEntitiesSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(FloorEntitiesSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<FloorCoord>();

            state.EntityManager.CreateSingleton(new FloorsSingleton{
                Lookup = new (32*32, Allocator.Persistent),
            });
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW<FloorsSingleton>(out var singleton))
            {
                if (singleton.ValueRW.Lookup.IsCreated) singleton.ValueRW.Lookup.Dispose();
            }
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var floors = SystemAPI.GetSingletonRW<FloorsSingleton>();
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

            // update entities that moved:
            state.Dependency = new FloorMovedJob{
                MapSize = mapSettings.Size,
                MapOrigin = mapSettings.Origin,
                Floors = floors.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            // remove destroyed entities:
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            state.Dependency = new RemoveDestroyedFloorJob{
                Floors = floors.ValueRW.Lookup,
                ECB = ecb,
            }.Schedule(state.Dependency);

            floors.ValueRW.Dependency = state.Dependency;
        }

        [WithChangeFilter(typeof(LocalToWorld))]
        [Unity.Burst.BurstCompile]
        partial struct FloorMovedJob : IJobEntity
        {
            public static FixedString64Bytes DebugName {get;} = nameof(FloorMovedJob);
            public uint2 MapSize;
            public float3 MapOrigin;
            public NativeHashMap<uint2, Entity> Floors;
            public void Execute(ref FloorCoord coord, in LocalToWorld ltw, in Entity entity)
            {
                uint2 newCoord = GameGrid.ToCoord(ltw.Position, MapOrigin, MapSize);
                if (math.any(coord!=newCoord))// coord changed
                {
                    uint2 prevCoord = coord;
                    coord.Value = newCoord;

                    if (Floors.TryGetValue(prevCoord, out Entity entityAtPrevCoord) && entityAtPrevCoord==entity)
                    {
                        Floors.Remove(prevCoord);
                    }

                    if (Floors.TryGetValue(newCoord, out Entity entityAtNewCoord))// potentially detaching a different entity
                    {
                        Floors[newCoord] = entity;
                    }
                    else
                    {
                        Floors.Add(newCoord, entity);
                    }
                }
                else if (!Floors.ContainsKey(coord))// adding detached entity
                {
                    Floors.Add(coord, entity);
                }
            }
        }

        [WithAbsent(typeof(Simulate))]
        [Unity.Burst.BurstCompile]
        partial struct RemoveDestroyedFloorJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<uint2, Entity> Floors;
            public void Execute(in FloorCoord coord, in Entity entity)
            {
                if (Floors.TryGetValue(coord, out Entity entityAtCoordNow) && entityAtCoordNow==entity)
                {
                    Floors.Remove(coord);
                }
                ECB.RemoveComponent<FloorCoord>(entity);
            }
        }
    }

    public struct FloorsSingleton : IComponentData
    {
        public NativeHashMap<uint2, Entity> Lookup;
        public JobHandle Dependency;
    }

}
