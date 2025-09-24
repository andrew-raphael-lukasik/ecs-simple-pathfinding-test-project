using UnityEngine;
using Unity.Entities;

using Client.Presentation;
using ServerAndClient.Gameplay;

namespace EditorOnly.Authoring
{
    [AddComponentMenu("Game/Authoring/Cursor")]
    public class CursorAuthoring : MonoBehaviour
    {
        [SerializeField] EGameState _type = EGameState.UNDEFINED;
        class Oven : Baker<CursorAuthoring>
        {
            public override void Bake(CursorAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);

                AddComponent<IsCursor>(entity);

                switch (authoring._type)
                {
                    case EGameState.EDIT:
                        AddComponent<IsEditModeCursor>(entity);
                        break;
                    
                    case EGameState.PLAY:
                        AddComponent<IsPlayModeCursor>(entity);
                        break;

                    default:
                        Debug.LogError($"value not implemented: {authoring._type}", authoring);
                        break;
                }
            }
        }
    }
}
