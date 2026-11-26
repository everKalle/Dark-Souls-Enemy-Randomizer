# Dark Souls Enemy Randomizer

Enemy randomizer generates a random placement of enemies to make your playthrough of Dark Souls different and more challenging. Depending on the options you choose, it's possible to, for example, fight Manus (or 3 of them) in the swamps of Blighttown, or pass through the fog gate in Anor Londo only to see a puny hollow and a painting guardian instead of O&S.

Supports **Dark Souls: Prepare to Die Edition** and **Dark Souls Remastered**.

> **About this fork:** this project is a **.NET 10 migration** of the Python/Tkinter Dark Souls Enemy Randomizer
> (v0.4.1.2), forked from <https://github.com/everKalle/Dark-Souls-Enemy-Randomizer>.
> The migration is feature complete (not yet checked against a real game install, see the CHANGELOG). The last Python build
> stays in `dist/`. See [CHANGELOG.md](CHANGELOG.md).

## Thanks

A big thank you to **everKalle**, the original author of the Enemy Randomizer. All of the game knowledge,
file format work and randomization rules in this project come from the original Python implementation.

## Repository layout

```
DarkSoulsEnemyRandomizer.slnx     .NET solution
src/                              .NET 10 projects (Core, Formats, Infrastructure, App)
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
