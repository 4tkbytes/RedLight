using System.Numerics;
using RedLight.Entities;
using Serilog;
using RedLight.Graphics;

namespace RedLight.Lighting;

public class LightManager
{
    private readonly Dictionary<string, RLLight> _lights;
    private readonly Dictionary<string, Entity> _lightCubes;
    
    private float _diffuse;
    private float _specular = 1;
    private float _shininess = 32f;
    
    public float Specular
    {
        get => _specular;
        set => _specular = value;
    }

    public float Diffuse
    {
        get => _diffuse;
        set => _diffuse = value;
    }

    public float Shininess
    {
        get => _shininess;
        set => _shininess = value;
    }

    public LightManager()
    {
        _lights = new Dictionary<string, RLLight>();
        _lightCubes = new Dictionary<string, Entity>();
    }

    public void AddLight(RLLight light)
    {
        if (_lights.ContainsKey(light.Name))
        {
            Log.Warning("Light with name {Name} already exists. Replacing.", light.Name);
        }
        
        _lights[light.Name] = light;
        Log.Verbose("Added light: {Name} of type {Type}", light.Name, light.Type);
    }

    public void AddLightWithVisual(RLLight light, Entity lightCube)
    {
        AddLight(light);
        _lightCubes[light.Name] = lightCube;
        Log.Verbose("Added light with visual representation: {Name}", light.Name);
    }

    public RLLight? GetLight(string name)
    {
        return _lights.TryGetValue(name, out var light) ? light : null;
    }

    public Entity? GetLightCube(string name)
    {
        return _lightCubes.TryGetValue(name, out var cube) ? cube : null;
    }

    public void RemoveLight(string name)
    {
        if (_lights.Remove(name))
        {
            _lightCubes.Remove(name);
            Log.Verbose("Removed light: {Name}", name);
        }
    }

    public void UpdateLightPosition(string name, Vector3 direction)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.Direction = direction;
        }
    }

    public void UpdateLightColor(string name, Vector3 color)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.Colour = color;
        }
    }
    
    public void UpdateLightDirection(string name, Vector3 direction)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.Direction = Vector3.Normalize(direction);
        }
    }

    public void EnableLight(string name, bool enabled = true)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.IsEnabled = enabled;
        }
    }
    
    public void ApplyLightsToShader(Vector3 viewPosition, string shaderName = "lit")
    {
        var shaderManager = ShaderManager.Instance;

        foreach (var light in _lights.Values)
        {
            try
            {
                if (shaderManager.HasUniform(shaderName, "viewPos"))
                    shaderManager.SetUniform(shaderName, "viewPos", viewPosition);

                var enabledLights = _lights.Values.Where(l => l.IsEnabled).ToList();
            
                var primaryLight = enabledLights.FirstOrDefault();
                if (primaryLight != null)
                {
                    ApplySingleLight(shaderName, light, shaderManager);
                }
                else
                {
                    if (shaderManager.HasUniform(shaderName, "light.colour"))
                        shaderManager.SetUniform(shaderName, "light.colour", Vector3.Zero);
                
                    if (shaderManager.HasUniform(shaderName, "light.position"))
                        shaderManager.SetUniform(shaderName, "light.position", Vector3.Zero);
                }
                
                if (shaderManager.HasUniform(shaderName, "material.shininess"))
                    shaderManager.SetUniform(shaderName, "material.shininess", _shininess);
            }
            catch (Exception ex)
            {
                Log.Error("Error applying lights to shader {ShaderName}: {Error}", shaderName, ex.Message);
            }
        }
    }

    public void ApplyLightCubeShader(string lightName, string shaderName, ShaderManager shaderManager)
    {
        try
        {
            if (shaderManager.HasUniform(shaderName, "lightColor"))
            {
                if (_lights.TryGetValue(lightName, out var light) && light.IsEnabled)
                {
                    var lightColor = light.Colour * light.Intensity;
                    shaderManager.SetUniform(shaderName, "lightColor", lightColor);
                }
                else
                {
                    shaderManager.SetUniform(shaderName, "lightColor", Vector3.One);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error applying light cube shader {ShaderName} for light {LightName}: {Error}", shaderName, lightName, ex.Message);
        }
    }

    private void ApplySingleLight(string shaderName, RLLight light, ShaderManager shaderManager)
    {
        // Use the light's color and intensity for ambient/diffuse/specular
        if (shaderManager.HasUniform(shaderName, "light.ambient"))
            shaderManager.SetUniform(shaderName, "light.ambient", light.Colour * 0.3f * light.Intensity);

        if (shaderManager.HasUniform(shaderName, "light.diffuse"))
            shaderManager.SetUniform(shaderName, "light.diffuse", light.Colour * 0.7f * light.Intensity);

        if (shaderManager.HasUniform(shaderName, "light.specular"))
            shaderManager.SetUniform(shaderName, "light.specular", light.Colour * 1.0f * light.Intensity);

        switch (light.Type)
        {
            case LightType.Directional:
                if (shaderManager.HasUniform(shaderName, "light.direction"))
                    shaderManager.SetUniform(shaderName, "light.direction", -light.Direction);
                shaderManager.SetUniform(shaderName, "light.type", (int)LightType.Directional);
                break;
            case LightType.Point:
                if (shaderManager.HasUniform(shaderName, "light.position"))
                    shaderManager.SetUniform(shaderName, "light.position", light.Position);
                shaderManager.SetUniform(shaderName, "light.constant", light.Attenuation.Constant);
                shaderManager.SetUniform(shaderName, "light.linear", light.Attenuation.Linear);
                shaderManager.SetUniform(shaderName, "light.quadratic", light.Attenuation.Quadratic);

                shaderManager.SetUniform(shaderName, "light.type", (int)LightType.Point);
                break;
        }
    }

    public IEnumerable<RLLight> GetAllLights() => _lights.Values;
    public IEnumerable<RLLight> GetEnabledLights() => _lights.Values.Where(l => l.IsEnabled);
    public IEnumerable<Entity> GetAllLightCubes() => _lightCubes.Values;
    
    public void DisableAllLights()
    {
        foreach (var light in _lights.Values)
        {
            light.IsEnabled = false;
        }
    }
    
    public void EnableAllLights()
    {
        foreach (var light in _lights.Values)
        {
            light.IsEnabled = true;
        }
    }
}