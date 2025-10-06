using UnityEngine;
using Unity.Entities;

using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Enemy Unit Authoring")]
    public class EnemyUnitAuthoring : MonoBehaviour
    {
        class Oven : Baker<EnemyUnitAuthoring>
        {
            public override void Bake(EnemyUnitAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent<IsEnemyUnit>(entity);
            }
        }
    }
}
