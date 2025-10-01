using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Input;
using Server.Simulation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct SelectedFloorSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputSingleton>();
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<FloorCoordsSingleton>();

            state.EntityManager.AddComponent<SelectedFloorSingleton>(state.SystemHandle);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerInput = SystemAPI.GetSingleton<PlayerInputSingleton>();
            if (playerInput.SelectStart==1)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
                if (GameGrid.Raycast(ray: playerInput.PointerRay, mapOrigin: mapSettings.Origin, mapSize: mapSettings.Size, out uint2 coord))
                {
                    var floorCoords = SystemAPI.GetSingleton<FloorCoordsSingleton>();
                    floorCoords.Dependency.AsReadOnly().Value.Complete();
                    if (floorCoords.Lookup.TryGetValue(coord, out Entity entity))
                    {
                        SystemAPI.SetSingleton(new SelectedFloorSingleton{
                            Selected = entity
                        });

                        if (entity!=Entity.Null)
                            Debug.Log($"Floor ({entity.Index}:{entity.Version}) selected");
                        else
                            Debug.Log("Floor unselected");
                    }
                }
            }
        }
    }
}
