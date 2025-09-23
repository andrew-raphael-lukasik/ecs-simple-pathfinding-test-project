using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ServerAndClient
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [Unity.Burst.BurstCompile]
    public partial struct EndPresentationECBSystem: ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<Singleton>(state.SystemHandle);
            SystemAPI.SetSingleton(new Singleton{
                commands = new (Allocator.Persistent),
                dependencies = new (Allocator.Persistent),
            });
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<Singleton>();
            
            JobHandle.CompleteAll(singleton.dependencies.AsArray());
            singleton.dependencies.Dispose();

            foreach (var next in singleton.commands)
            {
                next.Playback(state.EntityManager);
            }
            singleton.commands.Dispose();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<Singleton>();

            JobHandle.CompleteAll(singleton.dependencies.AsArray());
            singleton.dependencies.Clear();

            foreach (var next in singleton.commands)
            {
                next.Playback(state.EntityManager);
            }
            singleton.commands.Clear();
        }

        public struct Singleton : IComponentData
        {
            public NativeList<EntityCommandBuffer> commands;
            public NativeList<JobHandle> dependencies;
            
            public void Append(EntityCommandBuffer cmd, JobHandle jobHandle)
            {
                commands.Add(cmd);
                dependencies.Add(jobHandle);
            }
        }
    }
}
