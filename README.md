# HELLIONHarmony

A [Harmony](https://github.com/pardeike/Harmony) injector for late game [HELLION](https://store.steampowered.com/app/588210/HELLION/) to allow for modular plugins to patch the game as they please. This can patch the Client, Singleplayer server and dedicated server indepdently if required. 

**Unfortunately, this is not yet functional or fully tested** but feel free to contribute if you can!

## How do I patch my copy of HELLION?

It should be as easy, make sure the game isn't running, start `HELLIONHarmony.Injector.exe` and it should auto detect your HELLION installation, make a backup and patch it. 

To unpatch your game, reference the same assembly and add the -RB switch to the program command line to Restore Backup or simply overwrite the main files with the `<filename.ext>.bak` files. 

If you require further help, use the `-?` switch. 

## Where do I put the plugin I downloaded?
In most cases, it will be in the same location as the executable. HELLION clients and HELLION_SP plugins will reside in the same folder - `steamapps/common/HELLION/HELLIONHarmonyPlugins` - the same place as `HELLION.exe`. 

---

## What does each part of the solution do?

### HELLIONHarmony
A class that contains the Plugin. You will need to reference this when you create your own plugins. 

###  HELLIONHarmony.Injector
This is the program that patches the assembly. It has some switches to control the way the game is patched. 

This also contains some helper functions to patch functions using Mono.Cecil.CIL. The game is patched differently based on the version. `ZeroGravity.Client` or `ZeroGravity.Server` gets the static bool `Modded` added. 

Each patch is surrounded by a `ldstr [startIdentifier], pop, [patchedCode], ldstr [endId], pop` so that future patches can modify the patch in future versions or remove it without the user needing to restore from a backup. In C# decompilations these don't show up. When the patch returns early or skips over code, this is very likely to break decompilations. 

### HELLIONHarmony.Loader
This contains the PluginManager which will load files from the plugin directory on game start, load them on game joining and disables them on game leaving\*. Menu plugins are always enabled, started after the plugins have loaded. 

\* Not done yet

### HELLIONHarmony.TestPlugin
This is a test menu plugin to try and change the discord presence. You may use it as a template as it is likely the most up to date 'documentation' on how to make a plugin. 

## How do I make my own plugin?

You will need to create a .NET Class Library targetting .NET Framework 4.7.2. Add references to the (unmodified) HELLION assemblies. 

Create your Plugin derivative class and give it a unique identifier, this is how Harmony handles the patches. Then create your other classes in the same project, giving the classes the `HELLIONPatch` attribute and the patch scope for it to apply to. Alternatively you can give the plugin the `PatchScope OnlyScope` property and use the `HarmonyPatch` attribute. 

Build the plugin and put it in your plugin directory. Debug the game if you want debug information. 

## How do I build this repo?

HELLIONHarmony.Injector targets .NET 6.0 so you may need Visual Studio 2022 or higher. To be easily compatible with HELLION, the other projects target .NET Framework 4.7.2. 

Once you have referenced the (unmodified) assemblies, you will need to assign them aliases in HELLIONHarmony.Loader:

|Assembly|Alias|
|---|---|
| 0Harmony | HarmonyLib |
| Assembly-CSharp | HELLIONClient |
| HELLION_Dedicated | HELLIONDedicated |
| HELLION_SP | HELLIONSP |

As the Injector/Patcher targets a higher .NET version, ensure the Injector isn't Copying Local for any references such as Harmony as the Loader project will not be able to load the correct DLLs. 

When debugging, I recommend adding `-logFile -` to the command line and if using dnSpy, using a dnSpy-patched `mono-2.0-bdwgc.dll`. The game uses a Bleeding Edge Unity-2018.2.10 version of Mono. 