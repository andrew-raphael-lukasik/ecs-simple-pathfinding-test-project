using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ServerAndClient
{
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct DisposeNativeArrayOnDestroyedSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DisposeNativeArrayOnDestroyed>();
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (buffer, entity) in SystemAPI.Query< DynamicBuffer<DisposeNativeArrayOnDestroyed> >().WithAbsent<Simulate>().WithEntityAccess())
            {
                #if DEBUG
                UnityEngine.Debug.Log($"({entity.Index}:{entity.Version}) destroyed, cleaning up {buffer.Length} allocations...");
                #endif

                foreach (var item in buffer)
                {
                    if (item.Allocation.IsCreated) item.Allocation.Dispose();
                }

                ecb.RemoveComponent<DisposeNativeArrayOnDestroyed>(entity);
            }
        }
    }

    public struct DisposeNativeArrayOnDestroyed : ICleanupBufferElementData
    {
        public NativeArray<byte> Allocation;

        public static DisposeNativeArrayOnDestroyed Factory<T> (NativeArray<T> allocation) where T : unmanaged
        {
            return new DisposeNativeArrayOnDestroyed{
                Allocation = allocation.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()),
            };
        }
    }
}
