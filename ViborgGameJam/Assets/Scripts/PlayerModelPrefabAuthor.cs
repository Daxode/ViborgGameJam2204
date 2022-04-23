using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PlayerModelPrefabAuthor : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
    public GameObject _modelPrefab;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddComponentData(entity, new PlayerModelPrefabReference{Value = _modelPrefab});
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(_modelPrefab);
    }
}
public class PlayerModelPrefabReference : IComponentData {
    public GameObject Value;
}