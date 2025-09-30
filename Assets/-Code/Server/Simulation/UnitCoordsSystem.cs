using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Server.Simulation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct UnitCoordsSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(UnitCoordsSystem);

        byte _initialized;

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<UnitCoord>();

            state.EntityManager.AddComponent<UnitCoordsSingleton>(state.SystemHandle);
            SystemAPI.SetSingleton(new UnitCoordsSingleton{
                Lookup = new (32*32, Allocator.Persistent),
                Dependency = new (Allocator.Persistent),
            });
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<UnitCoordsSingleton>(out var singleton))
            {
                if (singleton.Dependency.IsCreated)
                {
                    singleton.Dependency.AsReadOnly().Value.Complete();
                    singleton.Dependency.Dispose();
                }
                if (singleton.Lookup.IsCreated) singleton.Lookup.Dispose();
            }
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<UnitCoordsSingleton>();
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

            if (_initialized==0)
            {
                state.Dependency = new InitializationJob{
                    Lookup = singleton.Lookup,
                }.Schedule(state.Dependency);
                _initialized = 1;
            }

            // update entities that moved:
            state.Dependency = new EntityMovedJob{
                MapSize = mapSettings.Size,
                MapOrigin = mapSettings.Origin,
                Lookup = singleton.Lookup,
            }.Schedule(state.Dependency);

            // remove destroyed entities:
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            state.Dependency = new RemoveDestroyedJob{
                Lookup = singleton.Lookup,
                ECB = ecb,
            }.Schedule(state.Dependency);

            // update public dependency:
            singleton.Dependency.Value = state.Dependency;
        }

        [WithPresent(typeof(Simulate))]
        [Unity.Burst.BurstCompile]
        partial struct InitializationJob : IJobEntity
        {
            public NativeHashMap<uint2, Entity> Lookup;
            public void Execute(in UnitCoord coord, in Entity entity)
            {
                Lookup.Add(coord, entity);
            }
        }

        [WithChangeFilter(typeof(LocalToWorld))]
        [Unity.Burst.BurstCompile]
        partial struct EntityMovedJob : IJobEntity
        {
            public uint2 MapSize;
            public float3 MapOrigin;
            public NativeHashMap<uint2, Entity> Lookup;
            public void Execute(ref UnitCoord coord, in LocalToWorld ltw, in Entity entity)
            {
                float3 newPosition = ltw.Position;
                float3 localPos = newPosition - MapOrigin;

                uint2 mewCoord = (uint2)(new float2(localPos.x, localPos.z) / new float2(MapSettingsSingleton.CellSize, MapSettingsSingleton.CellSize));
                mewCoord = math.min(mewCoord, MapSize-1);// clamp to map size

                if (math.any(coord.Value!=mewCoord))// has coord changed
                {
                    Lookup.Remove(coord);
                    coord = mewCoord;
                    Lookup.Add(coord, entity);
                }
            }
        }

        [WithAbsent(typeof(Simulate))]
        [Unity.Burst.BurstCompile]
        partial struct RemoveDestroyedJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public NativeHashMap<uint2, Entity> Lookup;
            public void Execute(in UnitCoord coord, in Entity entity)
            {
                Lookup.Remove(coord);
                ECB.RemoveComponent<UnitCoord>(entity);
            }
        }
    }

    public struct UnitCoordsSingleton : IComponentData
    {
        public NativeHashMap<uint2, Entity> Lookup;
        public NativeReference<JobHandle> Dependency;
    }

}
