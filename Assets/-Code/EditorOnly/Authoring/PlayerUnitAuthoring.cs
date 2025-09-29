using UnityEngine;
using Unity.Entities;

using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Player Unit Authoring")]
    public class PlayerUnitAuthoring : MonoBehaviour
    {
        [SerializeField][Min(1)] int _moveRange = 3;
        [SerializeField][Min(1)] int _attackRange = 3;

        class Oven : Baker<PlayerUnitAuthoring>
        {
            public override void Bake(PlayerUnitAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);
                
                AddComponent<IsPlayerUnit>(entity);

                AddComponent(entity, new UnitMoveData{
                    MoveRange = (ushort) authoring._moveRange
                });
                AddComponent(entity, new UnitAttackData{
                    AttackRange = (ushort) authoring._attackRange,
                });
            }
        }
    }
}
