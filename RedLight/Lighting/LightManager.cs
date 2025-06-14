using System.Numerics;
using RedLight.Graphics;
using Serilog;

namespace RedLight.Lighting;

public class LightManager
{
    private static LightManager _instance;
    public static LightManager Instance => _instance ?? (_instance = new LightManager());
    
    private List<RLLight> _lights = new();

    public void Add(RLLight light)
    {
        _lights.Add(light);
    }

    public void Remove(RLLight light)
    {
        _lights.Remove(light);
    }
    
    public List<RLLight> GetLights() => _lights;

    public RLLight GetDirectionalLight()
    {
        return _lights.FirstOrDefault(l => l.Type == LightType.Directional);
    }

    public List<RLLight> GetPointLights()
    {
        return _lights.Where(l => l.Type == LightType.Point).ToList();
    }

    public void Clear()
    {
        _lights.Clear();
    }

    // Apply lighting uniforms to a shader program - FIXED TO MATCH YOUR SHADER
    public void ApplyLighting(RLShaderProgram program, Vector3 viewPosition)
    {
        if (program == null)
        {
            Log.Warning("Cannot apply lighting - shader program is null");
            return;
        }

        var directionalLight = GetDirectionalLight();
        var pointLights = GetPointLights();
        var firstPointLight = pointLights.FirstOrDefault();

        try
        {
            // Ambient lighting
            program.SetUniform("ambientColor", new Vector3(0.3f, 0.3f, 0.4f));
            program.SetUniform("ambientStrength", 0.4f);
            program.SetUniform("viewPos", viewPosition);

            // Directional light (sun) - CORRECTED UNIFORM NAMES TO MATCH lit.frag
            if (directionalLight != null)
            {
                program.SetUniform("directionalLight_direction", directionalLight.Direction);
                program.SetUniform("directionalLight_color", directionalLight.Colour);
                program.SetUniform("directionalLight_intensity", directionalLight.Intensity);
                Log.Verbose("Applied directional light: Direction={Direction}, Color={Color}, Intensity={Intensity}", 
                    directionalLight.Direction, directionalLight.Colour, directionalLight.Intensity);
            }
            else
            {
                program.SetUniform("directionalLight_intensity", 0.0f);
            }

            // Point light (lamp) - NAMES MATCH YOUR SHADER
            if (firstPointLight != null)
            {
                program.SetUniform("pointLight_position", firstPointLight.Position);
                program.SetUniform("pointLight_color", firstPointLight.Colour);
                program.SetUniform("pointLight_intensity", firstPointLight.Intensity);
                program.SetUniform("pointLight_constant", firstPointLight.Constant);
                program.SetUniform("pointLight_linear", firstPointLight.Linear);
                program.SetUniform("pointLight_quadratic", firstPointLight.Quadratic);
                Log.Verbose("Applied point light: Position={Position}, Color={Color}, Intensity={Intensity}", 
                    firstPointLight.Position, firstPointLight.Colour, firstPointLight.Intensity);
            }
            else
            {
                program.SetUniform("pointLight_intensity", 0.0f);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error applying lighting uniforms: {Error}", ex.Message);
        }
    }
}