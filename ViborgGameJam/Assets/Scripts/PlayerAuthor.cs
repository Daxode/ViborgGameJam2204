using Unity.Entities;
using UnityEngine;

public class PlayerAuthor : MonoBehaviour, IConvertGameObjectToEntity {
    public int health = 3;
    public float ShootingInterval = 1f;
    public float HealthInterval = 5f;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddComponentData(entity, new PlayerTag());
        dstManager.AddComponentData(entity, new Health { Left = health, Total = health });
        dstManager.AddComponentData(entity, new ShootingCooldown{Interval = ShootingInterval});
        dstManager.AddComponentData(entity, new HealthCooldown{Interval = HealthInterval});
        dstManager.AddComponentData(entity, new TargetEntity());
        dstManager.AddComponentData(entity, new PlayerTargetIndex());
        dstManager.AddComponentData(entity, new PlayerIndex());
        dstManager.AddComponentData(entity, new Hands());
    }
}


public struct Hands : IComponentData {
    public Entity HeldEntity;
    public PickUpType Type;
}

public struct PlayerIndex : IComponentData {
    public int Value;
}
public struct PlayerTargetIndex : IComponentData {
    public int Value;
}

public struct TargetEntity : IComponentData {
    public Entity target;
}

public struct Health : IComponentData {
    public int Left;
    public int Total;
}

public struct HealthCooldown : IComponentData {
    public float TimeLeft;
    public float Interval;
}

public struct ShootingCooldown : IComponentData {
    public float TimeLeft;
    public float Interval;
}