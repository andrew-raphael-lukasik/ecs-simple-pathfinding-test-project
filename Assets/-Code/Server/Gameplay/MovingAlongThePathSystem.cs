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
using ServerAndClient.Presentation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
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

                        SystemAPI.GetComponentRW<UnitAnimationControls>(entity).ValueRW.Speed = 3;

                        float3 Z;
                        {
                            float3 vec = nextPos - ltw.ValueRO.Position;
                            float3 Z0 = math.normalizesafe(new float3(ltw.ValueRW.Value.c2.x, 0, ltw.ValueRW.Value.c2.z));
                            float3 Z1 = math.normalizesafe(new float3(vec.x, 0, vec.z));
                            quaternion q0 = quaternion.LookRotationSafe(Z0, new float3(0, 1, 0));
                            quaternion q1 = quaternion.LookRotationSafe(Z1, new float3(0, 1, 0));
                            float step = 7f * SystemAPI.Time.DeltaTime;
                            float t = math.min(1, step / math.angle(q0, q1)); 
                            quaternion q = math.slerp(q0, q1, t);
                            Z = math.mul(q, new float3(0, 0, 1));
                        }
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
                    SystemAPI.GetComponentRW<UnitAnimationControls>(entity).ValueRW.Speed = 0;

                    ecb.RemoveComponent<MovingAlongThePath>(entity);
                    ecb.RemoveComponent<PathfindingQueryResult>(entity);
                    path.Path.Dispose();
                }
            }

            if (ecb.ShouldPlayback) ecb.Playback(state.EntityManager);
        }

    }
}
