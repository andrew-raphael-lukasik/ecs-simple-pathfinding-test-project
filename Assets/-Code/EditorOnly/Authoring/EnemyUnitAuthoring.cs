using UnityEngine;
using Unity.Entities;

using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Enemy Unit Authoring")]
    public class EnemyUnitAuthoring : MonoBehaviour
    {
        [SerializeField][Min(1)] int _moveRange = 3;
        [SerializeField][Min(1)] int _attackRange = 3;

        class Oven : Baker<EnemyUnitAuthoring>
        {
            public override void Bake(EnemyUnitAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);
                
                AddComponent<IsEnemyUnit>(entity);

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
