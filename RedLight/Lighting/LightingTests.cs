using System.Numerics;
using RedLight.Entities;
using RedLight.Lighting;
using Serilog;

namespace ExampleGame;

public static class LightingTests
{
    public static void TestDirectionalOnly()
    {
        Log.Debug("=== TESTING DIRECTIONAL LIGHT ONLY ===");
        LightManager.Instance.Clear();

        var sun = new RLLight
        {
            Type = LightType.Directional,
            Direction = new Vector3(-0.3f, -1.0f, -0.2f), // Coming from top-left
            Colour = new Vector3(1.2f, 1.1f, 0.9f), // Warm sunlight
            Intensity = 1.5f
        };
        LightManager.Instance.Add(sun);

        Log.Debug("Created warm directional sun light");
    }

    public static void TestPointLightOnly(Player player)
    {
        Log.Debug("=== TESTING POINT LIGHT ONLY ===");
        LightManager.Instance.Clear();

        var lamp = new RLLight
        {
            Type = LightType.Point,
            Position = player.Position + new Vector3(0, 3.0f, 0), // Above player
            Colour = new Vector3(1.5f, 1.2f, 0.8f), // Warm orange lamp
            Intensity = 2.0f,
            Constant = 1.0f,
            Linear = 0.07f,
            Quadratic = 0.017f,
            Range = 20.0f
        };
        LightManager.Instance.Add(lamp);

        Log.Debug("Created warm point light above player");
    }

    public static void TestColoredLights()
    {
        Log.Debug("=== TESTING COLORED LIGHTS ===");
        LightManager.Instance.Clear();

        // Blue directional light (like moonlight)
        var moon = new RLLight
        {
            Type = LightType.Directional,
            Direction = new Vector3(0.2f, -1.0f, 0.3f),
            Colour = new Vector3(0.3f, 0.4f, 1.2f), // Blue moonlight
            Intensity = 0.8f
        };
        LightManager.Instance.Add(moon);

        // Red point light (like a campfire)
        var fire = new RLLight
        {
            Type = LightType.Point,
            Position = new Vector3(5f, 1f, 5f), // Fixed position
            Colour = new Vector3(2.0f, 0.5f, 0.2f), // Red-orange fire
            Intensity = 1.8f,
            Constant = 1.0f,
            Linear = 0.09f,
            Quadratic = 0.032f,
            Range = 15.0f
        };
        LightManager.Instance.Add(fire);

        Log.Debug("Created blue moonlight + red campfire");
    }

    public static void TestDynamicFollowLight(RLLight playerLamp, Player player)
    {
        Log.Debug("=== TESTING DYNAMIC FOLLOW LIGHT ===");
        LightManager.Instance.Clear();

        // Dim ambient directional light
        var ambient = new RLLight
        {
            Type = LightType.Directional,
            Direction = new Vector3(0f, -1.0f, 0f),
            Colour = new Vector3(0.4f, 0.4f, 0.5f), // Dim blue-gray
            Intensity = 0.5f
        };
        LightManager.Instance.Add(ambient);

        // Bright following lamp (this will be updated in OnUpdate)
        playerLamp = new RLLight
        {
            Type = LightType.Point,
            Position = player.Position + new Vector3(0, 2.0f, 0),
            Colour = new Vector3(1.8f, 1.6f, 1.2f), // Bright warm white
            Intensity = 2.5f,
            Constant = 1.0f,
            Linear = 0.045f,
            Quadratic = 0.0075f,
            Range = 25.0f
        };
        LightManager.Instance.Add(playerLamp);

        Log.Debug("Created dim ambient + bright following lamp");
    }

    public static void TestMultiplePointLights()
    {
        Log.Debug("=== TESTING MULTIPLE POINT LIGHTS ===");
        LightManager.Instance.Clear();

        // Create several colored point lights around the scene
        var positions = new[]
        {
            new Vector3(-10f, 2f, -10f),
            new Vector3(10f, 2f, -10f),
            new Vector3(-10f, 2f, 10f),
            new Vector3(10f, 2f, 10f)
        };

        var colors = new[]
        {
            new Vector3(1.5f, 0.2f, 0.2f), // Red
            new Vector3(0.2f, 1.5f, 0.2f), // Green
            new Vector3(0.2f, 0.2f, 1.5f), // Blue
            new Vector3(1.5f, 1.5f, 0.2f)  // Yellow
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var light = new RLLight
            {
                Type = LightType.Point,
                Position = positions[i],
                Colour = colors[i],
                Intensity = 1.2f,
                Constant = 1.0f,
                Linear = 0.07f,
                Quadratic = 0.017f,
                Range = 18.0f
            };
            LightManager.Instance.Add(light);
        }

        Log.Debug("Created 4 colored corner lights: Red, Green, Blue, Yellow");
    }

    public static void TestRealisticDaylight()
    {
        Log.Debug("=== TESTING REALISTIC DAYLIGHT ===");
        LightManager.Instance.Clear();

        // Realistic sun
        var sun = new RLLight
        {
            Type = LightType.Directional,
            Direction = new Vector3(-0.2f, -0.8f, -0.3f), // Afternoon sun angle
            Colour = new Vector3(1.0f, 0.95f, 0.8f), // Slightly warm white
            Intensity = 1.2f
        };
        LightManager.Instance.Add(sun);

        Log.Debug("Created realistic afternoon sunlight");
    }
}