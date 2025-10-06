using UnityEngine;
using Unity.Entities;

using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Player Unit Authoring")]
    public class PlayerUnitAuthoring : MonoBehaviour
    {
        class Oven : Baker<PlayerUnitAuthoring>
        {
            public override void Bake(PlayerUnitAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent<IsPlayerUnit>(entity);
            }
        }
    }
}
