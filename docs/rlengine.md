# How to use RLEngine
Hi there. This is the starting point to using the RedLight Game Engine. In this 
page, I will show you how to use the engine to create a simple game as well as some 
other basic stuff. 

To start off, this library is to be primarily used as a framework/library, not a fully
fledged out game engine. I am in the works for a GUI however it can only take me so far
with the knowledge I have. 

Now, onto the juicy parts
# Initialising RedLight

In your Program.cs:
```csharp
static void Main(string[] args)
{
    // Initialise scenes
    var scene1 = new TestingScene1();

    // Create engine instance
    var engine = new RLEngine(1280, 720, "RedLight Game Engine Editor", scene1, args);

    // add scenes to scene manager
    SceneManager.Instance.Add("testing_scene_1", scene1, scene1, scene1);

    // run
    engine.Run();
}
```

## Explanation
The first line is complicated, as you do not have enough context yet. I will explain it
later during the intro but for what you need to know: That is a scene. 

The second line creates an instance of an engine. According to the documentation, RLEngine
takes in a width, a height, a title, a starting scene and the arguments.

The third line adds the scene to a global SceneManager to make it easier to switch between scenes.
The three 'scene1' arguments as used as a **scene**, **keyboard** and **mouse**

The last line runs the app. Just let it rip. 

# Scenes

Scenes are a very important tool used in the RedLight engine.

This is a scene:
```csharp
public class TestingScene1 : RLScene, RLKeyboard, RLMouse
{
    // scene shenanigans
    public override RLEngine Engine { get; set; }
    public override RLGraphics Graphics { get; set; }
    public override SceneManager SceneManager { get; set; }
    public override ShaderManager ShaderManager { get; set; }
    public override TextureManager TextureManager { get; set; }
    public override InputManager InputManager { get; set; }
    public override PhysicsSystem PhysicsSystem { get; set; }
    public override LightManager LightManager { get; set; }
    public override TextManager TextManager { get; set; }
    
    public override void OnLoad()
    {
        throw new NotImplementedException();
    }

    public override void OnUpdate(double deltaTime)
    {
        throw new NotImplementedException();
    }

    // input code starts here
    public HashSet<Key> PressedKeys { get; set; }
    
    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        throw new NotImplementedException();
    }
}
```

As always, there are chances it may not be accurate, so do not depend on it. Instead, use your
IDE of choice to help you inherit the different values. 

This is your main focus:
```csharp
public class TestingScene1 : RLScene, RLKeyboard, RLMouse
```

`RLScene` is an abstract class for initialising scenes and storing its required values (under `RedLight.Scene`),
`RLMouse` and `RLKeyboard` are both input types. The reason on why they are separate is because some scenes
may want to use the same keyboard and mouse controls for other scenes. Makes life easier :)

Under `RLScene`, there are 2 mandatory functions:
```csharp
public void OnLoad()
{
    throw new NotImplementedException();
}
```
and
```csharp
public void OnUpdate(double deltaTime)
{
    throw new NotImplementedException();
}
```

As the name suggests, `OnLoad()` initialises the different objects and places them on the world. 
This function only runs once in the scene, however can be run again if for some reason you wish to 
recreate the scene again. 

The principles apply the same in `OnUpdate()`; the difference being that OnUpdate updates every frame (or roughly 60fps depending on your computer specs). 
`OnUpdate()` also has a parameter (which is required as per the interface), which allows you to update every frame. 
It is unfortunately a double due to Silk.NET's requirements however it shouldn't be that hard to cast to a float. 

Other input methods will be added in separately as more pages for docs. The variables at the top 
have documentation at their respective Docs page. 