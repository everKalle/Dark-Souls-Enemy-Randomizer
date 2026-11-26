# Dark Souls Enemy Randomizer

Enemy randomizer generates a random placement of enemies to make your playthrough of Dark Souls different and more challenging. Depending on the options you choose, it's possible to, for example, fight Manus (or 3 of them) in the swamps of Blighttown, or pass through the fog gate in Anor Londo only to see a puny hollow and a painting guardian instead of O&S.

Supports **Dark Souls: Prepare to Die Edition** and **Dark Souls Remastered**.

> **About this fork:** this project is a **.NET 10 migration** of the Python/Tkinter Dark Souls Enemy Randomizer
> (v0.4.1.2), forked from <https://github.com/xtvsx/Dark-Souls-Enemy-Randomizer>.
> The migration is feature complete (not yet checked against a real game install, see the CHANGELOG). The last Python build
> stays in `dist/`. See [CHANGELOG.md](CHANGELOG.md).

## Thanks

A big thank you to **rycheNhavalys**, the original author of the Enemy Randomizer. All of the game knowledge,
file format work and randomization rules in this project come from the original Python implementation,
which is kept unchanged in `legacy/python/` for reference.

## Repository layout

```
DarkSoulsEnemyRandomizer.slnx     .NET solution
src/                              .NET 10 projects (added step by step)
  EnemyRandomizer.Core/           domain logic, no IO / no UI
  EnemyRandomizer.Formats/        game file formats (DCX, BND3, MSB, PARAM, LUAGNL, LUAINFO, EMEVD, FFX)
  EnemyRandomizer.Infrastructure/ game detection, backups, file system, JSON config
  EnemyRandomizer.App/            WPF desktop app (MVVM)
tests/                            unit, config and (later) round-trip tests
dist/enemyRandomizerData/         runtime data shipped next to the exe
legacy/python/                    original Python implementation (reference only)
```

Layer rule: `App → Infrastructure → Formats → Core`. Core depends on nothing.

Settings and game tables live in JSON files under `src/EnemyRandomizer.App/config/` and are copied next to the exe,
so they are easy to read and change without rebuilding.

## Build

Requirements: Windows, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build DarkSoulsEnemyRandomizer.slnx
dotnet test --solution DarkSoulsEnemyRandomizer.slnx
```

Run from source against a game install without copying the exe there:

```powershell
dotnet run --project src/EnemyRandomizer.App -- --App:GameDirectory="D:\Games\DARK SOULS REMASTERED"
```

To try the UI without a game install, point it at `dist` (it contains `enemyRandomizerData`). Randomize and Revert
then change files in `dist`, so use a copy.

### Configuration

All settings are JSON files in `config/` next to the exe (source: `src/EnemyRandomizer.App/config/`).
They are validated at startup; an invalid file shows an error dialog with the exact problem.

| File | Content |
|---|---|
| `appsettings.json` | title, data folder layout, log levels |
| `game-editions.json` | maps, per-edition start indices, event ids, exe hashes, exe memory patch |
| `randomizer-options.json` | every option: tab, labels, descriptions, defaults, ranges, when it has an effect |
| `difficulty-curves.json` | chance tables per difficulty strictness |
| `rules.json` | banned replacements, spawn-rate nerfs, unique enemy limits |
| `map-patches.json` | position fixes, item lots, allegiances, tail cuts, easy asylum pools |
| `models.json` | character models added to every map |
| `messages.json` | user facing texts |

Any value can also be overridden with an environment variable `ENEMYRANDOMIZER_<Section>__<Key>`
or a command line argument `--<Section>:<Key>=value`.

Publish a single self-contained exe (about 136 MB):

```powershell
dotnet publish src/EnemyRandomizer.App -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The output folder contains `EnemyRandomizer.exe`, `config/` and `enemyRandomizerData/` (copied from
`dist/enemyRandomizerData`). Ship all three together; the `.pdb`/`.xml` files are not needed.

Format tests that need real game files run only when `DS_GAME_DIR` points to a Dark Souls install folder.

### Legacy Python

The old version stays runnable for comparison tests: `python legacy/python/randomizer.py` (Python 3, Tkinter),
started from inside a game folder that contains `enemyRandomizerData/`.

## Installation [Prepare to Die Edition]

### Fresh install

1. Unpack your Dark Souls archive files using [UnpackDarkSoulsForModding](https://www.nexusmods.com/darksouls/mods/1304/?).
2. Place `EnemyRandomizer.exe` and the `enemyRandomizerData` folder from the `.zip` file in `Dark Souls Prepare to Die Edition\DATA\` (the folder with `DARKSOULS.exe`).
3. Run `EnemyRandomizer.exe`. The first start takes a while: it prepares files for randomization and backs up the originals.
4. Press **Randomize** to randomize the enemies with the selected settings and write the modified `.msb` and `.luabnd` files.

### Updating from a previous version

1. Open the previous version and press **Revert to normal**.
2. Delete or rename the old `EnemyRandomizer.exe` and `enemyRandomizerData` folder.
3. Copy the new `EnemyRandomizer.exe` and `enemyRandomizerData` folder to `Dark Souls Prepare to Die Edition\DATA\`.

## Installation [Remastered]

1. Place `EnemyRandomizer.exe` and the `enemyRandomizerData` folder in `DARK SOULS REMASTERED\` (the folder with `DarkSoulsRemastered.exe`).
2. Run `EnemyRandomizer.exe`. The first start takes a while (longer than on PTDE).
3. Press **Randomize**.
4. Back up your save file in `Documents/NBGI/DARK SOULS REMASTERED` and consider Steam offline mode (it is unknown whether the changes can get you banned from online play).

Updating works the same as for PTDE, using `DARK SOULS REMASTERED\` as the target folder.

## Restoring normal enemy placement

1. Run the randomizer and press **Revert to normal** to restore the original map and script files.
2. [Remastered] Restore your backed-up save file as well.

## Credits / Thanks

* __HotPocketRemix__ - bnd file unpacking/repacking implementation, event scripting tools that allowed me to change certain event scripts, program GUI inspiration
* __Wulf2k__ - looking at MSBEdit's source code helped me create my implementation of msb editing
* __Meowmaritus__ - [this](https://www.reddit.com/r/DarkSoulsMods/comments/6a4sbg/are_custom_maps_technically_feasible/dhe114q/) comment describing luagnl and luainfo file formats
* __Metal Crow__ - fix for the game (PTDE) crashing when trying to load all visual effects at once
* __Lan5432__ - helping test v0.2, providing the best comments about screenshots
* __DuckyKoi__ - gifting the remaster so the randomizer could be ported to it
