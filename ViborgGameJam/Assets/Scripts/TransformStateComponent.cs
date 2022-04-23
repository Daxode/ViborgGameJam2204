using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public class GameObjectStateComponent : ISystemStateComponentData {
    public GameObject GameObject;
}

[BurstCompile]
struct DeleteCompanionSystem : ISystem {
    private EntityQuery query;
    public void OnCreate(ref SystemState state) {
        query = state.GetEntityQuery(
            new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<GameObjectStateComponent>()},
                None = new[] {ComponentType.ReadOnly<Transform>()}
            }
        );
    }

    public void OnDestroy(ref SystemState state) {}

    public void OnUpdate(ref SystemState state) {
        var array = query.ToComponentDataArray<GameObjectStateComponent>();
        foreach (var transformStateComponent in array) {
            Object.Destroy(transformStateComponent.GameObject);
        }
        state.EntityManager.DestroyEntity(query);
    }
}