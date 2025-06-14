using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using RedLight.Entities;
using RedLight.Graphics;
using Serilog;
using System.Numerics;

namespace RedLight;

public delegate void CollisionEventHandler(Entity entityA, Entity entityB, Vector3 contactPoint, Vector3 normal);

public class PhysicsSystem
{
    
    // bepu shii
    public Simulation Simulation { get; private set; }
    private BufferPool bufferPool;
    private ThreadDispatcher? threadDispatcher;

    // maps the entity to a physics body handle
    private Dictionary<Entity, BodyHandle> bodyHandles = new();
    // reverse mapping from body handle to entity for collision events
    internal Dictionary<BodyHandle, Entity> handleToEntity = new();

    // hashset used for debugging
    private HashSet<string> _registeredEntityNames = new();
    
    // collision events
    public event CollisionEventHandler? OnCollisionEnter;
    public event CollisionEventHandler? OnCollisionStay;
    public event CollisionEventHandler? OnCollisionExit;

    public PhysicsSystem(int threadCount = 1)
    {
        Log.Debug("Initialising physics system");

        // init bepu components
        bufferPool = new();
        threadDispatcher = threadCount > 1 ? new ThreadDispatcher(threadCount) : null;

        Simulation = Simulation.Create(
            bufferPool,
            new NarrowPhaseCallbacks(this),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(4, 1));
        Log.Debug("Initialised PhysicsSystem with a thread count of {threadCount}", threadCount);
    }

    public void AddEntity(Entity entity, bool silent = true)
    {
        if (!silent)
        {
            Log.Debug("Adding entity {EntityType} to physics system", entity.GetType().Name);
            Log.Debug("Entity position: {Position}, Scale: {Scale}", entity.Position, entity.Scale);
            Log.Debug("Entity bounding box: Min={Min}, Max={Max}", entity.BoundingBoxMin, entity.BoundingBoxMax);
        }

        // create a box shape based on entity
        var min = entity.DefaultBoundingBoxMin;
        var max = entity.DefaultBoundingBoxMax;
        var size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);

        // Ensure size is not zero or negative
        size = Vector3.Max(size, new Vector3(0.01f, 0.01f, 0.01f));

        // create the shape
        var boxShape = new Box(size.X, size.Y, size.Z);
        var shapeIndex = Simulation.Shapes.Add(boxShape);

        // Calculate the physics body position including the hitbox offset
        var entityPosition = entity.Position;
        var hitboxCenter = (min + max) * 0.5f; // Center of the hitbox
        var physicsPosition = entityPosition + hitboxCenter; // Offset the physics body position

        var pose = new RigidPose(new Vector3(physicsPosition.X, physicsPosition.Y, physicsPosition.Z));

        BodyHandle bodyHandle;
        
        if (entity.ApplyGravity)
        {
            if (entity is Player)
            {
                var inertia = boxShape.ComputeInertia(entity.Mass * 2.0f);
                bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shapeIndex, 0.2f));

                if (!silent)
                    Log.Debug("Added player entity at physics position: {PhysicsPos} (offset from entity pos: {EntityPos})", 
                        physicsPosition, entityPosition);
            }
            else
            {
                var inertia = boxShape.ComputeInertia(entity.Mass);
                bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shapeIndex, 0.01f));
                
                if (!silent)
                    Log.Debug("Added dynamic entity: {EntityName} at physics position: {PhysicsPos}", 
                        entity.Model?.Name, physicsPosition);
            }
        }
        else
        {
            bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateKinematic(pose, shapeIndex, 0.01f));
            
            if (!silent)
                Log.Debug("Added kinematic entity: {EntityName} at physics position: {PhysicsPos}", 
                    entity.Model?.Name, physicsPosition);
        }

        // store the handle
        bodyHandles[entity] = bodyHandle;
        handleToEntity[bodyHandle] = entity;
        entity.PhysicsSystem = this;
        
        if (!silent)
        {
            Log.Debug("Hitbox center offset: {HitboxCenter}", hitboxCenter);
            Log.Debug("Entity added to physics system successfully");
        }
    }
    
    public void RemoveEntity(Entity entity)
    {
        if (bodyHandles.TryGetValue(entity, out var bodyHandle))
        {
            Simulation.Bodies.Remove(bodyHandle);
            bodyHandles.Remove(entity);
            handleToEntity.Remove(bodyHandle);
            entity.PhysicsSystem = null;
            Log.Debug("Removed entity {Type} from physics system", entity.GetType());
        }
    }

    public void ListRegisteredEntities()
    {
        Log.Debug("Physics system contains {Count} registered entities", _registeredEntityNames.Count);
        foreach (var name in _registeredEntityNames)
        {
            Log.Information("  - {EntityName}", name);
        }
    }

    public void Update(float deltaTime, bool silent = true)
    {
        Log.Verbose("Physics system updating with deltaTime: {DeltaTime}", deltaTime);
        // run the simulation
        Simulation.Timestep(deltaTime);

        // update the entities with their new physics state
        foreach (var (entity, bodyHandle) in bodyHandles)
        {
            var bodyRef = Simulation.Bodies.GetBodyReference(bodyHandle);

            // Debug info to understand body state
            if (!silent) Log.Debug("[Physics] Entity: {EntityName}, Awake: {Awake}, Velocity: {Velocity}, Position: {Position}",
                entity.Model?.Name,
                bodyRef.Awake,
                bodyRef.Velocity.Linear,
                bodyRef.Pose.Position);

            // Get the physics body position
            var pose = bodyRef.Pose;
            var physicsPosition = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
        
            // Calculate the hitbox offset that was applied when creating the body
            var min = entity.DefaultBoundingBoxMin;
            var max = entity.DefaultBoundingBoxMax;
            var hitboxCenter = (min + max) * 0.5f;
        
            // Calculate the entity position by subtracting the hitbox offset
            var entityPosition = physicsPosition - hitboxCenter;
        
            // Update the entity's position
            entity.SetPosition(entityPosition);

            // Update velocity if it's dynamic
            if (entity.ApplyGravity)
            {
                entity.Velocity = new Vector3(
                    bodyRef.Velocity.Linear.X,
                    bodyRef.Velocity.Linear.Y,
                    bodyRef.Velocity.Linear.Z);
            }
        }
    }

    public bool TryGetBodyHandle(Entity entity, out BodyHandle handle)
    {
        return bodyHandles.TryGetValue(entity, out handle);
    }

    public void ApplyImpulse(Entity entity, Vector3 impulse, bool silent = true)
    {
        if (bodyHandles.TryGetValue(entity, out var handle))
        {
            if (!silent) Log.Debug("[Physics] Applying impulse {Impulse} to entity {EntityName}",
                impulse, entity.Model.Name);

            var bodyRef = Simulation.Bodies.GetBodyReference(handle);
            if (!silent) Log.Debug("[Physics] Current velocity before impulse: {Velocity}", bodyRef.Velocity.Linear);

            // apply impulse
            bodyRef.ApplyLinearImpulse(impulse);

            if (!silent) Log.Debug("[Physics] New velocity after impulse: {Velocity}", bodyRef.Velocity.Linear);
        }
        else
        {
            Log.Warning("[Physics] Failed to apply impulse - entity not found in physics system");
        }
    }

    internal void TriggerCollisionEvent(Entity entityA, Entity entityB, Vector3 contactPoint, Vector3 normal, bool silent = true)
    {
        // Update entity collision state
        entityA.IsColliding = true;
        entityB.IsColliding = true;
        
        // Trigger collision events
        OnCollisionEnter?.Invoke(entityA, entityB, contactPoint, normal);
        
        if (!silent) Log.Debug("[Collision] {EntityA} collided with {EntityB} at {ContactPoint}", 
            entityA.GetType().Name, entityB.GetType().Name, contactPoint);
    }

    public void Dispose()
    {
        Simulation.Dispose();
        bufferPool.Clear();
        threadDispatcher?.Dispose();
        Log.Debug("Physics system has been disposed");
    }
}

public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    private PhysicsSystem physicsSystem;
    
    public NarrowPhaseCallbacks(PhysicsSystem system)
    {
        physicsSystem = system;
    }

    public void Initialize(Simulation simulation) { }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float maximumExpansion)
    {
        return true;
    }

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Set material properties for collision response
        pairMaterial = new PairMaterialProperties
        {
            FrictionCoefficient = 0.8f,  // Low friction for smooth movement
            MaximumRecoveryVelocity = 1.0f,
            SpringSettings = new SpringSettings(20f, 1.0f)  // Softer springs
        };

        // Trigger collision event if physics system is available
        if (physicsSystem != null)
        {
            // Try to get entities from body handles
            var bodyA = pair.A.BodyHandle;
            var bodyB = pair.B.BodyHandle;
            
            if (physicsSystem.handleToEntity.TryGetValue(bodyA, out var entityA) && 
                physicsSystem.handleToEntity.TryGetValue(bodyB, out var entityB))
            {
                // Simplified collision event - we'll get contact details from the physics system
                Vector3 contactPoint = Vector3.Zero;
                Vector3 normal = Vector3.UnitY;

                // Trigger collision event
                physicsSystem.TriggerCollisionEvent(entityA, entityB, contactPoint, normal);
            }
        }

        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
        // *crickets*
    }
}

public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public Vector3 Gravity;
    public float dt;

    public PoseIntegratorCallbacks(Vector3 gravity)
    {
        Gravity = gravity;
        dt = 0;
    }

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float deltaTime)
    {
        dt = deltaTime;
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        // note to self: check if this works it doesnt look good

        // Correct the multiplication by broadcasting the Gravity vector to a Vector3Wide
        Vector3Wide.Broadcast(Gravity, out var gravityWide);

        // Multiply the broadcasted gravity vector with the delta time
        Vector3Wide.Scale(gravityWide, dt, out var scaledGravity);

        // Add the scaled gravity to the linear velocity
        Vector3Wide.Add(velocity.Linear, scaledGravity, out velocity.Linear);
    }

    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.ConserveMomentum;

    public bool AllowSubstepsForUnconstrainedBodies => false;
    public bool IntegrateVelocityForKinematics => false;
}