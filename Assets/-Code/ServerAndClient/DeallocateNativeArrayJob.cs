using Unity.Collections;
using Unity.Jobs;

namespace ServerAndClient
{
    public struct DeallocateNativeArrayJob<T> : IJob
        where T : unmanaged
    {
        [DeallocateOnJobCompletion] public NativeArray<T> Array;
        public DeallocateNativeArrayJob (NativeArray<T> array)
        {
            Array = array;
        }
        void IJob.Execute() {}
    }
}
