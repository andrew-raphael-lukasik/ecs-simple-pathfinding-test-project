using UnityEngine;
using Unity.Entities;

using Server.Gameplay;
using ServerAndClient.Gameplay;

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
                AddComponent<IsUnitUninitialized>(entity);

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
