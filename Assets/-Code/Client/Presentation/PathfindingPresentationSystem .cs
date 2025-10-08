using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using ServerAndClient;
using ServerAndClient.Gameplay;
using ServerAndClient.Navigation;

namespace Server.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(GameInitializationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    public partial struct PathfindingPresentationSystem : ISystem
    {
        Entity _segments;

        // [Unity.Burst.BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapSettingsSingleton>();
            state.RequireForUpdate<GeneratedMapData>();
            state.RequireForUpdate<PathfindingQueryResult>();

            var lineMat = Resources.Load<Material>("game-selection-lines");
            Segments.Core.Create(out _segments, lineMat);
        }

        [Unity.Burst.BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var mapSettings = SystemAPI.GetSingleton<MapSettingsSingleton>();
            var mapData = SystemAPI.GetSingleton<GeneratedMapData>();

            var segmentRef = SystemAPI.GetComponentRW<Segments.Segment>(_segments);
            var buffer = segmentRef.ValueRW.Buffer;
            bool preExistingLines = buffer.Length!=0;
            buffer.Length = 0;

            foreach (var pathResults in SystemAPI.Query<PathfindingQueryResult>())
            if (pathResults.Success==1)
            {
                int length = pathResults.Path.Length;
                int iBufferStart = buffer.Length;
                buffer.Length += length;
                uint2 coord = pathResults.Path[0];
                for (int i = 1; i < length; i++)
                {
                    int indexPrev = GameGrid.ToIndex((uint2) coord, mapSettings.Size);
                    coord = pathResults.Path[i];
                    int index = GameGrid.ToIndex((uint2) coord, mapSettings.Size);
                    buffer[iBufferStart+i] = new float3x2(
                        mapData.PositionArray[indexPrev] + new float3(0, .5f, 0),
                        mapData.PositionArray[index] + new float3(0, .5f, 0)
                    );
                }
            }

            if (buffer.Length!=0 || preExistingLines)
                Segments.Core.SetSegmentChanged(_segments, state.EntityManager);
        }
    }
}
