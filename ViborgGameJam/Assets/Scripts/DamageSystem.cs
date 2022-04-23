using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            var timer = GetComponent<HealthCooldown>(e);
            if (timer.TimeLeft>0) continue;
            
            var h = GetComponent<Health>(e);
            h.Left--;
            SetComponent(e, h);
            timer.TimeLeft = timer.Interval;
            SetComponent(e, timer);

            var renderer = EntityManager.GetComponentObject<SpriteRenderer>(e).GetComponent<HealthbarReference>().r;
            renderer.sprite = healthSprites[h.Left];

            if (h.Left <= 0) {
                EntityManager.DestroyEntity(e);
            }
        }

        var deltaTime = Time.DeltaTime;
        Entities.ForEach((SpriteRenderer r, ref HealthCooldown cooldown) => {
            cooldown.TimeLeft -= deltaTime;
            var col = r.color;
            col.a = math.sin(cooldown.TimeLeft*10) * 0.4f + 0.5f;
            r.color = col;
            var renderer = r.GetComponent<HealthbarReference>().r;
            var c = renderer.color;
            c.a = math.sin(cooldown.TimeLeft*10+5) * 0.4f + 0.5f;
            renderer.color = c;
            if (cooldown.TimeLeft > 0) return;
            col.a = 1;
            r.color = col;
            
            renderer.sprite = null;
        }).WithoutBurst().Run();

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