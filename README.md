# Overview

A collection of mods for Portals of Phereon. Tested with version 0.26.2.0 and 0.26.1.0, but may work on other versions.
  - Interface Tweaks focusing on QoL improvements
    - Sex-Training filters, combat damage preview, and more
  - BugFixes
    - Various bug fixes

## How to install

1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22)
    - Download the correct .zip for your platform (for Windows v0.26.2.0 use BepInEx_x64_5.4.22.0.zip, for v0.26.1.0 use BepInEx_x86_5.4.22.0.zip)
    - Unzip that into the same directory as your Portals of Phereon install. You should end up with a folder named "BepInEx" next to PortalsOfPhereon.exe
    - Run the game once - it will generate default configurations and such
2. [Download](https://github.com/durandal3/PoP-Mods/releases) the .dll for the mods you want.
3. Put the .dll file(s) into your game directory, inside BepInEx\plugins.


## Configuring

To change settings while in game, install [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager)

Otherwise, run once, and edit the config file generated in BepInEx\config


## Mod Info

### InterfaceTweaks

Various small updates interface updates
 - Add a damage preview in combat. When a character is selected and you hover over another character, it will show the expected damage (color-coded for hp/shield/mana damage) from a direct basic attack (no support for aoe attacks/skills/items abilities).
 - Add feature to home Sex-Training tab - you may now click on the info display for an MC part (e.g. Hands). This will filter the character views to only include species which can contribute to raising the max level for that part.
 - Optional highlight for species that you have not unlocked yet - will indicate characters in the character overview, hires in the tavern, enemies in battle, and overworld parties. Optional list of unlockable traits that are not unlocked yet. (both OFF by default)
 - Updated AP pips in combat to change color when over 2 AP
 - Make the character tooltip in the tavern hire screen easier to get to


### BugFixes

Fixes for bugs that's I've bothered to implement. Currently includes:
 - Fix pussy interest change for farm pairs to be based on if the partner has a pussy (originally, based on if partner has a cock)
 - Fix ratio of poor/medium/rich customers for brothel shows

