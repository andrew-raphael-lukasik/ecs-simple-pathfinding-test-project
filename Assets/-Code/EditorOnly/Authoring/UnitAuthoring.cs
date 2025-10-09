using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Unit Authoring")]
    public class UnitAuthoring : MonoBehaviour
    {
        [SerializeField][Min(1)] int _moveRange = 3;
        [SerializeField][Min(1)] int _attackRange = 3;

        class Oven : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace | TransformUsageFlags.Dynamic);
                
                AddComponent<IsUnit>(entity);

                // note: UnitCoord wont serialize - my guess is because it's a cleanup component

                // AddComponent(entity, new UnitCoord{
                //     Value = new uint2(uint.MaxValue, uint.MaxValue)
                // });
                // AddComponent<IsUnitCoordValid>(entity);
                // SetComponentEnabled<IsUnitCoordValid>(entity, false);

                AddComponent(entity, new MoveRange{
                    Value = (ushort) authoring._moveRange
                });
                AddComponent(entity, new AttackRange{
                    Value = (ushort) authoring._attackRange,
                });
            }
        }
    }
}
