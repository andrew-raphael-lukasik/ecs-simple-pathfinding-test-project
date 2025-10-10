using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct FloorEntitiesSystem : ISystem
    {
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
            var floorsRef = SystemAPI.GetSingletonRW<FloorsSingleton>();
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, floorsRef.ValueRW.Dependency);

            int requiredBufferLength = (int)(mapSettings.Size.x * mapSettings.Size.y);
            if (floorsRef.ValueRO.Lookup.Length!=requiredBufferLength)
            {
                floorsRef.ValueRW.Dependency.Complete();
                floorsRef.ValueRW.Lookup.Dispose();
                floorsRef.ValueRW.Lookup = new NativeArray<Entity>(requiredBufferLength, Allocator.Persistent);

                state.Dependency = new InvalidateAllFloorsJob{
                    ECB = ecb,
                }.Schedule(state.Dependency);
            }

            state.Dependency = new FloorEntityDestroyedJob{
                ECB = ecb,
                MapSize = mapSettings.Size,
                Floors = floorsRef.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            state.Dependency = new AddValidityTagJob{
                ECB = ecb,
            }.Schedule(state.Dependency);

            state.Dependency = new FloorInvalidCoordJob{
                ECB = ecb,
                MapSize = mapSettings.Size,
                MapOrigin = mapSettings.Origin,
                Floors = floorsRef.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            #if UNITY_EDITOR || DEBUG
            state.Dependency = new AssertionsJob{
                Floors = floorsRef.ValueRO.Lookup,
            }.ScheduleParallel(state.Dependency);
            #endif

            floorsRef.ValueRW.Dependency = state.Dependency;
        }

        [WithPresent(typeof(FloorCoord), typeof(LocalToWorld))]
        [WithAbsent(typeof(IsFloorCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct AddValidityTagJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute(in Entity entity)
            {
                ECB.AddComponent<IsFloorCoordValid>(entity);
                ECB.SetComponentEnabled<IsFloorCoordValid>(entity, false);
            }
        }

        [WithPresent(typeof(IsFloorCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct InvalidateAllFloorsJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute(in Entity entity)
            {
                ECB.SetComponentEnabled<IsFloorCoordValid>(entity, false);
            }
        }

        [WithDisabled(typeof(IsFloorCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct FloorInvalidCoordJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public uint2 MapSize;
            public float3 MapOrigin;
            public NativeArray<Entity> Floors;
            public void Execute(ref FloorCoord coord, in LocalToWorld ltw, in Entity entity)
            {
                uint2 prevCoord = coord;
                int prevIndex = GameGrid.ToIndex(prevCoord, MapSize);
                if (Floors[prevIndex]==entity)
                {
                    Floors[prevIndex] = Entity.Null;// detached self
                }

                uint2 newCoord = GameGrid.ToCoord(ltw.Position, MapOrigin, MapSize);
                int newIndex = GameGrid.ToIndex(newCoord, MapSize);
                // if (Floors[newIndex]!=Entity.Null && Floors[newIndex]!=entity)
                // {
                //     Entity other = Floors[newIndex];// detached other
                // }
                Floors[newIndex] = entity;
                coord = newCoord;
                ECB.SetComponentEnabled<IsFloorCoordValid>(entity, true);
            }
        }

        [WithAbsent(typeof(LocalToWorld))]
        [Unity.Burst.BurstCompile]
        partial struct FloorEntityDestroyedJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public uint2 MapSize;
            public NativeArray<Entity> Floors;
            public void Execute(in FloorCoord coord, in Entity entity)
            {
                int index = GameGrid.ToIndex(coord, MapSize);
                if (
                        index<Floors.Length// map size changed singe job started
                    &&  Floors[index]==entity
                )
                {
                    Floors[index] = Entity.Null;
                }
                ECB.RemoveComponent<FloorCoord>(entity);
            }
        }

        #if UNITY_EDITOR || DEBUG
        [WithChangeFilter(typeof(FloorCoord))]
        [WithPresent(typeof(FloorCoord), typeof(LocalToWorld))]
        [WithAll(typeof(IsFloorCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct AssertionsJob : IJobEntity
        {
            [ReadOnly] public NativeArray<Entity> Floors;
            public void Execute(in Entity entity)
            {
                if (!Floors.Contains(entity))
                    Debug.LogError($"Floor {entity} is detached");   
            }
        }
        #endif

    }

    public struct FloorsSingleton : IComponentData
    {
        public NativeArray<Entity> Lookup;
        public JobHandle Dependency;
    }

}
