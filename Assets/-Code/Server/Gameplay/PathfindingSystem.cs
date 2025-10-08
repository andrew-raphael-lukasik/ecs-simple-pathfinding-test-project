using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Simulation;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PathfindingSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<CalculatePathRequest>();

            Entity request = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(request, new CalculatePathRequest{
                Src = new uint2(0, 0),
                Dst = new uint2(uint.MaxValue, uint.MaxValue),
            });

            var lineMat = Resources.Load<Material>("game-selection-lines");
            Segments.Core.Create(out _segments, lineMat);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            if (SystemAPI.TryGetSingleton<MapSettingsSingleton>(out var mapSettings))
            if (SystemAPI.TryGetSingleton<GeneratedMapData>(out var mapData))
            foreach (var (request, entity) in SystemAPI.Query<CalculatePathRequest>().WithAbsent<CalculatePathResult>().WithEntityAccess())
            {
                NativeList<uint2> results = new (Allocator.Persistent);
                var job = new GameNavigation.AStarJob(
                    start: GameGrid.Clamp(request.Src, mapSettings.Size),
                    destination: GameGrid.Clamp(request.Dst, mapSettings.Size),
                    moveCost: mapData.FloorArray,
                    mapSize: mapSettings.Size.x,
                    results: results,
                    hMultiplier: 1,
                    stepBudget: mapSettings.Size.x * mapSettings.Size.y,
                    resultsStartAtIndexZero: true
                );
                job.Schedule().Complete();
                job.Dispose();

                if (job.Results.Length!=0)
                {
                    Debug.Log($"Pathfinding succeeded! Path length:{job.Results.Length}");
                    var path = job.Results;
                    var positions = mapData.PositionArray;

                    ecb.AddComponent(entity, new CalculatePathResult{
                        Success = 1,
                        Path = path.AsArray(),
                    });

                    var buffer = Segments.Core.GetSegment(_segments, state.EntityManager).Buffer;
                    {
                        int length = path.Length;
                        buffer.Length = length;
                        uint2 coord = path[0];
                        for (int i = 1; i < length; i++)
                        {
                            int indexPrev = GameGrid.ToIndex((uint2) coord, mapSettings.Size);
                            coord = path[i];
                            int index = GameGrid.ToIndex((uint2) coord, mapSettings.Size);
                            buffer[i] = new float3x2(
                                positions[indexPrev] + new float3(0, 1, 0),
                                positions[index] + new float3(0, 1, 0)
                            );
                        }
                    }

                    Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
                }
                else
                {
                    Debug.Log("Pathfinding found no sulution");
                    job.Results.Dispose();
                    ecb.AddComponent(entity, new CalculatePathResult{
                        Success = 0,
                        Path = default,
                    });
                }
            }
        }
    }

    public struct CalculatePathRequest : IComponentData
    {
        public uint2 Src, Dst;
    }

    public struct CalculatePathResult : IComponentData
    {
        public byte Success;
        public NativeArray<uint2> Path;
    }

}
