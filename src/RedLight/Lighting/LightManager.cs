using System.Numerics;
using RedLight.Entities;
using Serilog;
using RedLight.Graphics;

namespace RedLight.Lighting;

public class LightManager
{
    private readonly Dictionary<string, RLLight> _lights;
    private readonly Dictionary<string, Entity> _lightCubes;
    private readonly Dictionary<string, LightingCube> _lightingCubes;

    private Fog _fog;

    public Fog Fog
    {
        get => _fog;
        set => _fog = value;
    }

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
        _lights = new();
        _lightCubes = new();
        _lightingCubes = new();
    }

    public void AddLightingCube(string lightName, LightingCube lightingCube)
    {
        _lightingCubes.Add(lightName, lightingCube);
        Log.Verbose("Added lighting cube for light: {LightName}", lightName);
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

    public void ApplyLightsToShader(Vector3 viewPosition, Entity entity = null, string shaderName = "lit")
    {
        var shaderManager = ShaderManager.Instance;

        try
        {
            // Set view position
            if (shaderManager.HasUniform(shaderName, "viewPos"))
                shaderManager.SetUniform(shaderName, "viewPos", viewPosition);

            // Set material properties
            if (shaderManager.HasUniform(shaderName, "material.shininess"))
                shaderManager.SetUniform(shaderName, "material.shininess", _shininess);

            if (shaderManager.HasUniform(shaderName, "material.diffuse"))
                shaderManager.SetUniform(shaderName, "material.diffuse", 0); // Texture unit 0

            if (shaderManager.HasUniform(shaderName, "material.specular"))
                shaderManager.SetUniform(shaderName, "material.specular", 1); // Texture unit 1

            if (entity != null)
            {
                if (shaderManager.HasUniform(shaderName, "material.reflectivity"))
                    shaderManager.SetUniform(shaderName, "material.reflectivity", entity.Reflectivity);

                if (shaderManager.HasUniform(shaderName, "enableReflection"))
                    shaderManager.SetUniform(shaderName, "enableReflection", entity.EnableReflection);

                if (shaderManager.HasUniform(shaderName, "material.refractiveIndex"))
                    shaderManager.SetUniform(shaderName, "material.refractiveIndex", entity.RefractiveIndex);

                if (shaderManager.HasUniform(shaderName, "enableRefraction"))
                    shaderManager.SetUniform(shaderName, "enableRefraction", entity.EnableRefraction);
            }
            else
            {
                if (shaderManager.HasUniform(shaderName, "material.reflectivity"))
                    shaderManager.SetUniform(shaderName, "material.reflectivity", 0.0f);

                if (shaderManager.HasUniform(shaderName, "enableReflection"))
                    shaderManager.SetUniform(shaderName, "enableReflection", false);
            }

            // Set skybox texture unit
            if (shaderManager.HasUniform(shaderName, "skybox"))
                shaderManager.SetUniform(shaderName, "skybox", 2);


            var enabledLights = _lights.Values.Where(l => l.IsEnabled).ToList();

            // Apply directional light
            var dirLight = enabledLights.FirstOrDefault(l => l.Type == LightType.Directional);
            ApplyDirectionalLight(shaderName, dirLight, shaderManager);

            // Apply point lights (up to 4)
            var pointLights = enabledLights.Where(l => l.Type == LightType.Point).Take(4).ToList();
            ApplyPointLights(shaderName, pointLights, shaderManager);

            // Apply spotlight
            var spotLight = enabledLights.FirstOrDefault(l => l.Type == LightType.Spot);
            ApplySpotLight(shaderName, spotLight, shaderManager);

            // THE FOG IS COMING THE FOG IS COMING
            ApplyFogToShader(shaderName, shaderManager);
        }
        catch (Exception ex)
        {
            Log.Error("Error applying lights to shader {ShaderName}: {Error}", shaderName, ex.Message);
        }
    }

    private void ApplyDirectionalLight(string shaderName, RLLight? light, ShaderManager shaderManager)
    {
        string prefix = "dirLight";

        if (light != null)
        {
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.direction", light.Direction ?? Vector3.UnitY);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", light.Colour * 0.2f * light.Intensity);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", light.Colour * 0.5f * light.Intensity);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", light.Colour * 1.0f * light.Intensity);
        }
        else
        {
            // Set default/disabled values
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.direction", Vector3.UnitY);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", Vector3.Zero);
        }
    }

    private void ApplyPointLights(string shaderName, List<RLLight> lights, ShaderManager shaderManager)
    {
        for (int i = 0; i < 4; i++) // NR_POINT_LIGHTS = 4
        {
            string prefix = $"pointLights[{i}]";

            if (i < lights.Count)
            {
                var light = lights[i];
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.position", light.Position ?? Vector3.Zero);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", light.Colour * 0.2f * light.Intensity);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", light.Colour * 0.5f * light.Intensity);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", light.Colour * 1.0f * light.Intensity);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.constant", light.Attenuation.Constant);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.linear", light.Attenuation.Linear);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.quadratic", light.Attenuation.Quadratic);
            }
            else
            {
                // Set default/disabled values for unused slots
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.position", Vector3.Zero);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", Vector3.Zero);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", Vector3.Zero);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", Vector3.Zero);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.constant", 1.0f);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.linear", 0.0f);
                shaderManager.SetUniformIfExists(shaderName, $"{prefix}.quadratic", 0.0f);
            }
        }
    }

    private void ApplySpotLight(string shaderName, RLLight? light, ShaderManager shaderManager)
    {
        string prefix = "spotLight";

        if (light != null)
        {
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.position", light.Position ?? Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.direction", light.Direction ?? Vector3.UnitY);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", light.Colour * 0.2f * light.Intensity);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", light.Colour * 0.5f * light.Intensity);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", light.Colour * 1.0f * light.Intensity);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.constant", light.Attenuation.Constant);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.linear", light.Attenuation.Linear);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.quadratic", light.Attenuation.Quadratic);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.cutOff", MathF.Cos(MathF.PI * light.InnerCutoff / 180.0f));
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.outerCutOff", MathF.Cos(MathF.PI * light.OuterCutoff / 180.0f));
        }
        else
        {
            // Set default/disabled values
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.position", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.direction", Vector3.UnitY);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.ambient", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.diffuse", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.specular", Vector3.Zero);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.constant", 1.0f);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.linear", 0.0f);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.quadratic", 0.0f);
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.cutOff", MathF.Cos(MathF.PI * 12.5f / 180.0f));
            shaderManager.SetUniformIfExists(shaderName, $"{prefix}.outerCutOff", MathF.Cos(MathF.PI * 17.5f / 180.0f));
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

    public void RenderAllLightCubes(Camera camera)
    {
        foreach (var kvp in _lightingCubes)
        {
            var lightName = kvp.Key;
            var lightingCube = kvp.Value;

            // Only render if the light exists and is enabled
            if (_lights.TryGetValue(lightName, out var light) && light.IsEnabled)
            {
                lightingCube.Render(camera);
            }
        }
    }

    public void UpdateAllLightCubes(Camera playerCamera = null)
    {
        foreach (var kvp in _lightingCubes)
        {
            var lightName = kvp.Key;
            var lightingCube = kvp.Value;

            if (_lights.TryGetValue(lightName, out var light))
            {
                // For spotlight following player camera
                if (light.Type == LightType.Spot && playerCamera != null)
                {
                    lightingCube.Update(playerCamera);
                }
                else
                {
                    lightingCube.Update();
                }
            }
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
            case LightType.Spot:
                shaderManager.SetUniformIfExists(shaderName, "light.position", light.Position);
                shaderManager.SetUniformIfExists(shaderName, "light.direction", light.Direction);
                shaderManager.SetUniform(shaderName, "light.constant", light.Attenuation.Constant);
                shaderManager.SetUniform(shaderName, "light.linear", light.Attenuation.Linear);
                shaderManager.SetUniform(shaderName, "light.quadratic", light.Attenuation.Quadratic);
                shaderManager.SetUniformIfExists(shaderName, "light.cutOff", float.Cos(float.DegreesToRadians(light.InnerCutoff)));
                shaderManager.SetUniformIfExists(shaderName, "light.outerCutOff", float.Cos(float.DegreesToRadians(light.OuterCutoff)));
                shaderManager.SetUniform(shaderName, "light.type", (int)LightType.Spot);
                break;
        }
    }

    public IEnumerable<RLLight> GetAllLights() => _lights.Values;
    public IEnumerable<RLLight> GetEnabledLights() => _lights.Values.Where(l => l.IsEnabled);
    public IEnumerable<Entity> GetAllLightCubes() => _lightCubes.Values;
    public IEnumerable<LightingCube> GetAllLightingCubes() => _lightingCubes.Values;


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

    // Set visibility for all light cubes
    public void SetAllLightCubesVisible(bool visible)
    {
        foreach (var lightingCube in _lightingCubes.Values)
        {
            lightingCube.SetVisible(visible);
        }
    }

    // Set visibility for specific light cube
    public void SetLightCubeVisible(string lightName, bool visible)
    {
        if (_lightingCubes.TryGetValue(lightName, out var lightingCube))
        {
            lightingCube.SetVisible(visible);
        }
    }

    // Get specific lighting cube
    public LightingCube? GetLightingCube(string lightName)
    {
        return _lightingCubes.TryGetValue(lightName, out var lightingCube) ? lightingCube : null;
    }

    // Remove light and its cube
    public void RemoveLight(string name)
    {
        if (_lights.Remove(name))
        {
            _lightCubes.Remove(name);
            _lightingCubes.Remove(name); // Also remove lighting cube
            Log.Verbose("Removed light: {Name}", name);
        }
    }

    private void ApplyFogToShader(string shaderName, ShaderManager shaderManager)
    {
        if (_fog != null)
        {
            shaderManager.SetUniformIfExists(shaderName, "fog.density", _fog.Density);
            shaderManager.SetUniformIfExists(shaderName, "fog.color", _fog.Color);
            shaderManager.SetUniformIfExists(shaderName, "fog.start", _fog.Start);
            shaderManager.SetUniformIfExists(shaderName, "fog.end", _fog.End);
            shaderManager.SetUniformIfExists(shaderName, "fog.type", (int)_fog.Type);
        }
        else
        {
            // No fog - set density to 0
            shaderManager.SetUniformIfExists(shaderName, "fog.density", 0.0f);
            shaderManager.SetUniformIfExists(shaderName, "fog.color", Vector3.One);
            shaderManager.SetUniformIfExists(shaderName, "fog.start", 1.0f);
            shaderManager.SetUniformIfExists(shaderName, "fog.end", 100.0f);
            shaderManager.SetUniformIfExists(shaderName, "fog.type", 0);
        }
    }

    // Helper methods for setting fog
    public void SetLinearFog(Vector3 color, float start, float end)
    {
        _fog = Fog.CreateLinearFog(color, start, end);
    }

    public void SetExponentialFog(Vector3 color, float density)
    {
        _fog = Fog.CreateExponentialFog(color, density);
    }

    public void SetFog(Fog fog)
    {
        _fog = fog;
        Log.Debug("Fog updated: Density={Density}, Type={Type}", fog.Density, fog.Type);
    }

    public void EnableFog(Vector3 color, float density = 0.1f, FogType type = FogType.Linear)
    {
        if (_fog == null)
        {
            _fog = new Fog();
        }

        _fog.Color = color;
        _fog.Density = density;
        _fog.Type = type;

        Log.Debug("Fog enabled with density: {Density}", density);
    }

    public void DisableFog()
    {
        if (_fog != null)
        {
            _fog.Disable();
            Log.Debug("Fog disabled");
        }
    }

    public void SetFogDensity(float density)
    {
        if (_fog != null)
        {
            _fog.Density = density;
        }
    }
}