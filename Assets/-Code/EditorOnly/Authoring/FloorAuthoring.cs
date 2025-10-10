using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Floor Authoring")]
    public class FloorAuthoring : MonoBehaviour
    {
        class Oven : Baker<FloorAuthoring>
        {
            public override void Bake(FloorAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);

                AddComponent<IsFloor>(entity);
                AddComponent<IsFloorUninitialized>(entity);
            }
        }
    }
}
