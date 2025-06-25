using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Lighting;
using RedLight.Physics;
using RedLight.UI;
using RedLight.UI.ImGui;
using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Scene;

public abstract class RLScene
{
    public abstract RLEngine Engine { get; set; }
    public abstract RLGraphics Graphics { get; set; }
    public abstract SceneManager SceneManager { get; set; }
    public abstract ShaderManager ShaderManager { get; set; }
    public abstract TextureManager TextureManager { get; set; }
    public abstract InputManager InputManager { get; set; }
    public abstract PhysicsSystem PhysicsSystem { get; set; }
    public abstract LightManager LightManager { get; set; }
    public abstract TextManager TextManager { get; set; }


    public List<Entity> ObjectModels = new();

    public abstract void OnLoad();

    internal void Load()
    {
        Log.Debug("Scene loaded");
    }

    public abstract void OnUpdate(double deltaTime);

    public virtual void OnRender(double deltaTime) // useful for making stuff organised
    {

    }

    public void AddToLists<T>(T item, RLImGuiEditor _editor = null)
    {
        switch (item)
        {
            case Entity entity:
                // Add to main object models list
                ObjectModels.Add(entity);

                // Add to physics system if it has physics properties
                if (entity.EnablePhysics)
                {
                    PhysicsSystem.AddEntity(entity);
                }

                // Special handling for different entity types
                switch (entity)
                {
                    case Player playerEntity:
                        // Player-specific logic if needed
                        Log.Debug("Added player entity to lists");
                        break;

                    case Cube cubeEntity:
                        // Cube-specific logic if needed
                        Log.Debug("Added cube entity to lists");
                        break;

                    case Plane planeEntity:
                        // Plane-specific logic if needed
                        Log.Debug("Added plane entity to lists");
                        break;
                }
                break;

            case RLLight light:
                // Add light to light manager
                LightManager.AddLight(light);
                Log.Debug("Added light: {Name} of type {Type}", light.Name, light.Type);
                break;

            case LightingCube lightingCube:
                // Add lighting cube to light manager
                AddToLists(lightingCube.Light);
                AddToLists(lightingCube.Cube);

                Log.Debug("Added lighting cube: {Name}", lightingCube.Name);
                break;

            case RLTexture texture:
                // Add texture to texture manager
                TextureManager.TryAdd(texture.Name ?? $"texture_{TextureManager.Instance.textures.Count}", texture);
                Log.Debug("Added texture to texture manager");
                break;

            case RLShaderBundle shader:
                // Add shader to shader manager
                ShaderManager.TryAdd(shader.Name, shader);
                Log.Debug("Added shader to shader manager");
                break;

            case Mesh:
                throw new NotSupportedException(
                    "Meshes are not used anymore (in this case). It is recommended to use the RLModel class or RedLightUI classes instead of Meshes. Sorry :(");
            default:
                throw new NotSupportedException($"The item is of type {item.GetType()} is not supported in the AddToLists function. Sorry :(");
        }
    }

    public void RenderModel(Camera activeCamera, CubeMap skybox = null)
    {
        // Bind skybox to texture unit 2 BEFORE rendering objects
        if (skybox != null)
        {
            skybox.Bind(TextureUnit.Texture2);
        }

        foreach (var model in ObjectModels)
        {
            if (model.ModelType == ModelType.Light)
            {
                continue;
            }
            else
            {
                Graphics.Use(model);
                if (LightManager != null)
                    LightManager.ApplyLightsToShader(activeCamera.Position, model); // Pass the model for per-entity reflection
                Graphics.Update(activeCamera, model);
                Graphics.Draw(model);
            }
        }
    }

    public void BeforeEditorRender(RLImGuiEditor editor, Camera activeCamera)
    {
        if (editor.IsEditorMode)
        {
            editor.SetModelList(ObjectModels, activeCamera);
        }

        if (editor.IsEditorMode)
        {
            editor.GameFramebuffer.Bind();

            var viewportSize = editor.ViewportSize;
            if (viewportSize.X > 0 && viewportSize.Y > 0)
            {
                activeCamera.UpdateAspectRatio(viewportSize.X / viewportSize.Y);
            }
        }
        else
        {
            var windowSize = Engine.Window.Window.FramebufferSize;
            if (windowSize.X > 0 && windowSize.Y > 0)
            {
                activeCamera.UpdateAspectRatio((float)windowSize.X / windowSize.Y);
            }
        }
    }

    public void AfterEditorRender(RLImGuiEditor editor)
    {
        if (editor.IsEditorMode)
        {
            editor.GameFramebuffer.Unbind();

            Graphics.OpenGL.Viewport(Engine.Window.Window.FramebufferSize);
        }

        editor.Render();
    }
}