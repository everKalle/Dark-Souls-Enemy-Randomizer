# Changelog

All notable changes to this project are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: `MAJOR.MINOR.PATCH`.

## [Unreleased] - .NET 10 migration

This project is a .NET 10 migration and fork of <https://github.com/xtvsx/Dark-Souls-Enemy-Randomizer>.
Thanks to rycheNhavalys, the original author of the Enemy Randomizer.

### Step 9 - Publish
#### Added
- `dotnet publish` (single file, self-contained, win-x64) now also copies `dist/enemyRandomizerData` next to the exe,
  next to the `config/` folder. The published exe starts and shows the UI.

#### Known issues
- Not yet verified on a real game install: randomize, revert and copy placement have only been tested on generated
  game folders. Still to do before tagging v0.5.0: randomize with Python and .NET on PTDE and Remastered, feed each
  layout to the other's copy function and compare the msb files, then play-test Asylum and two more maps.
- Tail parts can get other entity ids than in the original run after "Copy enemy placement" (same as v0.4.x).

### Step 8 - Copy placement from reference
#### Added
- **"Copy enemy placement" works.** Pick an `enemy-layout-*.txt` (v0.4.x files work too) and the same layout is
  written to the game without any random numbers: saved model, AI, param, entity id, idle animation and position, plus
  the derived changes (AI scripts, item lots, tail cuts, allegiances, event fixes, roaming boss respawn, Darkroot Garden #2).
  A log file is written; no new layout file.
- `LayoutReference` (Core) parses the file with the v0.4.x error texts (missing settings/version, wrong edition,
  malformed lines); `ReplacementPlanner.PlanMapFromLayout` reuses the normal decision logic; `RandomizationRun`
  shares everything else between randomize and copy.
- Tests: parser (valid, missing header parts, malformed rows), copy after a different randomization restores the
  saved rows on PTDE and Remastered, edition mismatch is rejected.

#### Changed
- The placeholder `PreviewRandomizerService` was removed.

### Step 7 - Randomization engine
#### Added
- **Randomize now changes game files.** `RandomizationRun` prepares copies, resets event files and copies the
  randomizer's own events, patches NpcParam ("with souls" bosses), plans and writes every map (map, AI scripts,
  events, all with `.bak`), copies Darkroot Garden #2, and writes `logs/rlog<time>.txt` and
  `refs/enemy-layout-<time>.txt` (same format as v0.4.x).
- Core engine (pure logic, no file access): `ReplacementPlanner` (normal/boss/NPC picking, difficulty curve,
  unique-enemy limit, unique bosses, type replacement, easy asylum), `RunOptions` (typed option values),
  `IRandomSource`/`SeededRandom`, and the rules as a composite (`BannedPairingRule`, `RandomRejectionRule`,
  `PreventSameRule`, `DistinctTypeReplacementRule`).
- Infrastructure: `MapPlanApplier` (rows, entity ids, tails, positions), `MapAiScripts` (AI injection),
  `MapEventEditor` (port of `event_tools.py`), `LayoutReferenceWriter`, `RunLog`.
- All special cases are data now:
  - `rules.json` → `CreatureRules` (27 rules: skips, position fixes, reclassifications, pinwheel chaos,
    mosquitoes, DLC mimic size, Andre → Gwyndolin by enemy id instead of catalog index 117), `NoSoulVariant`.
  - `map-patches.json` → event templates and offsets, event fixes (gargoyle #2, Moonlight Butterfly),
    always-replaced events (Asylum Demon warping), instruction patches (undead dragon gravity/collision),
    tail cuts disabled on Remastered DLC, easy-asylum map id.
  - `game-editions.json` → `CopyOf` for derived maps.
- 25 new tests: planner rules with a scripted random source, full runs on generated PTDE and Remastered game
  folders (backups, model indices, AI injection, template events, soul variants, Darkroot copy, same seed = same
  files, randomize twice = same result, revert after randomize), shipped creature rules check.

#### Fixed (compared to v0.4.x)
- Creatures appended to Darkroot Garden #2 (tail parts) now get the map's model offset (pointed at the wrong model).
- "Try for unique bosses" with the unique-enemy limit reached no longer fails when no boss was used yet in the map
  (falls back to a normal enemy like the other paths).
- Item-lot event slots are counted per map (v0.4.x kept counting across maps).
- An empty seed no longer crashes on Python 3.11+ style errors; a generated seed is logged and shown.

#### Notes
- Same seed + same settings + same data = same layout. Seeds from v0.4.x give different layouts (new RNG).
- Copy enemy placement still uses the preview (step 8).

### Step 6 - Data repositories and enemy pools
#### Added
- Core data model (`EnemyDefinition`, `ReplacementTarget`, `OriginalEnemy`, `AiReference`) and parsers
  (`DataFiles`) for `valid_new.txt` / custom enemy configs, `valid_replacements.txt`, `original_enemies_ref/*.txt`
  and `airef.csv`. Broken rows now fail with the row number instead of an index error later.
- `EnemyPool`: usable enemies by kind (normal / boss / any), size class (an enemy fits every spot of its size or
  larger) and difficulty - replaces the nine nested lists of `loadFiles`. Hostile NPCs join the normal pool only
  when the option is on; "Remastered only" enemies (Artorias) only on Remastered.
- `ReplacementTargets`: substring lookup of the original enemy type (first match wins, like v0.4.x).
- `IGameDataRepository` / `GameDataRepository`: reads all of the above; targets and AI references are loaded once.
- 22 tests, incl. checks of the shipped data: all 123 enemies load, every map reference loads for both editions,
  every AI of an enabled enemy has an `airef.csv` entry, the easy-asylum indices point to normals/bosses.

#### Changed
- The enemy config editor and the randomizer now use the same "enabled" rule (only `0`, or `2` on Remastered).

### Step 5 - First-time setup, backup and revert
#### Added
- `FirstTimeSetup` (legacy `firstTimeSetup`): clean .msb copies with all 131 character/tail models appended,
  original .luabnd copies, extracted AI scripts, logs/refs folders, GameParam copy, CommonEffects merge
  (PTDE: map effect bundles reduced to their first entry), cleanup of broken v0.3.2 backups. Every part skips
  work that is already done, so it runs on every start without rewriting files.
- Startup runs after the main window is shown, with a progress window (v0.4.x froze before the window appeared,
  for minutes on Remastered). Broken copies are retried automatically (`App:FileCopyRetries`), then the user is
  asked "Retry copying?" like v0.4.x; a failed effect write is a warning.
- "Revert to normal" is real now (`GameRestorer`): maps, events, AI scripts from their `.bak`; effect files and the
  exe only on request; NpcParam from the GameParam copy (other params untouched).
- `BackupFiles` (`.bak` convention) and `GameArchive` (BND3 with transparent DCX).
- 11 new setup/revert tests on generated PTDE and Remastered game folders (181 tests in total).

#### Changed
- Copies, AI scripts and effect sources are taken from the `.bak` original when one exists (v0.4.x copied the
  current file, which could already be randomized).
- PTDE map effect bundles are only rewritten when they still have more than one entry (v0.4.x rewrote them on
  every start).
- Randomize and Copy placement still use the preview (no file changes) until steps 7-8.

### Step 4b - Binary formats: MSB, PARAM / NpcParam, FFX merge
#### Added
- `MsbFile`, `MsbTable`, `MsbRow`, `MsbLayout`: map files (models, 13 event types, 4 point types, 9 part types)
  driven by `Resources/msb-layouts.json` (the 28 layout strings of `msbdata.py`, unchanged). Rows can be read by
  field name (`row.GetInt("NPCParam")`), `AddCreature` shifts part references like v0.4.x, writer rebuilds offsets
  identically (points sorted by index).
- `ParamFile` (PARAM container), `ParamSchema`/`ParamRecord` (typed rows + bit fields) and `NpcParam` with
  `Resources/npcparam-schema.json` (122 fields + 36 bit fields from `NpcParam.py`): soul variants (id + 50),
  boss soul count, respawn flags, item lot removal.
- `EffectMerger`: merging map effect bundles into CommonEffects (PTDE: all, Remastered: enemy effects &lt; 20001),
  PTDE map bundle truncation.
- `rules.json`: `BossSouls` (29 bosses) and `NpcParamsWithoutItemLot` moved from code to config (validated: sorted,
  unique ids).
- Real-game round-trip tests for maps, AI archives and NpcParam; they run when `DS_GAME_DIR` points to a game
  folder (read-only) and are skipped otherwise.

#### Fixed (compared to v0.4.x)
- NpcParam bit fields: multi-bit fields were read MSB-first and written LSB-first (e.g. `vowType` 1 ↔ 4). v0.4.x
  only stayed correct because it saved the param twice per run; reading and writing are now symmetric.
- "With souls" rows get their own copy of the boss data (v0.4.x shared one object between both rows).

### Step 4a - Binary formats: DCX, BND3, Lua AI archives, EMEVD
#### Added
- `EnemyRandomizer.Formats` (pure `byte[]` ↔ model, no file access):
  - `StructFormat`: port of Python `struct` for `b B ? h H i I f` with native (`@`) alignment, `<` and `>`.
  - `GameText`: Shift-JIS (code page 932) and zero-terminated strings.
  - `DcxFile`: DCX "DFLT" read/write (mixed endianness, zlib body), EMEVD default header.
  - `Bnd3`: BND3 read (flags 0x74/0x54/0x70) and write (`07D7R6`, 16-byte alignment) - by HotPocketRemix.
  - `LuaBnd`, `LuaGnl`, `LuaInfo`: AI script archives incl. adding scripts and AI entries like v0.4.x.
  - `EmevdFile`, `DkScript`, `InstructionCatalog`: event scripts, the dkscript compiler/exporter, and
    `Resources/emevd-instructions.json` (246 instructions converted 1:1 from `method_names.py`).
  - Deep `Clone()` for events and instructions (v0.4.x appended the same template object to every map).
- `EnemyRandomizer.Formats.Tests` (55 tests) using the shipped `.emevd`/`.emevd.dcx` files and dkscripts.

#### Fixed (compared to v0.4.x)
- `init_event` calls lost their last 4 bytes when the started event only reads a later argument offset
  (e.g. m13_00_00_00 event 11305100, 30 calls). The reader now keeps those bytes.

#### Notes
- Original game EMEVD files have 4 padding bytes after the argument block; like v0.4.x the writer does not
  write them (content is identical, the game accepts both).
- The shipped dkscripts are a lossy text form of the shipped `.emevd` files (parameter offsets are renumbered),
  so tests compare structure and content, not bytes.
- Python is not installed on the dev machine, so there is no direct Python ↔ .NET byte comparison yet.

### Step 3 - Game install detection and startup checks
#### Added
- Exe detection by SHA-256 (PTDE: unpacked / not unpacked / patched, normal and debug; unknown hashes allowed with
  a warning) or by name (Remastered). The window title shows `[Current Mode: PTDE|REMASTERED]` like v0.4.x.
- Startup checks as a composite of `ExeCheck`, `GameFilesCheck`, `DataFilesCheck` and `WritePermissionCheck`
  (open/closed: a new check is one class + one DI line). Problems replace the description panel in red and
  disable Randomize / Revert / Copy placement.
- `GameFileLayout` (bridge): one place that knows the file names per edition (`.dcx` on Remastered).
- PTDE exe memory patch (fix by Metal Crow) with one-time backup `DARKSOULS.prerandomizer[.debug].exe`,
  applied at startup when all checks pass; `Restore()` ready for "Revert to normal" (wired in step 5).
- Required data files are listed in `appsettings.json` (`App:RequiredDataFiles`).
- 16 new tests (detection, every check, patch, restore).

#### Changed
- The write-permission check no longer overwrites a real `.msb` with test bytes (v0.4.x did, then restored it);
  it opens the file for writing and creates/deletes a temp file instead. Game files are never touched.
- A missing `GameParam.parambnd[.dcx]` is now reported (v0.4.x checked it but never showed it).
- Both edition reference files of m13_00_00_00 are checked (v0.4.x only checked the Remastered one).

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
3. ~~Game install detection and startup checks~~ (done)
4. ~~Binary file formats (DCX, BND3, MSB, PARAM, LUAGNL, LUAINFO, EMEVD, FFX)~~ (done; real-game check pending)
5. ~~First-time setup, backup and revert~~ (done; real-game check pending)
6. ~~Data repositories (enemy catalog, map references, AI references)~~ (done)
7. ~~Randomization engine~~ (done; real-game check pending)
8. Copy enemy placement from reference file
9. Single-file publish and parity check with v0.4.1.2

### Known issues (carried over from Python, fixed in a later refactor)
- Seeds from v0.4.x do not give the same layout in .NET (new RNG). Use "Copy enemy placement" with an old
  `enemy-layout-*.txt` file to reproduce an old layout.
- Blank seed path crashes on Python 3.11+ (`random.seed(datetime)`).

## [0.4.1.2] - last Python release
- Last version built with Python/Tkinter and PyInstaller (`dist/randomizer.exe`).
