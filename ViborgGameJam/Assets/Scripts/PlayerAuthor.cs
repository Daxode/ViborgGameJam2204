using Unity.Entities;
using UnityEngine;

public class PlayerAuthor : MonoBehaviour, IConvertGameObjectToEntity {
    public int health = 3;
    public float ShootingInterval = 1f;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddComponentData(entity, new PlayerTag());
        dstManager.AddComponentData(entity, new Health { Left = health, Total = health });
        dstManager.AddComponentData(entity, new Cooldown{Interval = ShootingInterval});
    }
}

public struct Health : IComponentData {
    public int Left;
    public int Total;
}

public struct Cooldown : IComponentData {
    public float TimeLeft;
    public float Interval;
}