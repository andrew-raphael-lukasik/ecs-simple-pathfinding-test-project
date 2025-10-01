using UnityEngine;
using Unity.Entities;

using ServerAndClient;
using ServerAndClient.Gameplay;

namespace Client.Presentation
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct ShaderEditStateStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.EDIT_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            Shader.DisableKeyword("_GAME_STATE_PLAY");
            Shader.EnableKeyword("_GAME_STATE_EDIT");
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(GamePresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct ShaderPlayStateStartSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState.PLAY_STARTED_EVENT>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            Shader.DisableKeyword("_GAME_STATE_EDIT");
            Shader.EnableKeyword("_GAME_STATE_PLAY");
        }
    }
}
