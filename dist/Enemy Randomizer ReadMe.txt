--------------------------------
Dark Souls Enemy Randomizer v0.4
--------------------------------

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

This is still kind of an early version of the mod, and does still have some issues.

--v0.4 Changelog:
* Fixed .msb file backups being overwritten every time in v0.3.2.
* Randomizer attempts to fix invalid .msb file backups that were caused by v0.3.2, should they exist.
* Bosses replacing normal enemies now drop souls.
* Added a slider to change the percentage of souls dropped by bosses replacing normal enemies.
* Added the 'Enemy type replacement' option: when enabled same enemies in one area will be replaced with the same enemies. (For example all Silver Knights in Anor Londo could become Darkwraiths, instead of individual Silver Knights being different).
* Added 'Pinwheel Chaos' option: when enabled the main Pinwheel in the boss fight is not replaced, but the clones are.
* Added an option to nerf Gwyn spawn rate.
* Added an option to avoid an enemy getting replaced with the same enemy.
* Changed the the item drop of all Mimics in the game from a dropped guaranteed item to an itemlot awarded via event scripts, so the items can be obtained when the enemies are replaced.
* 2 Passive Pisacas that drop the unique miracles recieved the same treatement as mimics.
* Randomizer removes the ItemLotId-s of the previously mentioned enemies from NpcParam.param, so that when these enemies don't get replaced (eg. when Replace chance is less than 100%), the items are not aquired twice.
* Added separate pages for options to fit them all without making the window massive.
* Description area shows descriptions only for the options on the selected page to avoid a massive wall of text.
* Removed Armored Tusk [Parish Version] and Great Felines from spawning by default, since all they want to do is go home (can be re-enabled using Enemy Config).
* Removed Phalanx from spawning as they do nothing (can be re-enabled using Enemy Config).
* Tweaked the size limit on some spawn locations to allow a bit larger enemies to spawn.
* Raised the difficulty value of Quelaag boss fight to be a bit higher than individual gargoyles.
* Quelaag is considered a larger boss than previously, to avoid her spawning in Taurus/Capra fights when size limit is enforced as her lava can be impossible to avoid in those arenas.
* Lowered the Unique enemy limit on Remastered from 70 to 60, improves stability a bit, without sacrificing much enemy variety.
* Blighttown has a separate, lower unique enemy limit now, to lessen the chances of the game crashing when entering Great Hollow or Demon Ruins.
* Writing permission error message is not displayed when the check wasn't actually done because of missing files.
* Changed the decription of Mimic replacement mode, to reflect the changes to the item rewards.