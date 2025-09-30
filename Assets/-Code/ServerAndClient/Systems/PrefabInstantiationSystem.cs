using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct PrefabInstantiationSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<Request>(state.SystemHandle);
            state.EntityManager.AddComponent<RequestBufferMeta>(state.SystemHandle);
            SystemAPI.SetComponent(state.SystemHandle, new RequestBufferMeta{
                Dependency = new (Allocator.Persistent)
            });
            
            state.RequireForUpdate<PrefabSystem.Prefabs>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.HasComponent<RequestBufferMeta>(state.SystemHandle))
            {
                var singleton = SystemAPI.GetComponent<RequestBufferMeta>(state.SystemHandle);
                if (singleton.Dependency.IsCreated)
                {
                    singleton.Dependency.AsReadOnly().Value.Complete();
                    singleton.Dependency.Dispose();
                }
            }
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<Request>();
            int numRequests = buffer.Length;
            if (numRequests==0) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var prefabs = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
            state.Dependency = JobHandle.CombineDependencies(
                state.Dependency,
                prefabs.Dependency,
                SystemAPI.GetComponent<RequestBufferMeta>(state.SystemHandle).Dependency.AsReadOnly().Value
            );

            state.Dependency = new InstantiateJob{
                DeltaTime       = SystemAPI.Time.DeltaTime,
                Buffer          = buffer,
                CommandBufferPW = ecb.AsParallelWriter(),
                Prefabs         = prefabs.Lookup,
            }.Schedule(arrayLength: buffer.Length, innerloopBatchCount: 128, state.Dependency);

            state.Dependency = new RemoveFullfiledAndExpiredJob{
                Buffer          = buffer,
            }.Schedule(state.Dependency);

            // var ecbss = SystemAPI.GetSingleton<EndInitializationECBSystem.Singleton>();
            // ecbss.Append(ecb, state.Dependency);

            // prefabs.Dependency.value = state.Dependency;

            state.Dependency.Complete();
            if(ecb.ShouldPlayback) ecb.Playback(state.EntityManager);
        }

        public struct Request : IBufferElementData
        {
            public FixedString64Bytes Key;
            public float SecondsUntileExpired;
        }
        public struct RequestBufferMeta : IComponentData
        {
            public NativeReference<JobHandle> Dependency;
        }

        [Unity.Burst.BurstCompile]
        partial struct InstantiateJob : IJobParallelFor
        {
            public float DeltaTime;
            [NativeDisableParallelForRestriction] public DynamicBuffer<Request> Buffer;
            public EntityCommandBuffer.ParallelWriter CommandBufferPW;
            [ReadOnly] public NativeHashMap<FixedString64Bytes, Entity> Prefabs;
            void IJobParallelFor.Execute(int index)
            {
                var request = Buffer[index];
                if (Prefabs.TryGetValue(request.Key, out Entity prefab))
                {
                    CommandBufferPW.Instantiate(index, prefab);

                    // mark as fullfiled:
                    request.SecondsUntileExpired = float.MinValue;
                }
                else
                {
                    request.SecondsUntileExpired -= DeltaTime;
                }
                Buffer[index] = request;
            }
        }

        [Unity.Burst.BurstCompile]
        partial struct RemoveFullfiledAndExpiredJob : IJob
        {
            public DynamicBuffer<Request> Buffer;
            void IJob.Execute()
            {
                int len = Buffer.Length;
                for (int i = len-1; i!=-1; --i)
                {
                    var request = Buffer[i];
                    if (request.SecondsUntileExpired<0)
                    {
                        if (request.SecondsUntileExpired==float.MinValue)
                        {
                            // instantiated
                        }
                        else
                        {
                            // expired
                            Debug.LogWarning($"prefab instantiation request '{Buffer[i].Key}' expired");
                        }
                        
                        Buffer.RemoveAtSwapBack(i);
                    }
                }
            }
        }

    }
}
