using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

public partial class PickUpSpawnerSystem : SystemBase {
    private EntityQuery pickupPrefabQuery;
    protected override void OnCreate() {
        RequireForUpdate(GetEntityQuery(typeof(GameStartedTag))); // Make sure game has started
        pickupPrefabQuery = GetEntityQuery(typeof(PickupPrefabReferenceElement));
    }

    private Random rnd;
    protected override void OnStartRunning() {
        rnd.InitState((uint)DateTime.Now.Ticks);
    }

    protected override void OnUpdate() {
        var deltaTime = Time.DeltaTime;
        Entities.ForEach((ref SpawnerCoolDown coolDown, ref SpawnerActivePickup activePickup, in Translation t) => {
            coolDown.TimeLeft -= deltaTime;
            if (coolDown.TimeLeft>0) return;
            if (EntityManager.Exists(activePickup.CurrentEntity)) return;
            coolDown.TimeLeft = coolDown.Interval;
            
            var prefabs = GetBuffer<PickupPrefabReferenceElement>(pickupPrefabQuery.GetSingletonEntity());
            var prefab = prefabs[rnd.NextInt(prefabs.Length)];

            var pickupEntity = EntityManager.Instantiate(prefab);
            activePickup.CurrentEntity = pickupEntity;
            SetComponent(pickupEntity, t);
            var pickupModelEntity = GetComponent<ModelReference>(pickupEntity).Value;
            var modelPrefab = EntityManager.GetComponentObject<PrefabReference>(pickupModelEntity);
            var pickupModelInstance = Object.Instantiate(modelPrefab.Prefab);
            EntityManager.AddComponentObject(pickupEntity, pickupModelInstance.GetComponent<Animator>());
            EntityManager.AddComponentObject(pickupEntity, pickupModelInstance.GetComponent<SpriteRenderer>());
            EntityManager.AddComponentObject(pickupModelEntity, pickupModelInstance.transform);
            EntityManager.AddComponentData(pickupModelEntity, new CopyTransformToGameObject());
            EntityManager.AddComponentData(pickupModelEntity, new GameObjectStateComponent{GameObject = pickupModelInstance});
        }).WithStructuralChanges().Run();
    }
}