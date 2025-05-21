using System.Numerics;
using RedLight.Core;
using RedLight.Scene;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Game;

public class TestingScene1 : RLScene
{
    public required RLWindow Window { get; set; }
    public required SceneManager SceneManager { get; set; }
    public required RLInputHandler inputHandler { get; set; }
    
    private Mesh? mesh;
    private GL? gl;
    private ShaderManager? shaderManager;
    private TextureManager? textureManager;
    private ModelManager? modelManager;
    private Camera? camera;
    private Cube? cube;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");

        gl = GL.GetApi(Window.window);

        shaderManager = new ShaderManager(gl);
        textureManager = new TextureManager(gl);
        modelManager = new ModelManager(gl, textureManager);

        // Create shaders
        shaderManager.Add("3d", RLConstants.RL_BASIC_SHADER_VERT, RLConstants.RL_BASIC_SHADER_FRAG);
        var shader = shaderManager.Get("3d");

        // Load texture from the model's directory
        textureManager.AddTexture(
            new Texture2D(gl, RLFiles.GetAbsolutePath("Resources/Models/LowPolyFerrisRust/rustacean-3d.png")),
            "ferris-texture"
        );        textureManager.AddTexture(
            new Texture2D(gl, RLConstants.RL_NO_TEXTURE),
            "no-texture"
        );
        
        // Create a camera
        camera = new Camera();
        camera.Position = new Vector3(0, 1, 5); // Start a bit above ground level
        camera.SetAspectRatio(Window.window.Size.X, Window.window.Size.Y);
        
        // Initialize camera orientation
        camera.Yaw = -90.0f; // Looking forward along negative Z
        camera.Pitch = 0.0f;  // Looking straight ahead

        // Enable back-face culling and depth testing
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthFunc(DepthFunction.Less);

        // Load the Ferris model
        var ferrisModel = modelManager.LoadModel(
            RLFiles.GetAbsolutePath("Resources/Models/LowPolyFerrisRust/rustacean-3d.obj"), 
            shader,
            textureManager.GetTexture("ferris-texture"));

        if (ferrisModel != null)
        {
            // Create a Ferris entity
            var ferrisEntity = ferrisModel.CreateEntity();
            ferrisEntity.Transform.Scale = new Vector3(0.5f, 0.5f, 0.5f); // Scale it down if needed
            ferrisEntity.Transform.Rotation = new Vector3(0, MathF.PI, 0); // Orient it properly if needed
            Console.WriteLine($"Successfully created Ferris entity with model ID: {ferrisEntity.ModelId}");
        }        else
        {
            Console.WriteLine("Failed to load Ferris model!");
        }
    }
    
    public void OnUpdate(double delta)
    {
        if (camera == null) return;
        
        // Convert delta to seconds for consistent speed regardless of framerate
        float deltaTime = (float)delta;
        
        // Camera movement speed
        float moveSpeed = 2.5f * deltaTime;
        
        // Forward and backward movement (along camera's front vector)
        if (inputHandler.IsKeyDown(Key.W))
            camera.Move(camera.Front, moveSpeed);
        if (inputHandler.IsKeyDown(Key.S))
            camera.Move(-camera.Front, moveSpeed);
            
        // Left and right movement (along camera's right vector)
        if (inputHandler.IsKeyDown(Key.A))
            camera.Move(-camera.Right, moveSpeed);
        if (inputHandler.IsKeyDown(Key.D))
            camera.Move(camera.Right, moveSpeed);
            
        // Up and down movement
        if (inputHandler.IsKeyDown(Key.Space))
            camera.Move(Vector3.UnitY, moveSpeed);
        if (inputHandler.IsKeyDown(Key.ShiftLeft) || inputHandler.IsKeyDown(Key.ShiftRight))
            camera.Move(-Vector3.UnitY, moveSpeed);
              // Handle mouse input for camera rotation
        const float mouseSensitivity = 0.1f;
        
        // Apply mouse movement to camera rotation if the cursor is captured or right mouse button is held
        if (Window.IsCursorCaptured || inputHandler.IsMouseButtonDown(MouseButton.Right))
        {
            float yawDelta = inputHandler.MouseDelta.X * mouseSensitivity;
            float pitchDelta = -inputHandler.MouseDelta.Y * mouseSensitivity; // Reversed to match expected behavior
            
            camera.Yaw += yawDelta;
            camera.Pitch += pitchDelta;
            
            // If cursor is captured, reset to center of screen to allow continuous rotation
            if (Window.IsCursorCaptured && (inputHandler.MouseDelta.X != 0 || inputHandler.MouseDelta.Y != 0))
            {
                Window.SetCursorPosition(Window.GetWindowCenter());
            }
        }
        
        // Also keep the arrow key rotation for convenience
        float rotationSpeed = 50.0f * deltaTime;
        if (inputHandler.IsKeyDown(Key.Up))
            camera.Pitch += rotationSpeed;
        if (inputHandler.IsKeyDown(Key.Down))
            camera.Pitch -= rotationSpeed;
        if (inputHandler.IsKeyDown(Key.Left))
            camera.Yaw -= rotationSpeed;
        if (inputHandler.IsKeyDown(Key.Right))
            camera.Yaw += rotationSpeed;
            
        // Reset mouse delta for the next frame
        inputHandler.ResetMouseDelta();
    }

    public void OnRender(double delta)
    {
        if (gl == null || shaderManager == null || modelManager == null || camera == null) return;
        
        gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);        // Render all models and their entities
        var shader = shaderManager.Get("3d");
        modelManager.RenderAll(shader, camera);
    }
    
    public void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            Window.window.Close();
        }
        
        // Toggle mouse capture with Tab key for camera control
        if (key == Key.Tab)
        {
            bool currentCaptureState = Window.IsCursorCaptured;
            Window.SetCursorVisible(currentCaptureState); // If currently captured, make visible, and vice versa
            
            Console.WriteLine($"Mouse capture toggled: {!currentCaptureState}");
            
            // If we're capturing the cursor, center it in the window
            if (!currentCaptureState)
            {
                Window.SetCursorPosition(Window.GetWindowCenter());
            }
        }
    }
}