# What is static terrain?
A support script which allows **YOU** the player to easily create (fake) terrains as well as interiors for a little known game named Blockland.

The idea was to bring back what cannot be simply built of out bricks.
These have ideal collision (tsstatics work better than bricks at stopping cars + they're scale 0.5 to ensure perfection) for driving and skiing.

<img src="https://user-images.githubusercontent.com/27306442/167024874-cc7357a8-cb59-4a39-9be6-d52ec56397db.png" width="800" height="500">
<img src="https://user-images.githubusercontent.com/27306442/167025040-0a828350-16c5-4779-9ebb-7bb3f8a53380.png" width="800" height="500">
<img src="https://user-images.githubusercontent.com/27306442/167024953-0f497a63-92b7-4570-89b4-7d761c3071b1.png" width="800" height="500">
<img src="https://user-images.githubusercontent.com/27306442/167025895-257ffa27-2e52-4d18-9fb7-a405e619ad00.png" width="800" height="500">
(all terrains shown are for testing, if these look ugly to you then worry not, the final ones will look much better :P)

# What is the state of this project?
Currently the project is in a funny place as we're limited by the engine and its collsion methods (convex meshes only, benchmarks have shown more than 2000 collisions start to lag both the server and client terribly) and sadly no default performance optimizations exist.
### tldr: we're in need of a fancy dll that'd make a breakthrough in performance otherwise we're stuck with small scale low detail terrains
If you're interested in knowing more about what we **can** and **cannot** do I recommend reading the wiki.

# How do you use this?
Download the folder from the github (remember to remove the -main and keep it as a folder)

<img src="https://user-images.githubusercontent.com/27306442/167012941-d190c855-5b15-4a6d-ad05-4f580bb0b1ce.png" width="350" height="350">

Enable the addon ingame and once loaded, use these commands to spawn/manipulate the terrain

<img src="https://user-images.githubusercontent.com/27306442/167013178-5a4540e6-92ef-409c-8140-7f0259cd811e.png" width="400" height="350">

(reference is anything you want except duplicates, fileName is the terrainGroup name aka the .dts model name)
For example, to create the Test3 terrain you'd do /maketerrain Test3 Test3

<img src="https://user-images.githubusercontent.com/27306442/167013480-c418ad1c-15c6-4c80-8f96-d4ce035b3299.png" width="800" height="500">

Using the commands listed above you can rotate (sadly breaks non-straight collisions), reskin, resize (also breaks collisions :P), move, bring terrain to player, even color it (works only on terrains that support reskins - /skinterrain NAME blank and then /colorterrain NAME X X X X)
(if you have many terrains and intend on moving or removing them at once you can input ALL into a terrain command instead of a single reference)

# Currently available terrains in the mod
* background (30x30 resolution, supports reskins)
* cube (800 cube collisions, used for testing, DONT USE)
* cube8 (100 x 8 cube collisions, used for testing, DONT USE)
* test2 (first island, 30x30 resolution, somehow broke collision accuracy throughout dev, supports reskins)
* test3 (second island made by meister, 35x35 resolution)
* tent (port from v20)
* milbase (port from v20)
* convextest (15x15 resolution, loopable, supports reskins)

# Credits
* aebaadcode (project organization + porting v20 assets/modeling awesome terrain + extensive debugging of loving terrain)
* Monoblaster (datablock generator + entire command interface)
* Conan (blender bulk collision export script + code help)
* Buddy (additional optimizations)
* Oxy (debugging and additions to command framework)
* Der_Meister (making the test3 terrain)
* Queuenard (debugging and big help with research)
* Tendon (discovering the effectiveness of tsstatics)
* Port (extra resource support)
