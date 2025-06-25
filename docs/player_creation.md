# Creating a player

Adding a player is a core functionality in a game, otherwise who would you be controlling? 
Here is how you make one:

## Model

The first steps in a player is to have a model. Without a model, you 
cannot get a player displayered/represented. 

In this example, I will use maxwell the cat. There is no specific reason
as to why, it was just the first model off the top of my mind

```csharp
var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
    .SetScale(new Vector3(0.05f))
    .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);
```

[`Graphics.CreateModel()`](https://4tkbytes.github.io/RedLight/api/RedLight.Graphics.RLGraphics.html#RedLight_Graphics_RLGraphics_CreateModel_System_String_System_String_) 
function creates a [`Transformable<Model>`](https://4tkbytes.github.io/RedLight/api/RedLight.Graphics.Transformable-1.html).
The Transformable Model allows you to manipulate the position, scale and rotation of the model. 

In this case, I have set the scale to 0.05x the size of its original and rotated it by -90 degrees
on the X axis. This does require a lot of tinkering and trial & error to 
get to your correct spot. 

One of the parameters is the model location. You only need to reference the .gltf file or the
.fbx file, no need to reference everything else.

Related documentation is in here: [RLFiles](https://4tkbytes.github.io/RedLight/api/RedLight.Utils.RLFiles.html)

## Camera

To be able to be put in the perspective of your player, you would attach a camera to the object.
This is done with the [`Camera`](https://4tkbytes.github.io/RedLight/api/RedLight.Graphics.Camera.html) 
class. The camera class does not only work with the Player, you can create as many `Camera` instances as you wish. 

Here is how you make a camera (specifically for the player)

```csharp
var playerCamera = new Camera(Engine.Window.Size);
```

One of the arguments is the Window size, as the camera's viewport is originally set to the window.

## Hitbox

To get the player to be able to jump and collide you need to setup physics, which includes creating a hitbox. 

To set Hitbox up, here is the code: 

```csharp
var playerHitbox = HitboxConfig.ForPlayer();
```

It seems simple, and it is because it is (for that circumstance). It creates a small but working hitbox for the player. 

In the case you want to create a custom player hitbox:

```csharp
var playerHitbox = new HitboxConfig()
{
    Width = 10,
    Height = 10,
    Length = 10,
    GroundOffset = 10,
    CenterOffset = new Vector3(10),
};
```

Despite the values all being 10, they serve different functions. It is recommended that you use
one of the static functions to create a hitbox. 

The documentation/explaination for each
of the different files is located in 
[`HitboxConfig`](https://4tkbytes.github.io/RedLight/api/RedLight.Entities.HitboxConfig.html)

# Converting to Entity
The entity class enables all physics related functions, and is a superset/recommended improvement
over `Transformable<T>`. 

The `Player` class inherits from `Entity`, which includes the best of `Entity` and the use of a `Player`
```csharp
var player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);
```

As you remember, it includes the camera, the player model and the player hitbox that we created. 

There are many different fields, variables, functions, calculations and more that you can edit to make
the player your own. 

For example, you can change the move speed of the player:
```csharp
player.MoveSpeed = 5f;
```

And that is how you create a player!

# Updating and Drawing
But you need to render that, you can't just leave it as it is otherwise it won't draw the model. 

You need to add the player to the infamous "Lists" using the function. This can be added AFTER you initialise
your player. You can always set its values before or after adding to lists. 
```csharp
AddToLists(player);
```

This function (as the name suggests) adds the item passed through into its related list, whether it is lighting, entity, 
scene and more. It is a function that is stored in [`RLScene`](https://4tkbytes.github.io/RedLight/api/RedLight.Scene.RLScene.html#RedLight_Scene_RLScene_AddToLists__1___0_RedLight_UI_ImGui_RLImGuiEditor_)

## Updating
```csharp
public override void OnUpdate(double deltaTime) 
{
    PhysicsSystem.Update((float)deltaTime);
    
    player.Update((float)deltaTime, PressedKeys); // player specific
    // debugCamera.Update((float)deltaTime, PressedKeys);
    // 👆 for any normal camera
}
```

Its pretty simple to update. Due to the player being an `Entity`, it includes physics logic. Therefore, to update
the physics, we must do `PhysicsSystem.Update()`. 

> [!NOTE] 
> As in normal gamedev, deltaTime is the time it took to render the frame. If there is a lot of draw calls, 
> expect the deltaTime to be high. 

> [!TIP]
> To calculate the Frames Per Second (FPS), it is `1000f/deltaTime`

`player.Update()` is also an update function that (as you guessed) updates the players location, camera and almost
everything else that needs to be updated. 

If the camera is not attached to anything, just run `{yourCamera}.Update()`. 

## Rendering
```csharp
public override void OnRender(double deltaTime) 
{
    Graphics.Clear();
    Graphics.ClearColour(Color.CornflowerBlue);
    
    RenderModel(player.Camera);
}
```

Yep that's it. To break it down:

- `Graphics.Clear()` clears the buffers. In layman's terms, it resets the display
- `Graphics.ClearColour(Color.CornflowerBlue)` is applied AFTER `Graphics.Clear()`. It adds in a layer for the 
background, in this case `Color.CornflowerBlue` from the (System.Drawing library). 
- `RenderModel()` renders all models in the ObjectModels list, which includes your player.

## Input
### Mouse
A sneaky yet often missed one, you need to ensure you can move your mouse around and stuff. 

```csharp
public void OnMouseMove(IMouse mouse, Vector2 mousePosition) 
{
    player.Camera.FreeMove(mousePosition);
    // debugCamera.FreeMove(mousePosition);
}
```
This function just makes player.Camera be able to free move. 

There might be issues with exiting, so I would recommend
you to just Alt+Tab or implement a check/boolean that checks if the mouse is captured. 
This can be checked using the boolean [`InputManager.isCaptured`](https://4tkbytes.github.io/RedLight/api/RedLight.Input.InputManager.html#RedLight_Input_InputManager_isCaptured). 

You can change your mouse capture status using this command:
```csharp
InputManager.ChangeCaptureToggle(key, Key.F1);
```
### Keyboard
Keyboard movement is also simple to implement:
```csharp
public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode) 
{
    PressedKeys.Add(key);
}
```
This just adds the key to the PressedKeys hashmap to parse through camera and player updating. 

---
And that's all it takes. Try running it yourself (in this case creating a new Scene)

Full scene code:
```csharp
// Scene that includes player creation. Add this to your Program's SceneManager
public class PlayerCreation : RLScene, RLKeyboard, RLMouse
{
    public override RLEngine Engine { get; set; }
    public override RLGraphics Graphics { get; set; }
    public override SceneManager SceneManager { get; set; }
    public override ShaderManager ShaderManager { get; set; }
    public override TextureManager TextureManager { get; set; }
    public override InputManager InputManager { get; set; }
    public override PhysicsSystem PhysicsSystem { get; set; }
    public override LightManager LightManager { get; set; }
    public override TextManager TextManager { get; set; }

    private Player player;
    public override void OnLoad()
    {
        // This function enables all graphical functions like back buffers. 
        // For beginners: Required/Recommended function, however can be removed
        Graphics.Enable();
        
        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.05f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);
        
        var playerCamera = new Camera(Engine.Window.Size);
        
        var playerHitbox = HitboxConfig.ForPlayer();
        
        player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);
        
        player.MoveSpeed = 5f;

        // This function is recommended to add if you want to see your model. Otherwise,
        // the default shader is "lit"
        player.Model.AttachShader(ShaderManager.Get("basic")); 
        
        AddToLists(player);
    }

    public override void OnUpdate(double deltaTime) 
    {
        PhysicsSystem.Update((float)deltaTime);
    
        player.Update((float)deltaTime, PressedKeys); // player specific
        // debugCamera.Update((float)deltaTime, PressedKeys);
        // 👆 for any normal camera
    }
    
    public override void OnRender(double deltaTime) 
    {
        Graphics.Clear();
        Graphics.ClearColour(Color.CornflowerBlue);
    
        RenderModel(player.Camera);
    }

    public HashSet<Key> PressedKeys { get; set; } = new();
    public void OnMouseMove(IMouse mouse, Vector2 mousePosition) 
    {
        if (InputManager.isCaptured)
        {
            player.Camera.FreeMove(mousePosition);
            // debugCamera.FreeMove(mousePosition);
        }
    }
    
    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode) 
    {
        // PressedKeys.Add(key);
        bool isNewKeyPress = PressedKeys.Add(key); // improved version of original

        if (isNewKeyPress)
        {
            InputManager.ChangeCaptureToggle(key, Key.F1);
        }
    }
}
```

# Last bits
To review over what we looked at/learnt:

- Creating a model and manipulate it
- Creating a camera
- Creating a hitbox
- Converting to a model + camera + hitbox to a player
- Updating and Drawing the player
- Input Management

> [!NOTE]
> All the concepts that have been taught in here can be applied in creating other models such as ducks or characters,
> creating cameras like for filming (as if), creating a complex hitbox (TBC), updating and drawing a model and input management

GLHF!