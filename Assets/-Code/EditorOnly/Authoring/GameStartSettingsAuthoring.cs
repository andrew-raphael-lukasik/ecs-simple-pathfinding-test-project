using Unity.Entities;
using UnityEngine;

using ServerAndClient.Gameplay;
using Server.Gameplay;

namespace EditorOnly.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Game Start Settings Authoring")]
    public class GameStartSettingsAuthoring : MonoBehaviour
    {
        [SerializeField] Vector2Int _mapSize = new Vector2Int(16, 16);
        [SerializeField] Vector3 _mapOffset = new Vector3(0, 0, 0);
        [SerializeField][Min(1)] int _numPlayerUnits = 3;
        [SerializeField][Min(1)] int _numEnemyUnits = 4;
        [SerializeField][Min(1)] uint _seed = 1;

        class Oven : Baker<GameStartSettingsAuthoring>
        {
            public override void Bake(GameStartSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new StartTheGameData{
                    MapSettings = new MapSettingsData{
                        Size = authoring._mapSize,
                        Offset = authoring._mapOffset,
                        NumPlayerUnits = (uint) authoring._numPlayerUnits,
                        NumEnemyUnits = (uint) authoring._numEnemyUnits,
                        Seed = authoring._seed,
                    }
                });
            }
        }
    }
}
