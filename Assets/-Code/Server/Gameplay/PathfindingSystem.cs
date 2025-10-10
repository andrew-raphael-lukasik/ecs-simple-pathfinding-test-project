using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PathfindingSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<PathfindingQuery>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            if (SystemAPI.TryGetSingleton<MapSettingsSingleton>(out var mapSettings))
            if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
            foreach (var (request, entity) in SystemAPI.Query<PathfindingQuery>().WithEntityAccess())
            {
                ushort moveRange = SystemAPI.GetComponent<MoveRange>(entity);

                NativeList<uint2> results = new (Allocator.Persistent);
                var job = new GameNavigation.MovePathJob(
                    start: GameGrid.Clamp(request.Src, mapSettings.Size),
                    destination: GameGrid.Clamp(request.Dst, mapSettings.Size),
                    range: moveRange,
                    floor: mapData.FloorArray,
                    mapSize: mapSettings.Size,
                    results: results,
                    hMultiplier: 1
                );
                job.Schedule().Complete();
                job.Dispose();

                if (SystemAPI.HasComponent<PathfindingQueryResult>(entity))
                {
                    var prevPathResults = SystemAPI.GetComponent<PathfindingQueryResult>(entity);
                    if (prevPathResults.Path.IsCreated) prevPathResults.Path.Dispose();
                    ecb.RemoveComponent<PathfindingQueryResult>(entity);
                }

                if (job.results.Length!=0)
                {
                    Debug.Log($"Pathfinding succeeded! Path length:{job.results.Length}");

                    ecb.AddComponent(entity, new PathfindingQueryResult{
                        Success = 1,
                        Path = job.results.AsArray(),
                    });

                    DynamicBuffer<DisposeNativeArrayOnDestroyed> cleanupBuffer;
                    {
                        if (SystemAPI.HasBuffer<DisposeNativeArrayOnDestroyed>(entity))
                            cleanupBuffer = SystemAPI.GetBuffer<DisposeNativeArrayOnDestroyed>(entity);
                        else
                            cleanupBuffer = ecb.AddBuffer<DisposeNativeArrayOnDestroyed>(entity);

                        cleanupBuffer.Add(DisposeNativeArrayOnDestroyed.Factory(job.results));
                    }
                }
                else
                {
                    Debug.Log("Pathfinding found no sulution");
                    ecb.AddComponent(entity, new PathfindingQueryResult{
                        Success = 0,
                        Path = default,
                    });
                    job.results.Dispose();
                }

                ecb.RemoveComponent<PathfindingQuery>(entity);
            }
        }
    }
}
