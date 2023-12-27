# Memoria - Persona 5 Tactica
This is a small modification that makes gameplay of [Persona 5 Tactica](https://store.steampowered.com/app/2254740/Persona_5_Tactica/) more enjoyable. 

## Support
[Patreon](https://www.patreon.com/Albeoris?fan_landing=true)

## Installation:
- Unpack [BepInEx_UnityMono_x64_6.0.0-pre.1](https://github.com/Albeoris/Memoria.Persona5T/releases/download/v2023.12.26/BepInEx.6.0.0-be.674.-.Persona.5.Tactica.zip) into the game folder.
- Unpack the [mod](https://github.com/Albeoris/Memoria.Persona5T/releases/download/v2023.12.27/Memoria.Persona5T.Steam_v2023.12.27.zip) archive into the game folder.

If you are already using BepInEx to load other mods, use the most recent version of the loader.
(But the current version has been patched; new versions may not be compatible with Persona 5 Tactica)

If you playing on Steam Deck check [this page](https://github.com/Albeoris/Memoria.FFPR/wiki/Steam-Deck).

## Deinstalation:
- To remove the mod - delete $GameFolder$\BepInEx\plugins\Memoria.Persona5T.*.dll
- To remove the mod launcer - delete $GameFolder$\winhttp.dll

## Changes:
- You can increase game speed (Default Key: F1).
- You can export, edit and import game databases (like characteristics of enemies) (Export disabled by default)
- [Partial modification](https://github.com/Albeoris/Memoria.Persona5T/wiki/Features-Mods) of CSV and .json resources

## Configuration:

1. Start the game first.
2. Features that are enabled by default (for example, increasing the speed of the game) will already work.
3. Close the game and edit the configuration file `$GameFolder$\BepInEx\config\Memoria.Persona5T\$Section$.cfg`
   
## Troubleshooting:

- Share mod logs: $GameFolder$\BepInEx\LogOutput.log
- Create an issue.
