using Unity.Entities;
using UnityEngine;

using Client.Presentation;

namespace Client.Authoring
{
    class CursorAuthoring : MonoBehaviour
    {
        [SerializeField] EType _type = EType.EditAndPlay;
        class Oven : Baker<CursorAuthoring>
        {
            public override void Bake(CursorAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);
                switch (authoring._type)
                {
                    case EType.EditAndPlay:
                        AddComponent<IsEditModeCursor>(entity);
                        AddComponent<IsPlayModeCursor>(entity);
                    break;
                    case EType.Edit: AddComponent<IsEditModeCursor>(entity); break;
                    case EType.Play: AddComponent<IsPlayModeCursor>(entity); break;
                    default: throw new System.NotImplementedException($"{authoring._type}");
                }
            }
        }

        enum EType : byte {EditAndPlay, Edit, Play}
    }
}
