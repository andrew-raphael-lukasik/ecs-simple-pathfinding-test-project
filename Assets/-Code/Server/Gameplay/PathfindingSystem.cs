using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
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
            state.RequireForUpdate<CalculatePathRequest>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            if (SystemAPI.TryGetSingleton<MapSettingsSingleton>(out var mapSettings))
            if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
            foreach (var (request, entity) in SystemAPI.Query<CalculatePathRequest>().WithEntityAccess())
            {
                NativeList<uint2> results = new (Allocator.Persistent);
                var job = new GameNavigation.AStarJob(
                    start: GameGrid.Clamp(request.Src, mapSettings.Size),
                    destination: GameGrid.Clamp(request.Dst, mapSettings.Size),
                    moveCost: mapData.FloorArray,
                    mapSize: mapSettings.Size,
                    results: results,
                    hMultiplier: 1,
                    stepBudget: mapSettings.Size.x * mapSettings.Size.y,
                    resultsStartAtIndexZero: true
                );
                job.Schedule().Complete();
                job.Dispose();

                if (SystemAPI.HasComponent<CalculatePathResult>(entity))
                {
                    var prevPathResults = SystemAPI.GetComponent<CalculatePathResult>(entity);
                    if (prevPathResults.Path.IsCreated) prevPathResults.Path.Dispose();
                    ecb.RemoveComponent<CalculatePathResult>(entity);
                }

                if (job.Results.Length!=0)
                {
                    Debug.Log($"Pathfinding succeeded! Path length:{job.Results.Length}");
                    ecb.AddComponent(entity, new CalculatePathResult{
                        Success = 1,
                        Path = job.Results.AsArray(),
                    });

                    DynamicBuffer<DisposeNativeArrayOnDestroyed> cleanupBuffer;
                    {
                        if (state.EntityManager.HasBuffer<DisposeNativeArrayOnDestroyed>(entity))
                            cleanupBuffer = state.EntityManager.GetBuffer<DisposeNativeArrayOnDestroyed>(entity);
                        else
                            cleanupBuffer = ecb.AddBuffer<DisposeNativeArrayOnDestroyed>(entity);

                        cleanupBuffer.Add(
                            DisposeNativeArrayOnDestroyed.Factory(job.Results.AsArray())
                        );
                    }
                }
                else
                {
                    Debug.Log("Pathfinding found no sulution");
                    ecb.AddComponent(entity, new CalculatePathResult{
                        Success = 0,
                        Path = default,
                    });
                    job.Results.Dispose();
                }

                ecb.RemoveComponent<CalculatePathRequest>(entity);
            }
        }
    }
}
