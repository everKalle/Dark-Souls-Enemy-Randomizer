----------------------------------
Dark Souls Enemy Randomizer v0.4.1
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

--v0.4.1 Changelog:
* Tail cuts now work on spawned enemies (though loot will be awarded only for the first cut for each tail-cuttable enemy type; meaning each tail-cut award can not be obtained more than once).
* Added an option to make bosses, that replace normal enemies, stay dead permanently.
* Added an option to spawn hostile Undead Merchant, Andre of Astora, Vamos and Hawkeye Gough as enemies.
* Bosses replacing normal enemies no longer respawn after reloading the game after a save&quit/crash.
* Enemies in ambushes involving Hollows hanging from ledges have been moved, so that the replacing enemies don't immediately fall to their deaths.
* Enemies replacing ghosts in New Londo Ruins that previously spawned in walls (and got stuck) or in locations where they immediately die have been moved to valid locations.
* Added an option called "try for unique bosses" which, when enabled, tries to minimize the repetition of bosses in actual boss fights.
* Enemies replacing the two Undead Dragons have finally discovered the concept of gravity and no longer walk around in mid-air.
* Enemy replacing the Undead Dragon in Valley of the Drakes no longer spawns in mid-air.
* Enemies replacing Titanite Demons now award Demon Titanite when killed.
* Enemy replacing the Berenike Knight now awards the Titanite Shard when killed.
* Enemy replacing the red-eyed Chaos Bug now awards the Sunlight Maggot when killed.
* Added a new option for Type Replacement that makes bosses replacing normal enemies disobey the normal type replacement, so that when normal enemies can be replaced with both normal enemies and bosses, individual enemies can become bosses, instead of all of one specific enemy type becoming bosses.
* Added an option to disable replacement of the respawning mosquitoes in Blighttown swamp.
* Chaos Bugs are not spawned by default, since they're passive.
* Evil Vagrants can now spawn.
* Fixed an issue that caused Anor Londo Gargoyles and Hellkite Drake having invalid parameter values when spawned as a normal enemies.
* Seath can no longer replace Stray Demon, to avoid him poking his head and wings into the Asylum Demon arena.
* Added a Difficulty Curve + Easy Asylum mode, so that even on looser difficulty curves the Asylum is guaranteed to remain easier.
* Size limit is always enforced on the enemy replacing the Crest Key mimic to avoid the replacer potentially getting stuck in the wall/ceiling and making the key unobtainable.
* Randomizer now saves the current settings on randomization and automatically loads them on startup.
* Added 'Restore default settings' button to set all the options to their default values.
* Randomizer should now work with notaprofi's Infinite NG+ mod.
* Message given when PTDE version's .exe checksum does not match any known ones is improved.
* Randomizer gives a proper error message when it's unable to modify DARKSOULS.exe on PTDE.
* Fixed a small bug that caused the randomizer to throw an error on launch about a sellout page variable in some circumstances.