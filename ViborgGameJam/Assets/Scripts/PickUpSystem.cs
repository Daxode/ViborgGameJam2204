using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class PickUpSystem : SystemBase {
    private StepPhysicsWorld physicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate() {
        physicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
        _entityCommandBufferSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
        var job = new PickUpTriggerJob {
            hands = GetComponentDataFromEntity<Hands>(),
            pickups = GetComponentDataFromEntity<PickUpData>(),
        };
        Dependency = job.Schedule(physicsWorld.Simulation, Dependency);
        _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        
        
        Entities.WithChangeFilter<Hands>().ForEach((Entity e, in Hands hands) => {
            var render = EntityManager.GetComponentObject<PowerUpIndicatorRendererReference>(e);
            render.Renderer.color = hands.Type switch {
                PickUpType.ColorChange => Color.blue,
                PickUpType.Desaturate => Color.grey,
                _ => new Color(0,0,0,0)
            };
        }).WithoutBurst().Run();
        
        Entities.WithChangeFilter<PickUpData>().ForEach((Entity e, in PickUpData data) => {
            var render = EntityManager.GetComponentObject<SpriteRenderer>(e);
            render.gameObject.SetActive(!data.PickedUp || data.Type == PickUpType.Animal || data.Type == PickUpType.Empty);
        }).WithoutBurst().Run();
    }
}

//[BurstCompile]
public struct PickUpTriggerJob : ITriggerEventsJob {
    public ComponentDataFromEntity<Hands> hands;
    public ComponentDataFromEntity<PickUpData> pickups;

    public void Execute(TriggerEvent triggerEvent) {
        HandsVPickUp(triggerEvent.EntityA, triggerEvent.EntityB);
        HandsVPickUp(triggerEvent.EntityB, triggerEvent.EntityA);
    }

    private void HandsVPickUp(Entity handsEntity, Entity pickupEntity) {
        if (!hands.HasComponent(handsEntity)) return;
        if (!pickups.HasComponent(pickupEntity)) return;
        var hand = hands[handsEntity];
        var pickup = pickups[pickupEntity];
        if (hand.Type == PickUpType.Empty && !pickup.PickedUp) {
            pickup.PickedUp = true;
            hand.Type = pickup.Type;
            hand.HeldEntity = pickupEntity;
            hands[handsEntity] = hand;
            pickups[pickupEntity] = pickup;
        }
    }
}