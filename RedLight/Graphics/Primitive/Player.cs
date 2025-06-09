using Silk.NET.Input;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using Serilog;

namespace RedLight.Graphics.Primitive;

public class Player
{
    public Camera Camera { get; set; }
    public Transformable<RLModel> Model { get; set; }

    /// <summary>
    /// Camera toggle changes between first and third person POV's. 
    /// 1 = First Person
    /// 3 = Third Person
    /// Default = First Person
    /// </summary>
    /// <see cref="PlayerCameraPOV"/>
    public PlayerCameraPOV CameraToggle = PlayerCameraPOV.FirstPerson;

    public Vector3D<float> Position { get; set; }
    public Vector3D<float> Rotation { get; set; }
    public Vector3D<float> Scale { get; set; }

    public float MoveSpeed { get; set; } = 2.5f;

    private Vector3D<float> lastModelPosition;

    public Player(Camera camera, Transformable<RLModel> model)
    {
        Camera = camera;
        Model = model;
        Position = model.Position;
        Rotation = model.Rotation;
        Scale = model.Scale;
        lastModelPosition = Position;
        Log.Debug("[Player] Created with initial position: {Position}, rotation: {Rotation}, scale: {Scale}", Position, Rotation, Scale);
    }

    public Player(Vector2D<int> screenSize, Transformable<RLModel> model) : this(new Camera(screenSize), model) { }

    /// <summary>
    /// Call this every frame to update player logic.
    /// </summary>
    public void Update(HashSet<Silk.NET.Input.Key> pressedKeys, float deltaTime)
    {
        var prevPos = Position;
        HandleMovement(pressedKeys, deltaTime);
        if (prevPos != Position)
            Log.Verbose("[Player] Position changed: {Prev} -> {Current}", prevPos, Position);

        UpdateCameraPosition();
        SyncModelTransform();
    }

    private void HandleMovement(HashSet<Silk.NET.Input.Key> pressedKeys, float deltaTime)
    {
        Vector3D<float> direction = Vector3D<float>.Zero;

        // Use the player camera's orientation for movement
        var forward = Vector3D.Normalize(new Vector3D<float>(Camera.Front.X, 0, Camera.Front.Z));
        var right = Vector3D.Normalize(Vector3D.Cross(Camera.Front, Camera.Up));
        var up = Camera.Up;

        if (pressedKeys.Contains(Silk.NET.Input.Key.W))
            direction += forward;
        if (pressedKeys.Contains(Silk.NET.Input.Key.S))
            direction -= forward;
        if (pressedKeys.Contains(Silk.NET.Input.Key.A))
            direction -= right;
        if (pressedKeys.Contains(Silk.NET.Input.Key.D))
            direction += right;
        if (pressedKeys.Contains(Silk.NET.Input.Key.Space))
            direction += up;
        if (pressedKeys.Contains(Silk.NET.Input.Key.ShiftLeft))
            direction -= up;

        if (direction != Vector3D<float>.Zero)
            direction = Vector3D.Normalize(direction);

        if (direction != Vector3D<float>.Zero)
            Log.Verbose("[Player] Moving direction: {Direction}, speed: {Speed}, deltaTime: {DeltaTime}", direction, MoveSpeed, deltaTime);

        Position += direction * MoveSpeed * deltaTime;
    }

    /// <summary>
    /// Toggle between first and third person camera.
    /// </summary>
    public void ToggleCamera()
    {
        var prevPOV = CameraToggle;
        if (CameraToggle == PlayerCameraPOV.FirstPerson)
        {
            CameraToggle = PlayerCameraPOV.ThirdPerson;
        }
        else
        {
            CameraToggle = PlayerCameraPOV.FirstPerson;
        }
        Log.Debug("[Player] Camera POV toggled: {Prev} -> {Current}", prevPOV, CameraToggle);
        UpdateCameraPosition();
    }

    /// <summary>
    /// Updates the camera's position based on the current perspective.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (CameraToggle == PlayerCameraPOV.FirstPerson)
        {
            // First person: camera at player position
            Camera.Position = Position;
            Log.Verbose("[Player] Camera set to first person at {Position}", Position);
        }
        else
        {
            // Third person: camera behind the player
            float thirdPersonDistance = 5.0f;
            Vector3D<float> cameraOffset = new Vector3D<float>(0, 2, 0);
            Camera.Position = Position - Camera.Front * thirdPersonDistance + cameraOffset;

            var prevPos = Position;
            if (Position != lastModelPosition)
            {
                // Update rotation to match camera yaw (Y axis, or change to Z if needed)
                Rotation = new Vector3D<float>(Rotation.X, -float.DegreesToRadians(Camera.Yaw), Rotation.Z);
                lastModelPosition = Position;
            }

            if (prevPos != Position)
                Log.Verbose("[Player] Player's position changed: {Prev} -> {Current}", prevPos, Position);

            SyncModelTransform();

            Log.Verbose("[Player] Camera set to third person at {Position}", Camera.Position);
            Log.Verbose("[Player] Player's rotation set to {Rotation}", Rotation);
        }
        Camera.UpdateCamera();
    }

    /// <summary>
    /// Syncs the model's transform with the player's position, rotation, and scale.
    /// </summary>
    private void SyncModelTransform()
    {
        var scaleMatrix = Matrix4X4.CreateScale(Scale.X, Scale.Y, Scale.Z);
        var rotationMatrix = Matrix4X4.CreateRotationX(Rotation.X) 
            * Matrix4X4.CreateRotationY(Rotation.Y) 
            * Matrix4X4.CreateRotationZ(Rotation.Z);
        var translationMatrix = Matrix4X4.CreateTranslation(Position.X, Position.Y, Position.Z);
        var modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;
        Model.SetModel(modelMatrix);
        Log.Verbose("[Player] Model transform updated. Position: {Position}, Rotation: {Rotation}, Scale: {Scale}", Position, Rotation, Scale);
    }

    /// <summary>
    /// Example free roam method (can be expanded).
    /// </summary>
    public void FreeRoam()
    {
        Log.Debug("[Player] FreeRoam called.");
        // Implement free roam logic if needed
    }
}

// Extension method for extracting translation from Matrix4X4<float>
public static class MatrixExtensions
{
    public static Vector3D<float> M41M42M43(this Matrix4X4<float> m)
        => new Vector3D<float>(m.M41, m.M42, m.M43);
}

/// <summary>
/// Enum changing between the different player camera point of views. 
/// </summary>
public enum PlayerCameraPOV
{
    FirstPerson = 1,
    ThirdPerson = 3,
}