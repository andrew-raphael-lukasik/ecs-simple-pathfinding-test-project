using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ServerAndClient.Gameplay
{
    [InternalBufferCapacity(0)]// keep it external
    public struct GameObjectCleanup : ICleanupBufferElementData
    {
        public UnityObjectRef<GameObject> Value;

        public static implicit operator GameObject (GameObjectCleanup value) => value.Value;
        public static implicit operator GameObjectCleanup  (GameObject value) => new GameObjectCleanup{Value = value};
    }
}
