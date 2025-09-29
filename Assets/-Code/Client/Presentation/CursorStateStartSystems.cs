using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

using ServerAndClient;
using ServerAndClient.Input;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct CursorEditStateStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query< RefRO<IsCursor> >().WithAbsent<IsEditModeCursor>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            SystemAPI.GetSingleton<PrefabInstantiationSystem.RequestBufferMeta>().Dependency.Complete();
            var requests = SystemAPI.GetSingletonBuffer<PrefabInstantiationSystem.Request>();
            requests.Add(new PrefabInstantiationSystem.Request{
                Key = "cursor-edit",
                SecondsUntileExpired = 3f,
            });
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct CursorPlayStateStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query< RefRO<IsCursor> >().WithAbsent<IsPlayModeCursor>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            SystemAPI.GetSingleton<PrefabInstantiationSystem.RequestBufferMeta>().Dependency.Complete();
            var requests = SystemAPI.GetSingletonBuffer<PrefabInstantiationSystem.Request>();
            requests.Add(new PrefabInstantiationSystem.Request{
                Key = "cursor-play",
                SecondsUntileExpired = 3f,
            });
        }
    }
}
