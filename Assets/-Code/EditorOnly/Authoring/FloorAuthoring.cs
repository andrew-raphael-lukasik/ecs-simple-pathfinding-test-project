using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient.Gameplay;

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

                // note: FloorCoord wont serialize - my guess is because it's a cleanup component

                // AddComponent(entity, new FloorCoord{
                //     Value = new uint2(uint.MaxValue, uint.MaxValue)
                // });
                // AddComponent<IsFloorCoordValid>(entity);
                // SetComponentEnabled<IsFloorCoordValid>(entity, false);
            }
        }
    }
}
