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
            if (playerInput.AttackStart==1)
            {
                var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();

                var plane = new Plane(Vector3.up, Vector3.zero);
                var ray = playerInput.PointerRay;
                if (plane.Raycast(ray, out float dist))
                {
                    float3 hit = ray.origin + ray.direction * dist;
                    float3 localPos = hit - mapSettings.Origin;

                    uint2 coord = (uint2)(new float2(localPos.x, localPos.z) / new float2(MapSettingsSingleton.CellSize, MapSettingsSingleton.CellSize));
                    coord = math.min(coord, mapSettings.Size-1);// clamp to map size

                    var floorCoords = SystemAPI.GetSingleton<FloorCoordsSingleton>();
                    floorCoords.Dependency.AsReadOnly().Value.Complete();
                    if (floorCoords.Lookup.TryGetValue(coord, out Entity entity))
                    {
                        SystemAPI.SetSingleton(new SelectedFloorSingleton{
                            Selected = entity
                        });

                        if(entity!=Entity.Null)
                            Debug.Log($"Floor {entity} selected");
                        else
                            Debug.Log("Floor unselected");
                    }
                }
            }
        }
    }
}
