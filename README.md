# Chained Chicken Mod - Chained Together meets UCH

![chainchicken_logo-removebg-preview](https://github.com/user-attachments/assets/f1555169-d098-4dfb-9fe4-dd1491331228)


This is an `Ultimate Chicken Horse` `BepInEx` mod that chains the players together.

![ChainedChicken](https://github.com/user-attachments/assets/e2227b94-01fe-4d07-9a04-cb5afb5c4567)


The chains can be activated in the menu under `Rules & Modifiers` => `Modifiers` => `Chain Players` 
(all the way at the bottom of the list)
They only work in a local or [EvenMorePlayers](https://github.com/batram/UCH-EvenMorePlayers) lobby, the mod can't be activated in normal Online lobbies.

## Manual installation
- Download [BepInEx Version 5](https://github.com/BepInEx/BepInEx/releases/latest) for your platform (windows64 or linux) (UCH is a x64 program)
- Download [the latest UCH-ChainedChicken release (ChainedChicken-x.x.x.x.zip)](https://github.com/batram/UCH-ChainedChicken/releases) 
- Put all the contents inside the zip files into your `Ultimate Chicken Horse` folder found via `Steam -> Manage -> Browse Local Files`.
  (Just drag the Bepinex folder from the zip to your game folder.)
Run game! (Linux users need an additional step, follow instructions in BepInEx)

## Help
If you have questions, comments or suggestions join the [UCH Mods discord](https://discord.gg/GgzDQW6zbq)


## Build with dotnet
1. Download the source code of the mod (or use git):
      - https://github.com/batram/UCH-ChainedChicken/archive/refs/heads/main.zip

2. Extract the folder at a location of your choice (the source code should not be in the `BepInEx` plugins folder)

3. Install dotnet (SDK x64):
      - https://dotnet.microsoft.com/en-us/download

4. Make sure you have BepInEx installed:
      - Download [BepInEx](https://github.com/BepInEx/BepInEx/releases) for your platform (UCH is a x64 program)
      - Put all the contents from the `BepInEx_x64` zip file into your `Ultimate Chicken Horse` folder found via `Steam -> Manage -> Browse Local Files`.

5. Click on the `build.bat` file in the source code folder `UCH-ChainedChicken-main` you extracted 

## Config and Issues
1. UCH installation path
      - If Ultimate Chicken Horse is not installed at the default steam location, 
  the correct path to the installation needs to be set in `ChainedChicken.csproj`.
      - You can edit the `ChainedChicken.csproj` file with any Text editor (e.g. notepad, notepad++). 
      - Replace the file path between `<UCHfolder>` and `</UCHfolder>` with your correct Ultimate Chicken Horse game folder.

            <PropertyGroup>
              <UCHfolder>C:\Program Files (x86)\Steam\steamapps\common\Ultimate Chicken Horse\</UCHfolder>
            </PropertyGroup>
      
      - If the path is wrong you see the following errors during the build:

            ...
            warning MSB3245: Could not resolve this reference. Could not locate the assembly "Assembly-CSharp"
            warning MSB3245: Could not resolve this reference. Could not locate the assembly "UnityEngine"
            ...
            error CS0246: The type or namespace name 'UnityEngine' could not be found
            ...

2. Missing BepInEx
      - If the build errors only metion `BepInEx` and `0Harmony`, check that BepInEx is installed in your game folder
      - Example Errors (no other `MSB3245` warnings):

            warning MSB3245: Could not resolve this reference. Could not locate the assembly "BepInEx"
            warning MSB3245: Could not resolve this reference. Could not locate the assembly "0Harmony"
            ...
            error CS0246: The type or namespace name 'BepInEx' could not be found
            ...
              
      - correct folder structure:

            -> Ultimate Chicken Horse
                   -> BepInEx
                        -> core
                              -> 0Harmony.dll
                              -> ...
                   -> UltimateChickenHorse_Data
                   -> doorstop_config.ini
                   -> ...
                   -> UltimateChickenHorse.exe
                   -> ...
                   -> winhttp.dll


## Credits
- [Clever Endeavour Games](https://www.cleverendeavourgames.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx) team
- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike
