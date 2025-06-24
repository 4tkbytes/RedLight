# RedLight Game Engine
a game engine made by me (4tkbytes). built with dotnet 8 and Silk.NET. its more like a framework as there is no editor as of right now.  

# current backends
current backends that are supported: 
- OpenGL
more backends to be added (like DirectX and Vulkan) as I add more to it (if i have the time).

# build
currently, there is no nuget package so you will have to add in the RedLight project and add it to your solution (or if there is another
method available). 

you do require some type of python, min py8, recommended: py13/latest. if you
don't have python, redlight guides you through the process anyway.

to build this repository (including the editor), just clone it:
```bash
git clone git@github.com:4tkbytes/RedLight
cd RedLight
```
then you build/restore it. you need a minimum of dotnet 9.0. 
```bash
dotnet run --project ExampleGame --Log=1
```

# action download

for some people like my friends who wanna test this out but do not want to sign into github, you
can download it from this link: [nightly.link](https://nightly.link/4tkbytes/RedLight/workflows/dotnet-desktop.yaml/main?preview)

fyi: this website is open source under [here](https://github.com/oprypin/nightly.link) and is very trustworthy :D

# todo

quick note: this todo is often not up to date as i don't bother updating the README.md
unless a substantial change is found. you are better off running it and testing
out the features for yourself.

- [ ] create a proper readme file + documentation
- [ ] scene creation - Had to change it back because it was broken, will fix later
- [ ] add some UI (not imgui, proper native)
  - [ ] native text rendering
  - [ ] buttons
  - [ ] user interactions
- [ ] fix up imgui (IN PROGRESS)
- [ ] create an editor
- [ ] add networking (if you have the time or are bothered)
- [x] create a way to make a player. (notes: i made it primitive)
- [x] add physics : collisions (primitive)
- [x] add better physics : gravity, other stuff idk || Note: I used Bepu Physics for this
- [x] add lighting (basic) like the sun
- [x] add imguizmo and get it working

# licensing
i dont care what you do as long as you credit me and the engine
