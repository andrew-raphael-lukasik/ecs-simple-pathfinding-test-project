using UnityEngine;
using Unity.Entities;

using Client.Presentation;
using ServerAndClient.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Cursor Authoring")]
    public class CursorAuthoring : MonoBehaviour
    {

        [SerializeField] EGameState _type = EGameState.UNDEFINED;

        class Oven : Baker<CursorAuthoring>
        {
            public override void Bake(CursorAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.WorldSpace);

                switch (authoring._type)
                {
                    case EGameState.EDIT:
                        AddComponent<IsEditModeCursor>(entity);
                        AddComponent<IsEditStateOnly>(entity);
                        break;
                    
                    case EGameState.PLAY:
                        AddComponent<IsPlayModeCursor>(entity);
                        AddComponent<IsPlayStateOnly>(entity);
                        break;

                    default:
                        Debug.LogError($"value not implemented: {authoring._type}", authoring);
                        break;
                }
            }
        }
    }
}
