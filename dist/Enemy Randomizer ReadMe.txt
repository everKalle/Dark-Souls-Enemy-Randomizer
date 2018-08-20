----------------------------------
Dark Souls Enemy Randomizer v0.3.1
----------------------------------

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

1. Download Enemy Randomizer and place EnemyRandomizer.exe and enemyRandomizerData folder from the .zip file to DARK SOULS REMASTERED\ (the same folder where DarkSoulsRemastered.exe is).
2. Run the EnemyRandomizer.exe. The first time you launch the program, it will take some time to start up, as it's preparing files for randomization and backing up the originals (this part takes a bit longer on the Remaster).
3. Press the Randomize button to randomize the enemies according to the selected settings and write the modified data to .msb and .luabnd files.
4. You should also probably back up your save file in Documents/NBGI/DARK SOULS REMASTERED and go into offline mode in Steam just in case (I don't know if the changes made by the randomizer can get one banned from online play).


--Restoring normal enemy placement after randomizing:
* Run the randomizer and press the "Revert to normal" button to restore the original map and script files.
* [Remaster] If you backed up your save file, then restore that as well

Note that this mod is incompatible with any mod that changes the .msb files (like Prepare To Die Again).

Credits/Thanks:
HotPocketRemix - bnd file unpacking/repacking implementation, program GUI inspiration.
wulf2k - looking at MSBEdit's source code helped me create my implementation of msb editing.
Meowmaritus - this: (https://www.reddit.com/r/DarkSoulsMods/comments/6a4sbg/are_custom_maps_technically_feasible/dhe114q/) comment i found describing luagnl and luainfo file formats.
Metal Crow - fix for the game (PTDE) crashing when trying to load all visual effects at once.
Lan5432 - helping me test v0.2, providing the best comments for screenshots.
DuckyKoi - gifting me the remaster so I could port the randomizer to it.

This is still an early version of the mod, and does still have some issues.

--v0.3.1 Changelog:
* Added the possibility to configure what enemies will be placed in the world.
* Custom enemy configurations are saved in enemyRandomizerData\customConfigs as text files.
* EnemyConfig screen contains comments on several enemies, mostly explaining why certain ones are disabled.
* Randomizer logs the custom config if one is used (in a compact, not really humanly readable format).
* Artorias can be placed into the world by default on the Remastered version, as he doesn't seem to cause crashes there (like he does on PTDE) when spawned as a normal enemy.
* Gaping Dragon and Kalameet can't replace Capra anymore, as they can potentially get stuck above the arena.
* Seath, Gaping Dragon and Kalameet can't replace Boss Pinwheel, again can easyly phase themselves above the arena and can't be killed.
* Seath, Gaping Dragon and Kalameet are not allowed to be Iron Golem, as they can get stuck floating above the ground, being no threat and only hittable with ranged attacks.
* Fixed Gargoyle#2 dying when Gargoyle#2 mode is 'Do not replace'.
* Moonlight Butterfly no longer sits on it's tower if it doesn't get replaced.
* Quelaag should no longer appear in Asylum on the strictest difficulty mode.
* Tweaked the strictest difficulty curve a bit, reduced the possibility of noticeably harder foes showing up instead of easier enemies (eg Super Ornstein as a Gargoyle).
* Removed the Hellkite and ChaosBug/Vile Maggot options, because they're kinda pointless now that enemy config can be changed.
* Hellkite spawning has been disabled by default (but can be re-enabled with the enemy config), since it's a boring enemy
* Titanite demons are disabled by default (again), since their AI doesn't activate properly, but like hellkite, can be re-enabled if one wants them to spawn.
* Huge rat can be placed into the world (honestly I have no idea why I had it disabled)
* Removed a few effect files from being loaded on the Remaster (didn't seem to be enemy related), very slightly reduces the time it takes for the effect preparation step to complete.
* Tweaked unique enemy limit on Remaster.
* Should have a few less crashes on Remastered.
* Added a message to the console for the effect preparation step to state that this part takes a while.
* FFX Handler no longer reports that the CommonEffects.ffxbnd(.dcx) has been saved when it has been prepared previously and doesn't need saving again.
* Randomizer sort of checks whether or not it has permission to modify files, complains if it can't. Avoids the situation where the randomizer seems to complete the randomization properly, but doesn't actually change the files.
* Randomizer gives shows an error message should it run into an exception when randomizing instead of doing absolutely nothing and getting stuck.