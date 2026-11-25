# Changelog

All notable changes to this project are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: `MAJOR.MINOR.PATCH`.

## [Unreleased] - .NET 10 migration

This project is a .NET 10 migration and fork of <https://github.com/xtvsx/Dark-Souls-Enemy-Randomizer>.
Thanks to rycheNhavalys, the original author of the Enemy Randomizer.

### Step 2 - WPF UI shell
#### Added
- Main window built from `randomizer-options.json`: three tabs, radio groups and sliders, "[No effect]" marking
  from the enable conditions, and a description panel that previews a value's description on hover.
- Seed box, "Restore default settings", enemy config selection, Randomize / Revert / Copy placement buttons.
- Text config dialog (v0.4.x `/-/` strings still work in both directions).
- Enemy config editor (enable/disable enemies, Save, Save As, Enable All / Disable All; same file format as v0.4.x).
- Progress window; operations run off the UI thread (`IProgress<T>`), fixing the old thread-unsafe progress bar.
- `lastConfig.json` keeps the last used settings; an existing v0.4.x `lastConfig.txt` is imported once.
- About dialog with the fork notice and thanks to the original author.
- Simple game edition detection (by exe name) for the status line and the Artorias "Remastered only" flag.
- `EnemyRandomizer.App.Tests` for the view model logic (75 tests in total).

#### Changed
- Buttons call a preview service that shows the progress steps but **does not change game files yet**
  (the engine is ported in steps 5-8).
- Text config validation uses the allowed values from the JSON, which also fixes the v0.4.x validation slips
  (NPC mode checked against the normal-enemy value, strictness accepted 0-100, prevent-same accepted 0-2).

#### Removed
- Donation button/link and the download links to the original author's mod pages (README, `dist` readme, config).

### Step 1 - Base projects, DI, JSON configuration
#### Added
- Projects `EnemyRandomizer.Core`, `.Formats` (empty until step 4), `.Infrastructure`, `.App` (WPF, `net10.0-windows`)
  and tests `EnemyRandomizer.Core.Tests`, `EnemyRandomizer.Infrastructure.Tests` (xunit v3 on Microsoft.Testing.Platform).
- Generic Host bootstrap (`AppHost`) with DI, Serilog file log (`enemyRandomizerData/logs/app-YYYYMMDD.log`)
  and a placeholder main window (MVVM, CommunityToolkit.Mvvm).
- JSON configuration in `config/` next to the exe, bound with `IOptions<T>` and validated on start
  (broken config shows a dialog instead of crashing):
  - `appsettings.json` - title, project link, data folder layout, retries
  - `game-editions.json` - maps, start indices per edition, event ids, folders, exe hashes, exe memory patch
  - `randomizer-options.json` - all 21 options: tab, labels, descriptions, defaults, ranges, enable conditions, legacy text-config index
  - `difficulty-curves.json` - three strictness tables
  - `rules.json` - unique-enemy limits, banned pairings, Gwyn/Lautrec spawn nerf
  - `map-patches.json` - ambush/other positions, item lots, allegiances, tail cuts, easy-asylum pools
  - `models.json` - character + tail models added to every .msb
  - `messages.json` - startup error and dialog texts
- Tests check the shipped JSON against the legacy Python tables (start indices, defaults string, exe patch bytes, model counts).

#### Notes
- Per-map special cases (`randomizer_rng.py:1427-1521`: skips, reclassifications, mosquito ids, Andre/Gwyndolin)
  move to `rules.json` in step 7 together with the code that uses them.
- Format tables (`method_names.py`, `msbdata.py`, `NpcParam.py`) become embedded JSON in step 4.

### Step 0 - Repository preparation
#### Added
- .NET solution `DarkSoulsEnemyRandomizer.slnx` with shared `Directory.Build.props`,
  central package management (`Directory.Packages.props`), `global.json` (SDK 10) and `.editorconfig`.
- New README with build/publish instructions and repository layout.
- This changelog.

#### Changed
- Python implementation moved to `legacy/python/` (reference only, still runnable).
- `.gitignore` switched from Python to .NET rules; game-folder ignores kept.

### Planned (migration steps)
1. ~~Base projects, DI, JSON configuration~~ (done)
2. ~~WPF UI shell~~ (done) (options rendered from JSON)
3. Game install detection and startup checks
4. Binary file formats (DCX, BND3, MSB, PARAM, LUAGNL, LUAINFO, EMEVD, FFX)
5. First-time setup, backup and revert
6. Data repositories (enemy catalog, map references, AI references)
7. Randomization engine
8. Copy enemy placement from reference file
9. Single-file publish and parity check with v0.4.1.2

### Known issues (carried over from Python, fixed in a later refactor)
- Seeds from v0.4.x do not give the same layout in .NET (new RNG). Use "Copy enemy placement" with an old
  `enemy-layout-*.txt` file to reproduce an old layout.
- Blank seed path crashes on Python 3.11+ (`random.seed(datetime)`).

## [0.4.1.2] - last Python release
- Last version built with Python/Tkinter and PyInstaller (`dist/randomizer.exe`).
