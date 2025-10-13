using Unity.Entities;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct InMoveRangeSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();

            state.RequireForUpdate<UnitCoord>();
            state.RequireForUpdate<MoveRange>();
            state.RequireForUpdate<InMoveRange>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            foreach (var (coord, attackRange, inMoveRange, entity) in SystemAPI
                .Query<UnitCoord, MoveRange, InMoveRange>()
                .WithChangeFilter<UnitCoord, MoveRange>().WithEntityAccess())
            {
                state.Dependency = new GameNavigation.MoveRangeJob(
                    start: coord,
                    range: attackRange,
                    floor: mapData.FloorArray,
                    mapSize: mapSettings.Size,
                    reachable: inMoveRange.Coords
                ).Schedule(state.Dependency);
            }
        }
    }
}
