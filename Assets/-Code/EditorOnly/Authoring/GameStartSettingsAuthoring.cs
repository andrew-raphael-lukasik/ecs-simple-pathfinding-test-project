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
        [SerializeField] int _numPlayerUnits = 3;
        [SerializeField] int _numEnemyUnits = 4;
        [SerializeField][Min(1)] uint _seed = 1;

        class Oven : Baker<GameStartSettingsAuthoring>
        {
            public override void Bake(GameStartSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new GameStartSettings{
                    MapSize = authoring._mapSize,
                    MapOffset = authoring._mapOffset,
                    NumPlayerUnits = authoring._numPlayerUnits,
                    NumEnemyUnits = authoring._numEnemyUnits,
                    Seed = authoring._seed,
                });
            }
        }
    }
}
