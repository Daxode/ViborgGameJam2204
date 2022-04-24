using Unity.Entities;
using UnityEngine;

public class SpawnerAuthor : MonoBehaviour, IConvertGameObjectToEntity {
    public float SpawnerInterval = 1f;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddComponentData(entity, new SpawnerCoolDown { Interval = SpawnerInterval, TimeLeft = SpawnerInterval});
        dstManager.AddComponentData(entity, new SpawnerActivePickup());
    }
}

public struct SpawnerCoolDown : IComponentData {
    public float Interval;
    public float TimeLeft;
}

public struct SpawnerActivePickup : IComponentData {
    public Entity CurrentEntity;
}