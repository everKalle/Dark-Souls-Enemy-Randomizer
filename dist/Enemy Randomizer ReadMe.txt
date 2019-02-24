------------------------------------
Dark Souls Enemy Randomizer v0.4.1.2
------------------------------------

--Installation Instructions [PTDE]:

Fresh Install:

1. Unpack your Dark Souls archive files using [UnpackDarkSoulsForModding](www.nexusmods.com/darksouls/mods/1304/?)
2. Place EnemyRandomizer.exe and enemyRandomizerData folder from the .zip file to Dark Souls Prepare to Die Edition\DATA\ (the same folder where DARKSOULS.exe is).
3. Run the EnemyRandomizer.exe. The first time you launch the program, it will take some time to start up, as it's preparing files for randomization and backing up the originals.
4. The randomizer starts with the recommended settings, but feel free to configure
5. Configure the options and press the Randomize button to randomize the enemies and write the modified data to .msb and .luabnd files.

If you've used a previous version of the enemy randomizer:

1. Open the previous version of the randomizer and revert to normal
2. Copy EnemyRandomizer.exe and enemyRandomizerData folder of the new version to `Dark Souls Prepare to Die Edition\DATA\` overwriting any previously existing files.

--Installation Instructions [REMASTERED]:

Fresh Install:

1. Download Enemy Randomizer and place EnemyRandomizer.exe and enemyRandomizerData folder from the .zip file to DARK SOULS REMASTERED\ (the same folder where DarkSoulsRemastered.exe is).
2. Run the EnemyRandomizer.exe. The first time you launch the program, it will take some time to start up, as it's preparing files for randomization and backing up the originals (this part takes a bit longer on the Remaster).
3. Press the Randomize button to randomize the enemies according to the selected settings and write the modified data to .msb and .luabnd files.
4. You should also probably back up your save file in Documents/NBGI/DARK SOULS REMASTERED and go into offline mode in Steam just in case (I don't know if the changes made by the randomizer can get one banned from online play).

If you've used a previous version of the enemy randomizer:

1. Open the previous version of the randomizer and revert to normal
2. Copy EnemyRandomizer.exe and enemyRandomizerData folder of the new version to `DARK SOULS REMASTERED\` overwriting any previously existing files.

--Restoring normal enemy placement after randomizing:
* Run the randomizer and press the "Revert to normal" button to restore the original map and script files.
* [Remaster] If you backed up your save file, then restore that as well.

Note that this mod is incompatible with any mod that changes the .msb/.emevd files (like Prepare To Die Again).

Credits/Thanks:
HotPocketRemix - bnd file unpacking/repacking implementation, program GUI inspiration.
wulf2k - looking at MSBEdit's source code helped me create my implementation of msb editing.
Meowmaritus - this: (https://www.reddit.com/r/DarkSoulsMods/comments/6a4sbg/are_custom_maps_technically_feasible/dhe114q/) comment i found describing luagnl and luainfo file formats.
Metal Crow - fix for the game (PTDE) crashing when trying to load all visual effects at once.
Lan5432 - helping me test v0.2, providing the best comments for screenshots.
DuckyKoi - gifting me the remaster so I could port the randomizer to it.

--v0.4.1.2 Changelog:
*Fixed the randomizer throwing an error when loading a reference file where NPC replacement is enabled.
*Artorias is no longer allowed to replace Ceaseless Discharge as he crashes the game upon his death.
*Disabled Tail Cuts in DLC on Remastered for now as it's causing issues with bonfires.
*Added a message when .ffxbnd saving fails.
*Improved error message for missing game files.
*Fixed some of the error messages giving information about PTDE when using the randomizer on Remastered.
*Fixed missing .ffxbnd error message being shown twice.

--Changelogs for previous versions can be found on here on the nexusmods page:
https://www.nexusmods.com/darksouls/mods/1407?tab=logs