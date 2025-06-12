# RedLight Game Engine
a game engine made by me (4tkbytes). contains imgui support, scene creation and built with dotnet 9.0 and Silk.NET. 

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

# imgui commands
there are commands you can use on the editor console that will make editing a lot more easier:

# todo
- [ ] create a proper readme file
- [ ] scene creation - Had to change it back because it was broken, will fix later
- [x] create a way to make a player. (notes: i made it primitive)
- [ ] add imguizmo and get it working (somehow)
- [ ] add lighting (basic) like the sun
- [x] add physics : collisions (primitive)
- [ ] add better physics : gravity, other stuff idk
- [ ] add networking (if you have the time or are bothered)

# licensing
i dont care what you do as long as you credit me and the engine (and perhaps a lil bit of that moolah if you make some??? no pressure)
