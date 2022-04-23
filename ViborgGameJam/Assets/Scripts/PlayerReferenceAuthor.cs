using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PlayerReferenceAuthor : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
    public PlayerAuthor player;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddComponentData(entity, new PlayerPrefabReference(conversionSystem.GetPrimaryEntity(player)));
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(player.gameObject);
    }
}

public struct PlayerPrefabReference : IComponentData {
    public Entity Value;

    public PlayerPrefabReference(Entity value) {
        Value = value;
    }
}