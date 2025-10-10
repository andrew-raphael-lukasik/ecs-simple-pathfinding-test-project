using Unity.Entities;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct InAttackRangeSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();

            state.RequireForUpdate<UnitCoord>();
            state.RequireForUpdate<AttackRange>();
            state.RequireForUpdate<InAttackRange>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            foreach (var (coord, attackRange, inAttackRange, entity) in SystemAPI
                .Query<UnitCoord, AttackRange, InAttackRange>()
                .WithChangeFilter<UnitCoord, AttackRange>().WithEntityAccess())
            {
                state.Dependency = new GameNavigation.AttackRangeJob(
                    start: coord,
                    range: attackRange,
                    floor: mapData.FloorArray,
                    mapSize: mapSettings.Size,
                    reachable: inAttackRange.Coords
                ).Schedule(state.Dependency);
            }
        }
    }
}
