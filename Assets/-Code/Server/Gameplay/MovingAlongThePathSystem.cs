using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct MovingAlongThePathSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<PathfindingQueryResult>();
            state.RequireForUpdate<MovingAlongThePath>();
            state.RequireForUpdate<MapSettingsSingleton>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (path, moving, ltw, entity) in SystemAPI.Query< PathfindingQueryResult, RefRW<MovingAlongThePath>, RefRW<LocalToWorld> >().WithEntityAccess())
            {
                UnityEngine.Assertions.Assert.IsTrue(path.Success==1);

                if (moving.ValueRO.Index<path.Path.Length)
                {
                    uint2 nextCoord = path.Path[moving.ValueRO.Index];
                    int nextIndex = GameGrid.ToIndex(nextCoord, mapSettings.Size);
                    float3 nextPos = mapData.PositionArray[nextIndex];

                    if (moving.ValueRW.TimeUntilNextCoord<1)
                    {
                        moving.ValueRW.TimeUntilNextCoord = math.saturate(moving.ValueRW.TimeUntilNextCoord + SystemAPI.Time.DeltaTime * 3f);

                        float4 Zprev = ltw.ValueRW.Value.c2;
                        float3 Z = math.normalizesafe(
                            math.lerp(
                                new float3(Zprev.x, Zprev.y, Zprev.z),
                                math.normalizesafe(nextPos - ltw.ValueRO.Position),
                                SystemAPI.Time.DeltaTime * 7f
                            )
                        );
                        float3 Y = new float3(0, 1, 0);
                        float3 X = math.cross(Z, Y);

                        ltw.ValueRW.Value.c0 = new float4(X, 0);
                        ltw.ValueRW.Value.c1 = new float4(Y, 0);
                        ltw.ValueRW.Value.c2 = new float4(Z, 0);
                        ltw.ValueRW.Value.c3 = math.lerp(
                            ltw.ValueRO.Value.c3,
                            new float4(nextPos, 1),
                            moving.ValueRO.TimeUntilNextCoord
                        );
                    }
                    else
                    {
                        ltw.ValueRW.Value.c3 = new float4(nextPos, 1);
                        moving.ValueRW.TimeUntilNextCoord = 0;
                        moving.ValueRW.Index++;

                        ecb.SetComponentEnabled<IsUnitCoordValid>(entity, false);
                    }
                }
                else
                {
                    ecb.RemoveComponent<MovingAlongThePath>(entity);
                    ecb.RemoveComponent<PathfindingQueryResult>(entity);
                    path.Path.Dispose();
                }
            }

            if (ecb.ShouldPlayback) ecb.Playback(state.EntityManager);
        }

    }
}
