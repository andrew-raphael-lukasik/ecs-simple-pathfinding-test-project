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
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitEntitiesSystem : ISystem
    {
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
            var unitsRef = SystemAPI.GetSingletonRW<UnitsSingleton>();
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, unitsRef.ValueRW.Dependency);

            int requiredBufferLength = (int)(mapSettings.Size.x * mapSettings.Size.y);
            if (unitsRef.ValueRO.Lookup.Length!=requiredBufferLength)
            {
                unitsRef.ValueRW.Dependency.Complete();
                unitsRef.ValueRW.Lookup.Dispose();
                unitsRef.ValueRW.Lookup = new NativeArray<Entity>(requiredBufferLength, Allocator.Persistent);

                state.Dependency = new InvalidateAllUnitsJob{
                    ECB = ecb,
                }.Schedule(state.Dependency);
            }

            state.Dependency = new UnitEntityDestroyedJob{
                ECB = ecb,
                MapSize = mapSettings.Size,
                Units = unitsRef.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            state.Dependency = new AddValidityTagJob{
                ECB = ecb,
            }.Schedule(state.Dependency);

            state.Dependency = new UnitInvalidCoordJob{
                ECB = ecb,
                MapSize = mapSettings.Size,
                MapOrigin = mapSettings.Origin,
                Units = unitsRef.ValueRW.Lookup,
            }.Schedule(state.Dependency);

            #if UNITY_EDITOR || DEBUG
            state.Dependency = new AssertionsJob{
                Units = unitsRef.ValueRO.Lookup,
            }.ScheduleParallel(state.Dependency);
            #endif

            unitsRef.ValueRW.Dependency = state.Dependency;
        }

        [WithPresent(typeof(UnitCoord), typeof(LocalToWorld))]
        [WithAbsent(typeof(IsUnitCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct AddValidityTagJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute(in Entity entity)
            {
                ECB.AddComponent<IsUnitCoordValid>(entity);
                ECB.SetComponentEnabled<IsUnitCoordValid>(entity, false);
            }
        }

        [WithPresent(typeof(IsUnitCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct InvalidateAllUnitsJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public void Execute(in Entity entity)
            {
                ECB.SetComponentEnabled<IsUnitCoordValid>(entity, false);
            }
        }

        [WithDisabled(typeof(IsUnitCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct UnitInvalidCoordJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public uint2 MapSize;
            public float3 MapOrigin;
            public NativeArray<Entity> Units;
            public void Execute(ref UnitCoord coord, in LocalToWorld ltw, in Entity entity)
            {
                UnityEngine.Assertions.Assert.IsTrue(Units.Length==MapSize.x*MapSize.y, $"These do not match: Units.Length is {Units.Length} while MapSize is {MapSize}");

                uint2 prevCoord = coord;
                int prevIndex = GameGrid.ToIndex(prevCoord, MapSize);
                if (Units[prevIndex]==entity)
                {
                    Units[prevIndex] = Entity.Null;// detached self
                }

                uint2 newCoord = GameGrid.ToCoord(ltw.Position, MapOrigin, MapSize);
                int newIndex = GameGrid.ToIndex(newCoord, MapSize);
                // if (Units[newIndex]!=Entity.Null && Units[newIndex]!=entity)
                // {
                //     Entity other = Units[newIndex];// detached other
                // }
                Units[newIndex] = entity;
                coord = newCoord;
                ECB.SetComponentEnabled<IsUnitCoordValid>(entity, true);
            }
        }

        [WithAbsent(typeof(LocalToWorld))]
        [Unity.Burst.BurstCompile]
        partial struct UnitEntityDestroyedJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public uint2 MapSize;
            public NativeArray<Entity> Units;
            public void Execute(in UnitCoord coord, in Entity entity)
            {
                int index = GameGrid.ToIndex(coord, MapSize);
                if (Units[index]==entity)
                {
                    Units[index] = Entity.Null;
                }
                ECB.RemoveComponent<UnitCoord>(entity);
            }
        }

        #if UNITY_EDITOR || DEBUG
        [WithChangeFilter(typeof(UnitCoord))]
        [WithPresent(typeof(UnitCoord), typeof(LocalToWorld))]
        [WithAll(typeof(IsUnitCoordValid))]
        [Unity.Burst.BurstCompile]
        partial struct AssertionsJob : IJobEntity
        {
            [ReadOnly] public NativeArray<Entity> Units;
            public void Execute(in Entity entity)
            {
                if (!Units.Contains(entity))
                    Debug.LogError($"Unit {entity} is detached");   
            }
        }
        #endif

    }

    public struct UnitsSingleton : IComponentData
    {
        public NativeArray<Entity> Lookup;
        public JobHandle Dependency;
    }

}
