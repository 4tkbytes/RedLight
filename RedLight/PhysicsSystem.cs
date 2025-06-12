using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Utils;
using Serilog;
using Silk.NET.Maths;
using System.Numerics;

namespace RedLight;
public class PhysicsSystem
{
    // bepu shii
    public Simulation Simulation { get; private set; }
    private BufferPool bufferPool;
    private ThreadDispatcher? threadDispatcher;

    // maps the entity to a physics body handle
    private Dictionary<Entity<Transformable<RLModel>>, BodyHandle> bodyHandles = new();

    // hashset used for debugging
    private HashSet<string> _registeredEntityNames = new();

    public PhysicsSystem(int threadCount = 1)
    {
        Log.Debug("Initialising physics system");

        // init bepu components
        bufferPool = new();
        threadDispatcher = threadCount > 1 ? new ThreadDispatcher(threadCount) : null;

        Simulation = Simulation.Create(
            bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(4, 1)); // Updated to use SolveDescription instead of ThreadDispatcher
        Log.Debug("Initialised PhysicsSystem with a thread count of {threadCount}", threadCount);
    }

    public void AddEntity(Entity<Transformable<RLModel>> entity)
    {
        Log.Debug("Adding entity {EntityType} to physics system", entity.GetType().Name);
        Log.Debug("Entity position: {Position}, Scale: {Scale}", entity.Position, entity.Scale);
        Log.Debug("Entity bounding box: Min={Min}, Max={Max}", entity.BoundingBoxMin, entity.BoundingBoxMax);

        // create a box shape based on entity
        var min = entity.DefaultBoundingBoxMin;
        var max = entity.DefaultBoundingBoxMax;
        var size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);

        // create the shape
        var boxShape = new Box(size.X, size.Y, size.Z);
        var shapeIndex = Simulation.Shapes.Add(boxShape);

        // create the body
        var position = entity.Position;
        var pose = new RigidPose(new Vector3(position.X, position.Y, position.Z));

        BodyHandle bodyHandle;
        if (entity.ApplyGravity)
        {
            // dynamic body
            var inertia = boxShape.ComputeInertia(entity.Mass);
            bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shapeIndex, 0.01f));
        } else
        {
            // static/kinematic body
            bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateKinematic(pose, shapeIndex, 0.01f));
        }

        // store the handle
        bodyHandles[entity] = bodyHandle;
        Log.Debug("Is Dynamic? {IsDynamic}", entity.ApplyGravity);
        Log.Debug("Entity added to physics system successfully");

    }

    public void RemoveEntity(Entity<Transformable<RLModel>> entity)
    {
        if (bodyHandles.TryGetValue(entity, out var bodyHandle))
        {
            Simulation.Bodies.Remove(bodyHandle);
            bodyHandles.Remove(entity);
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

    public void Update(float deltaTime)
    {
        Log.Verbose("Physics system updating with deltaTime: {DeltaTime}", deltaTime);
        // run the simulation
        Simulation.Timestep(deltaTime);

        // update the entities with their new physics state
        foreach (var (entity, bodyHandle) in bodyHandles)
        {
            var bodyRef = Simulation.Bodies.GetBodyReference(bodyHandle);

            // Debug info to understand body state
            Log.Debug("[Physics] Entity: {EntityName}, Awake: {Awake}, Velocity: {Velocity}, Position: {Position}",
                entity.Target.Target.Name,
                bodyRef.Awake,
                bodyRef.Velocity.Linear,
                bodyRef.Pose.Position);

            // Always update position regardless of Awake status
            var pose = bodyRef.Pose;
            var position = new Vector3D<float>(pose.Position.X, pose.Position.Y, pose.Position.Z);
            entity.SetPosition(position);

            // Update velocity if it's dynamic
            if (entity.ApplyGravity)
            {
                entity.Velocity = new Vector3D<float>(
                    bodyRef.Velocity.Linear.X,
                    bodyRef.Velocity.Linear.Y,
                    bodyRef.Velocity.Linear.Z);
            }
        }
    }

    public bool TryGetBodyHandle(Entity<Transformable<RLModel>> entity, out BodyHandle handle)
    {
        return bodyHandles.TryGetValue(entity, out handle);
    }

    public void ApplyImpulse(Entity<Transformable<RLModel>> entity, Vector3 impulse, bool silent = true)
    {
        silent = false;
        if (bodyHandles.TryGetValue(entity, out var handle))
        {
            // Debug the impulse being applied
            if (!silent) Log.Debug("[Physics] Applying impulse {Impulse} to entity {EntityName}",
                impulse, entity.Target.Target.Name);

            // Get the current body state
            var bodyRef = Simulation.Bodies.GetBodyReference(handle);
            if (!silent) Log.Debug("[Physics] Current velocity before impulse: {Velocity}", bodyRef.Velocity.Linear);

            // Apply the impulse
            bodyRef.ApplyLinearImpulse(impulse);

            // Verify the new velocity
            if (!silent) Log.Debug("[Physics] New velocity after impulse: {Velocity}", bodyRef.Velocity.Linear);
        }
        else
        {
            Log.Warning("[Physics] Failed to apply impulse - entity not found in physics system");
        }
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
        pairMaterial = new PairMaterialProperties
        {
            FrictionCoefficient = 0.5f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30, 1)
        };
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
