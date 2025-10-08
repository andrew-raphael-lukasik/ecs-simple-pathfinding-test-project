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
                }
            }

            if (ecb.ShouldPlayback) ecb.Playback(state.EntityManager);
        }

    }
}
