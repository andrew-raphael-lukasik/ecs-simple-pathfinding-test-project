using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

using ServerAndClient.Input;
using ServerAndClient.GameState;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct CursorInstantiationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<IsUninitializad>(state.SystemHandle);
            state.RequireForUpdate<IsUninitializad>();
            state.RequireForUpdate<PointerPositionData>();
            state.RequireForUpdate<PrefabSystem.Prefabs>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
            prefabs.Dependency.Complete();

            if( prefabs.Registry.Count==0 ) return;

            {
                FixedString32Bytes key = "cursor-edit";
                if (prefabs.Registry.TryGetValue(key, out Entity prefab))
                {
                    Entity e = state.EntityManager.Instantiate(prefab);

                    #if UNITY_EDITOR
                    state.EntityManager.SetName(e, key);
                    #endif
                }
                else Debug.LogWarning($"prefab key not found: '{key}'");
            }
            {
                FixedString32Bytes key = "cursor-play";
                if (prefabs.Registry.TryGetValue(key, out Entity prefab))
                {
                    Entity e = state.EntityManager.Instantiate(prefab);

                    #if UNITY_EDITOR
                    state.EntityManager.SetName(e, key);
                    #endif
                }
                else Debug.LogWarning($"prefab key not found: '{key}'");
            }

            // we're done here
            state.EntityManager.RemoveComponent<IsUninitializad>(state.SystemHandle);
            Debug.Log($"{state.DebugName}: cursors spawned");
        }

        public struct IsUninitializad : IComponentData {}
    }

    // [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    // [UpdateInGroup(typeof(InitializationSystemGroup))]
    // [Unity.Burst.BurstCompile]
    // partial struct CursorPlayToEditModeSystem : ISystem
    // {
    //     [Unity.Burst.BurstCompile]
    //     void ISystem.OnCreate(ref SystemState state)
    //     {
    //         state.RequireForUpdate<IS_EDIT_GAME_STATE>();
    //         state.RequireForUpdate<IsPlayModeActive>();
    //     }

    //     [Unity.Burst.BurstCompile]
    //     void ISystem.OnUpdate(ref SystemState state)
    //     {
    //         state.EntityManager.RemoveComponent<IsPlayModeActive>(state.SystemHandle);
    //         state.EntityManager.AddComponent<IsEditModeActive>(state.SystemHandle);

    //         var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
    //         prefabs.Dependency.Complete();

    //         FixedString32Bytes editCursorKey = "cursor-edit";
    //         if (prefabs.Registry.TryGetValue(editCursorKey, out Entity editCursorPrefab))
    //         {
    //             Entity e = state.EntityManager.Instantiate(editCursorPrefab);
    //         }
    //         else Debug.LogWarning($"prefab key not found: '{editCursorKey}'");
    //     }

    //     public struct IsEditModeActive : IComponentData {}
    //     public struct IsPlayModeActive : IComponentData {}
    // }
}
