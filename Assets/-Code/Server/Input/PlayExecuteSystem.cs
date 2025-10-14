using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using ServerAndClient.Navigation;
using Server.Gameplay;
using ServerAndClient.Presentation;

namespace Server.Input
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]// early simulation phase is best for input execution
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PlayExecuteSystem : ISystem
    {
        public static FixedString64Bytes DebugName {get;} = nameof(PlayExecuteSystem);

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
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();

            // EXECUTE action (right mouse button click)
            if (playerInput.ExecuteStart==1 && playerInput.IsPointerOverUI==0)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                Entity selectedUnit = SystemAPI.GetSingleton<SelectedUnitSingleton>();

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
                                Entity targettingEnemy = SystemAPI.GetComponent<TargettingEnemy>(selectedUnit);
                                if (targettingEnemy==dstEntity)
                                {
                                    var targettingEnemyRW = SystemAPI.GetComponentRW<TargettingEnemy>(selectedUnit);
                                    targettingEnemyRW.ValueRW = Entity.Null;

                                    var damageBuf = SystemAPI.GetBuffer<Damage>(dstEntity);
                                    damageBuf.Add(new Damage{
                                        Amount = 60,
                                        TypeMask = EDamageType.Ranged | EDamageType.Kinetic,
                                        Instigator = selectedUnit,
                                    });

                                    var selectedAnimControlsRef = SystemAPI.GetComponentRW<UnitAnimationControls>(selectedUnit);
                                    selectedAnimControlsRef.ValueRW.EventPistolAttack = 1;
                                    selectedAnimControlsRef.ValueRW.IsAiming = 0;

                                    UnityEngine.Debug.Log($"{DebugName}: ({selectedUnit.Index}:{selectedUnit.Version}) attacks ({dstEntity.Index}:{dstEntity.Version})");
                                    return;// <- pointer click action ends here
                                }
                                else
                                {
                                    var targettingEnemyRW = SystemAPI.GetComponentRW<TargettingEnemy>(selectedUnit);
                                    targettingEnemyRW.ValueRW = dstEntity;

                                    var selectedLtwRef = SystemAPI.GetComponentRW<LocalToWorld>(selectedUnit);
                                    var dstLtwRef = SystemAPI.GetComponentRO<LocalToWorld>(dstEntity);
                                    selectedLtwRef.ValueRW.Value = float4x4.TRS(
                                        selectedLtwRef.ValueRO.Position,
                                        quaternion.LookRotationSafe(
                                            -math.normalizesafe(dstLtwRef.ValueRO.Position - selectedLtwRef.ValueRO.Position),
                                            new float3(0, 1, 0)
                                        ),
                                        new float3(1, 1, 1)
                                    );

                                    SystemAPI.GetComponentRW<UnitAnimationControls>(selectedUnit).ValueRW.IsAiming = 1;

                                    UnityEngine.Debug.Log($"{DebugName}: ({selectedUnit.Index}:{selectedUnit.Version}) selects ({dstEntity.Index}:{dstEntity.Version}) as target");
                                    return;// <- pointer click action ends here
                                }
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
                                    return;// <- pointer click action ends here
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
                            return;// <- pointer click action ends here
                        }
                    }
                }
            }
        }
    }
}
