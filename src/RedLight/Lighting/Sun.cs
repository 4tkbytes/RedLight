using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Lighting;

public class Sun
{
    private RLGraphics graphics;
    private LightManager lightManager;

    public RLLight Light;
    public Entity SunSphere;
    public string Name;
    public Vector3 Direction { get; set; }
    
    public Sun(RLGraphics graphics, LightManager lightManager, string name, Vector3 direction, Color colour)
    {
        this.graphics = graphics;
        this.lightManager = lightManager;
        Name = name;
        Direction = direction;

        // Create a larger sphere to represent the sun
        // var hitbox = HitboxConfig.ForCube(2.0f, 0f);
        SunSphere = new Sphere(graphics, TextureManager.Instance, ShaderManager.Instance, $"{Name}_sun", false).SetScale(new Vector3(0.5f));
        SunSphere.Model.AttachShader(ShaderManager.Instance.Get("light_cube"));
        // SunSphere.SetHitboxConfig(hitbox);

        // Create directional light for the sun
        Light = RLLight.CreateDirectionalLight($"{Name}_light", direction, colour);
        
        lightManager.AddLightWithVisual(Light, SunSphere);
    }
    
    public void Render(Camera camera)
    {
        graphics.Use(SunSphere);
        lightManager.ApplyLightCubeShader($"{Name}_sun", "light_cube", ShaderManager.Instance);
        graphics.Update(camera, SunSphere);
        graphics.Draw(SunSphere);
    }

    public void Update()
    {
        lightManager.UpdateLightPosition($"{Name}_light", SunSphere.Position);
        lightManager.UpdateLightDirection($"{Name}_light", Direction);
    }

    public void Translate(Vector3 translation)
    {
        SunSphere.Translate(translation);
    }

    public void Rotate(float axis, Vector3 rotation)
    {
        SunSphere.Rotate(axis, rotation);
        // Update direction based on rotation if needed
        Direction = Vector3.Transform(Direction, Matrix4x4.CreateFromAxisAngle(rotation, axis));
    }

    public void Scale(Vector3 scale)
    {
        SunSphere.SetScale(scale);
    }

    public void SetDirection(Vector3 newDirection)
    {
        Direction = Vector3.Normalize(newDirection);
    }

    public void OrbitAround(Vector3 center, float radius, float angle)
    {
        // Calculate new position based on orbit
        var x = center.X + radius * MathF.Cos(angle);
        var y = center.Y + radius * MathF.Sin(angle);
        var z = center.Z;
        
        var newPosition = new Vector3(x, y, z);
        SunSphere.SetPosition(newPosition);
        
        // Update direction to point towards center (like sunlight pointing down)
        Direction = Vector3.Normalize(center - newPosition);
    }
}