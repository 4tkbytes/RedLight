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
}