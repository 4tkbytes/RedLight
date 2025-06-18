using System.Numerics;
using RedLight.Entities;
using Serilog;
using RedLight.Graphics;

namespace RedLight.Lighting;

public class LightManager
{
    private readonly Dictionary<string, RLLight> _lights;
    private readonly Dictionary<string, Entity> _lightCubes;
    private float _ambientStrength = 0.1f;
    private float _specularStrength = 0.5f;
    private int _shininess = 32;

    public float AmbientStrength
    {
        get => _ambientStrength;
        set => _ambientStrength = value;
    }

    public float SpecularStrength
    {
        get => _specularStrength;
        set => _specularStrength = value;
    }

    public int Shininess
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

    public void UpdateLightPosition(string name, Vector3 position)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.Position = position;
        }
    }

    public void UpdateLightColor(string name, Vector3 color)
    {
        if (_lights.TryGetValue(name, out var light))
        {
            light.Color = color;
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
    
    public void ApplyLightsToShader(string shaderName, Vector3 viewPosition)
    {
        var shaderManager = ShaderManager.Instance;
        
        try
        {
            // Only set uniforms that exist in the lit shader
            if (shaderManager.HasUniform(shaderName, "ambientStrength"))
                shaderManager.SetUniform(shaderName, "ambientStrength", _ambientStrength);
            
            if (shaderManager.HasUniform(shaderName, "specularStrength"))
                shaderManager.SetUniform(shaderName, "specularStrength", _specularStrength);
            
            if (shaderManager.HasUniform(shaderName, "shininess"))
                shaderManager.SetUniform(shaderName, "shininess", _shininess);
            
            if (shaderManager.HasUniform(shaderName, "viewPos"))
                shaderManager.SetUniform(shaderName, "viewPos", viewPosition);

            var enabledLights = _lights.Values.Where(l => l.IsEnabled).ToList();
            
            var primaryLight = enabledLights.FirstOrDefault();
            if (primaryLight != null)
            {
                ApplySingleLight(shaderName, primaryLight, shaderManager);
            }
            else
            {
                if (shaderManager.HasUniform(shaderName, "lightColor"))
                    shaderManager.SetUniform(shaderName, "lightColor", Vector3.Zero);
                
                if (shaderManager.HasUniform(shaderName, "lightPos"))
                    shaderManager.SetUniform(shaderName, "lightPos", Vector3.Zero);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error applying lights to shader {ShaderName}: {Error}", shaderName, ex.Message);
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
                    var lightColor = light.Color * light.Intensity;
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
        var finalColor = light.Color * light.Intensity;
        
        switch (light.Type)
        {
            case LightType.Directional:
                if (shaderManager.HasUniform(shaderName, "lightDir"))
                    shaderManager.SetUniform(shaderName, "lightDir", light.Direction);
                if (shaderManager.HasUniform(shaderName, "lightColor"))
                    shaderManager.SetUniform(shaderName, "lightColor", finalColor);
                break;
                
            case LightType.Point:
                if (shaderManager.HasUniform(shaderName, "lightPos"))
                    shaderManager.SetUniform(shaderName, "lightPos", light.Position);
                if (shaderManager.HasUniform(shaderName, "lightColor"))
                    shaderManager.SetUniform(shaderName, "lightColor", finalColor);
                break;
                
            case LightType.Spot:
                if (shaderManager.HasUniform(shaderName, "lightPos"))
                    shaderManager.SetUniform(shaderName, "lightPos", light.Position);
                if (shaderManager.HasUniform(shaderName, "lightDir"))
                    shaderManager.SetUniform(shaderName, "lightDir", light.Direction);
                if (shaderManager.HasUniform(shaderName, "lightColor"))
                    shaderManager.SetUniform(shaderName, "lightColor", finalColor);
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