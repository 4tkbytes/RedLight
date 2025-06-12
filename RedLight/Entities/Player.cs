﻿using Silk.NET.Input;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using Serilog;
using RedLight.Physics;

namespace RedLight.Graphics.Primitive;

public class Player: Entity<Transformable<RLModel>>
{
    public Camera Camera { get; set; }
    
    /// <summary>
    /// Camera toggle changes between first and third person POV's. 
    /// 1 = First Person
    /// 3 = Third Person
    /// Default = Third Person
    /// </summary>
    /// <see cref="PlayerCameraPOV"/>
    public PlayerCameraPOV CameraToggle = PlayerCameraPOV.ThirdPerson;

    public Vector3D<float> Position { get; set; }
    public Vector3D<float> Rotation { get; set; }
    public Vector3D<float> Scale { get; set; }

    public float MoveSpeed { get; set; } = 2.5f;

    private Vector3D<float> lastModelPosition;

    public Player(Camera camera, Transformable<RLModel> model, bool autoMapHitbox = true): base(model)
    {
        Camera = camera;
        Target = model;
        Position = model.Position;
        Rotation = model.Rotation;
        Scale = model.Scale;
        lastModelPosition = Position;
        if (autoMapHitbox)
            AutoMapHitboxToModel();
        Log.Debug("[Player] Created with initial position: {Position}, rotation: {Rotation}, scale: {Scale}", Position, Rotation, Scale);
        ApplyGravity = true;
    }

    public Player(Vector2D<int> screenSize, Transformable<RLModel> model) : this(new Camera(screenSize), model) { }

    /// <summary>
    /// Updates player logic
    /// </summary>
    public void Update(HashSet<Silk.NET.Input.Key> pressedKeys, float deltaTime)
    {
        var prevPos = Position;
        HandleMovement(pressedKeys, deltaTime);
        if (prevPos != Position)
            Log.Verbose("[Player] Position changed: {Prev} -> {Current}", prevPos, Position);

        UpdateCameraPosition();
        UpdatePhysics(deltaTime);
        Log.Debug("Colliding with down, Velocity");
        Velocity = new Vector3D<float>(Velocity.X, 0f, Velocity.Z);

        SyncModelTransform();
    }

    private void HandleMovement(HashSet<Key> pressedKeys, float deltaTime)
    {
        Vector3D<float> direction = Vector3D<float>.Zero;

        var forward = Vector3D.Normalize(new Vector3D<float>(Camera.Front.X, 0, Camera.Front.Z));
        var right = Vector3D.Normalize(Vector3D.Cross(Camera.Front, Camera.Up));
        var up = Camera.Up;

        if (pressedKeys.Contains(Key.W) && !ObjectCollisionSides.Contains(CollisionSide.Front))
            direction += forward;
        if (pressedKeys.Contains(Key.S) && !ObjectCollisionSides.Contains(CollisionSide.Back))
            direction -= forward;
        if (pressedKeys.Contains(Key.A) && !ObjectCollisionSides.Contains(CollisionSide.Left))
            direction -= right;
        if (pressedKeys.Contains(Key.D) && !ObjectCollisionSides.Contains(CollisionSide.Right))
            direction += right;
        if (pressedKeys.Contains(Key.Space) && !ObjectCollisionSides.Contains(CollisionSide.Up))
            direction += up;
        if (pressedKeys.Contains(Key.ShiftLeft) && !ObjectCollisionSides.Contains(CollisionSide.Down))
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
            // first person
            Camera.Position = Position;
            Log.Verbose("[Player] Camera set to first person at {Position}", Position);
        }
        else
        {
            // third person
            float thirdPersonDistance = 5.0f;
            Vector3D<float> cameraOffset = new Vector3D<float>(0, 2, 0);
            Camera.Position = Position - Camera.Front * thirdPersonDistance + cameraOffset;

            var prevPos = Position;
            if (Position != lastModelPosition)
            {
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
        Target.SetModel(modelMatrix);
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

    /// <summary>
    /// Sets the point of view of the players camera. 
    /// </summary>
    /// <param name="cameraPOV"><see cref="PlayerCameraPOV"/></param>
    public void SetPOV(PlayerCameraPOV cameraPOV)
    {
        CameraToggle = cameraPOV;
    }

    /// <summary>
    /// Sets the point of view of the players camera. This function overload uses an int, which is then
    /// type casted into the PlayerCameraPOV. It is recommended to use the <see cref="SetPOV(RedLight.Graphics.Primitive.PlayerCameraPOV)"/>
    /// function to avoid any exceptions. 
    /// </summary>
    /// <param name="cameraPOV"><see cref="int"/> 1 for first person or 3 for third person </param>
    /// <exception cref="Exception"></exception>
    public void SetPOV(int cameraPOV)
    {
        if (cameraPOV != 1 || cameraPOV != 3)
            throw new Exception("Camera POV value is not valid (1|3)");
        CameraToggle = (PlayerCameraPOV)cameraPOV;
    }
}

/// <summary>
/// Enum changing between the different player camera point of views. 
/// </summary>
public enum PlayerCameraPOV
{
    FirstPerson = 1,
    ThirdPerson = 3,
}