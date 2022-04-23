using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class DamageSystem : SystemBase {
    private StepPhysicsWorld physicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem _entityCommandBufferSystem;
    private EntityQuery _healthSpritesQuery;

    protected override void OnCreate() {
        physicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
        _entityCommandBufferSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        _healthSpritesQuery = GetEntityQuery(typeof(HealthSprites));
        RequireForUpdate(_healthSpritesQuery);
    }

    protected override void OnUpdate() {
        var entityToDepleteHealth = new NativeHashSet<Entity>(4, Allocator.TempJob);
        var job = new BulletTriggerJob {
            bullets = GetComponentDataFromEntity<BulletDamage>(true),
            bulletOrigin = GetComponentDataFromEntity<BulletOrigin>(true),
            healths = GetComponentDataFromEntity<Health>(true),
            ecb = _entityCommandBufferSystem.CreateCommandBuffer(),
            entityToDepleteHealth = entityToDepleteHealth
        };
        Dependency = job.Schedule(physicsWorld.Simulation, Dependency);
        _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        
        CompleteDependency();
        var healthSprites = EntityManager.GetComponentObject<HealthSprites>(_healthSpritesQuery.GetSingletonEntity()).Values;
        foreach (var e in entityToDepleteHealth) { 
            var h = GetComponent<Health>(e);
            h.Left--;
            SetComponent(e, h);

            var renderer = EntityManager.GetComponentObject<SpriteRenderer>(e).GetComponent<HealthbarReference>().r;
            renderer.sprite = healthSprites[h.Left];

            if (h.Left <= 0) {
                EntityManager.DestroyEntity(e);
            }
        }

        entityToDepleteHealth.Dispose();
    }
}

[BurstCompile]
public struct BulletTriggerJob : ITriggerEventsJob {
    [ReadOnly] public ComponentDataFromEntity<BulletDamage> bullets;
    [ReadOnly] public ComponentDataFromEntity<BulletOrigin> bulletOrigin;
    [ReadOnly] public ComponentDataFromEntity<Health> healths;
    public EntityCommandBuffer ecb;
    public NativeHashSet<Entity> entityToDepleteHealth;

    public void Execute(TriggerEvent triggerEvent) {
        BulletVHealth(triggerEvent.EntityA, triggerEvent.EntityB);
        BulletVHealth(triggerEvent.EntityB, triggerEvent.EntityA);
    }

    private void BulletVHealth(Entity bulletEntity, Entity healthEntity) {
        if (!bullets.HasComponent(bulletEntity)) return;
        if (bulletOrigin[bulletEntity].Value == healthEntity || bullets.HasComponent(healthEntity)) return;

        if (healths.HasComponent(healthEntity)) {
            entityToDepleteHealth.Add(healthEntity);
        }

        ecb.DestroyEntity(bulletEntity);
    }
}