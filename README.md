# RedLight Game Engine
a game engine made by me (4tkbytes). built with dotnet 9.0 and Silk.NET. its more like a framework as there is no editor as of right now.  

# current backends
current backends that are supported: 
- OpenGL
more backends to be added (like DirectX and Vulkan) as I add more to it (if i have the time).

# build
currently, there is no nuget package so you will have to add in the RedLight project and add it to your solution (or if there is another
method available). 

to build this repository (including the editor), just clone it:
```bash
git clone git@github.com:4tkbytes/RedLight
cd RedLight
```
then you build/restore it. you need a minimum of dotnet 9.0. 
```bash
dotnet run --project ExampleGame --Log=1
```

# todo
- [ ] create a proper readme file + documentation
- [ ] scene creation - Had to change it back because it was broken, will fix later
- [ ] add some UI (not imgui, proper native)
  - [ ] native text rendering
  - [ ] buttons
  - [ ] user interactions
- [ ] fix up imgui
- [ ] add imguizmo and get it working (somehow)
- [ ] add networking (if you have the time or are bothered)
- [x] create a way to make a player. (notes: i made it primitive)
- [x] add physics : collisions (primitive)
- [x] add better physics : gravity, other stuff idk || Note: I used Bepu Physics for this
- [x] add lighting (basic) like the sun


# licensing
i dont care what you do as long as you credit me and the engine
