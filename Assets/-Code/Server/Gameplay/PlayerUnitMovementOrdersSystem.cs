using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlayerUnitMovementOrdersSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(PlayerUnitMovementOrdersSystem);

        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<SelectedUnitSingleton>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();

            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            if (selectedUnit!=Entity.Null && SystemAPI.Exists(selectedUnit))
            if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 dstCoord))
            {
                var unitsRW = SystemAPI.GetSingletonRW<UnitsSingleton>();
                int dstIndex = GameGrid.ToIndex(dstCoord, mapSettings.Size);
                Entity dstEntity = unitsRW.ValueRO.Lookup[dstIndex];

                var inAttackRangeRW = SystemAPI.GetComponentRW<InAttackRange>(selectedUnit);
                if (inAttackRangeRW.ValueRO.Coords.Contains(dstCoord))
                {
                    if (dstEntity!=Entity.Null && SystemAPI.Exists(dstEntity))
                    {
                        bool clickedOverEnemy = SystemAPI.HasComponent<IsPlayerUnit>(selectedUnit)
                            ? SystemAPI.HasComponent<IsEnemyUnit>(dstEntity)
                            : SystemAPI.HasComponent<IsPlayerUnit>(dstEntity);

                        if (clickedOverEnemy)
                        {
                            state.EntityManager.DestroyEntity(dstEntity);
                            UnityEngine.Debug.Log($"{DebugName}: ({selectedUnit.Index}:{selectedUnit.Version}) killed ({dstEntity.Index}:{dstEntity.Version})");
                            return;
                        }
                    }
                }

                var inMoveRangeRW = SystemAPI.GetComponentRW<InMoveRange>(selectedUnit);
                bool clickedOnMoveDestination = inMoveRangeRW.ValueRO.Coords.Contains(dstCoord);
                if (clickedOnMoveDestination)
                {
                    if (SystemAPI.HasComponent<PathfindingQueryResult>(selectedUnit))
                    {
                        var pathResultRW = SystemAPI.GetComponentRW<PathfindingQueryResult>(selectedUnit);
                        var pathResult = pathResultRW.ValueRO;
                        if (pathResult.Success==1)
                        {
                            uint2 pathEnd = pathResult.Path[pathResult.Path.Length-1];
                            bool clickedOnPathDestination = math.all(dstCoord==pathEnd);
                            if (clickedOnPathDestination)
                            {
                                state.EntityManager.AddComponent<MovingAlongThePath>(selectedUnit);
                                UnityEngine.Debug.Log($"{DebugName}: ({selectedUnit.Index}:{selectedUnit.Version}) moving along the path");
                                return;
                            }
                        }
                    }

                    if(!SystemAPI.HasComponent<MovingAlongThePath>(selectedUnit))
                    if (dstEntity==Entity.Null)
                    {
                        uint2 srcCoord = SystemAPI.GetComponent<UnitCoord>(selectedUnit);
                        state.EntityManager.AddComponentData(selectedUnit, new PathfindingQuery{
                            Src = srcCoord,
                            Dst = dstCoord,
                        });
                        UnityEngine.Debug.Log($"{DebugName}: ({selectedUnit.Index}:{selectedUnit.Version}) pathfinding");
                        return;
                    }
                }
            }
        }
    }
}
