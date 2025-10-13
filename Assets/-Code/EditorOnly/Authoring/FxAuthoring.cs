using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

using ServerAndClient.Presentation;
using ServerAndClient.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Fx Emiter Authoring")]
    public class FxAuthoring : MonoBehaviour
    {

        [SerializeField][Min(0.01f)] float _lifeTime = 1;

        class Oven : Baker<FxAuthoring>
        {
            public override void Bake(FxAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);

                AddComponent<IsFxEmiter>(entity);
                AddComponent(entity, new LifeTime{
                    Value = authoring._lifeTime,
                });
            }
        }
    }
}
