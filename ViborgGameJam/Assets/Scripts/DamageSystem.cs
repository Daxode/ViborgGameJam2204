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
    protected override void OnCreate() {
        physicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
        _entityCommandBufferSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
        var job = new BulletTriggerJob {
            bullets = GetComponentDataFromEntity<BulletDamage>(true),
            bulletOrigin = GetComponentDataFromEntity<BulletOrigin>(true),
            healths = GetComponentDataFromEntity<Health>(),
            ecb = _entityCommandBufferSystem.CreateCommandBuffer()
        };
        Dependency = job.Schedule(physicsWorld.Simulation, Dependency);
        _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}

//[BurstCompile]
public struct BulletTriggerJob : ITriggerEventsJob {
    [ReadOnly] public ComponentDataFromEntity<BulletDamage> bullets;
    [ReadOnly] public ComponentDataFromEntity<BulletOrigin> bulletOrigin;
    
    public ComponentDataFromEntity<Health> healths;
    public EntityCommandBuffer ecb;

    public void Execute(TriggerEvent triggerEvent) {
        var bulletEntity = triggerEvent.EntityA;
        var healthEntity = triggerEvent.EntityB;

        if (!bullets.HasComponent(bulletEntity)) return;
        if (bulletOrigin[bulletEntity].Value == healthEntity || bullets.HasComponent(healthEntity)) return;
        
        if (healths.HasComponent(healthEntity)) {
            var health = healths[healthEntity];
            health.Left--;
            Debug.Log(health.Left);
            healths[healthEntity] = health;
            if (health.Left < 0) {
                ecb.DestroyEntity(healthEntity);
            }
        }
            
        ecb.DestroyEntity(bulletEntity);
    }
}