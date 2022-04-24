using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(8)]
public struct PickupPrefabReferenceElement : IBufferElementData {
    public Entity e;
    public static implicit operator PickupPrefabReferenceElement(Entity e) => new PickupPrefabReferenceElement { e = e };
    public static implicit operator Entity(PickupPrefabReferenceElement e) => e.e;
}

public class PickupPrefabReferenceElementAuthor : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
    public GameObject[] prefabs;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        var buffer = dstManager.AddBuffer<PickupPrefabReferenceElement>(entity);
        foreach (var prefab in prefabs) {
            buffer.Add(conversionSystem.GetPrimaryEntity(prefab));
        }
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.AddRange(prefabs);
    }
}