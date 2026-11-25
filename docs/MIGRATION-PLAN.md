# Plan: Migrate Dark Souls Enemy Randomizer (Python/Tkinter) → .NET 10 WPF

## Context
Today: ~6.3k LOC Python 3 + Tkinter, no third-party deps, shipped via PyInstaller as `randomizer.exe`.
Core logic (`randomizer_rng.py`, 2082 lines) mixes UI (imports tkinter, touches widgets from worker thread),
file IO and rules. Lots of hardcoded tables. Goal: same features as one .NET 10 single-file exe,
built step by step (skeleton + UI first, then features). Big refactor comes later, but new code
follows SOLID/KISS/DRY and clean layers from day one. Config moves to JSON.

User decisions:
- UI: **WPF + MVVM** (CommunityToolkit.Mvvm).
- RNG: **new RNG ok** (System.Random behind `IRandomSource`); old seeds not reproducible.
  Old layouts still reusable via "Copy enemy placement" (layout ref file → no RNG).
- Repo: **move Python to `legacy/python/` now**, .NET in `src/`.
- Publish: **single-file, self-contained, win-x64**. `enemyRandomizerData/` stays beside exe.

## Target layout
```
DarkSoulsEnemyRandomizer.slnx
Directory.Build.props        (net10.0, nullable, implicit usings, TreatWarningsAsErrors, LangVersion latest)
Directory.Packages.props     (central package versions)
.editorconfig
src/
  EnemyRandomizer.Core/          domain + abstractions (no IO, no UI)
  EnemyRandomizer.Formats/       binary formats: BinaryReaderBE/LE, DCX, BND3, MSB, PARAM, LUAGNL, LUAINFO, EMEVD, FFX
  EnemyRandomizer.Infrastructure/ game install detection, exe patch, backups, file system, JSON config loading, logging
  EnemyRandomizer.App/           WPF exe (Views, ViewModels, DI bootstrap)
  EnemyRandomizer.App/config/    *.json (copied to output)
tests/
  EnemyRandomizer.Core.Tests/
  EnemyRandomizer.Infrastructure.Tests/  (shipped JSON config vs legacy tables; later IO tests)
  EnemyRandomizer.Formats.Tests/  (step 4; round-trip tests: read → write → bytes equal)
legacy/python/                    old code + eventscripts/ (reference only)
dist/enemyRandomizerData/         stays (runtime data, shipped)
```
Dependency rule: App → Infrastructure → Formats → Core. Core depends on nothing.

## Patterns (where + why)
| Pattern | Use |
|---|---|
| Factory | `IGameFormatFactory` → BND/MSB readers by PTDE vs Remaster (DCX on/off); `IRandomSourceFactory` from seed |
| Strategy | Difficulty mode (Curve / Random / EasyAsylum variants), fit mode, enemy pick (normal/boss/NPC) |
| Composite | `IReplacementRule` → `CompositeRule` (banned pairings, Gwyn nerf, preventSame, unique bosses, mosquito skip) |
| Chain / Pipeline | `IRandomizationStep` per map: load → pick → write MSB → inject AI → events → save |
| Decorator | `DcxCompressingStream`/`DcxFileSource` around plain BND; `LoggingRandomSource`; `BackupFileWriter` (makes `.bak` before write) |
| Proxy | `LazyGameParam` (load GameParam only when used); `BackupStoreProxy` |
| Bridge | `IGameEdition` (PTDE / Remaster) abstraction vs `IFileLayout` impl (paths, extensions, start indices) |
| Repository | `IEnemyCatalog` (valid_new.txt), `IMapEnemyRefRepository`, `IAiRefRepository`, `IEnemyConfigRepository` |
| Command (MVVM) | Randomize, Revert, CopyPlacement, Save config… as `IAsyncRelayCommand` |
| Options | `IOptions<T>` from JSON via Microsoft.Extensions.Configuration + DI (Generic Host) |

## JSON config (src/EnemyRandomizer.App/config/)
- `appsettings.json` – title, fork project URL, original author (credit), data folder layout, retry counts.
- `game-editions.json` – per edition: exe name, SHA-256 hashes → state, patch bytes, file extensions, dcx flag,
  `inputFiles`, `names`, `startIndices`, `startEventEntityIDs`, ffx files, GameParam path, NpcParam index.
- `randomizer-options.json` – each GUI option: key, tab, label, description text, values/labels, default, range,
  enable-condition. (Drives UI → DRY; replaces `messages`, radio defs, default string, validation ranges.)
- `difficulty-curves.json` – 3 strictness tables (offset → chance).
- `rules.json` – banned pairings, Gwyn/Lautrec nerf %, mosquito IDs, Andre→Gwyndolin, reclassify list, skip list.
- `map-patches.json` – ambush/other positions, item lots, allegiances, tail values, respawn event IDs, easy-asylum indices.
- `models-to-add.json` – 125 models + 8 tail models, model row template.
- `messages.json` – error/info texts.
- Format tables → embedded JSON resources in Formats (not user-facing, but readable):
  `emevd-instructions.json` (`method_names.py` METHOD_NAMES + IAT → `{class:{idx:{name,fmt}}}`),
  `msb-layouts.json` (`msbdata.dataLayoutMap`, 27 entries), `npcparam-schema.json` (115 fields + 33 bitfields),
  `boss-souls.json` (29 entries + 11 item lots to remove).
- `airef.csv` stays as-is for now (read by repo); JSON conversion in refactor step.
User data stays in `enemyRandomizerData/` (lastConfig → `lastConfig.json`, customConfigs → keep `.txt` read + write).

## Step-by-step migration (each step = buildable, runnable, own commit + CHANGELOG entry)

### Step 0 – Repo prep
- `git mv` all `*.py`, `eventscripts/`, favicon source into `legacy/python/`; keep `dist/` as is.
- Replace `.gitignore` with .NET one (bin/ obj/ .vs/ *.user, publish/, TestResults/, artifacts/) + keep
  custom game-folder ignores (map/, param/, script/, event/, sfx/, DARKSOULS.exe…) + `legacy/python/__pycache__/`.
- `dotnet new sln` → `.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `global.json` (SDK 10).
- README.md rewrite: what it is, build (`dotnet build`), publish cmd, install steps (kept from old README), credits.
- CHANGELOG.md (Keep a Changelog): `[Unreleased] – .NET 10 migration`, `[0.4.1.2]` last Python.
- `analysis/ds-enemy-randomizer/INTENT.md` (modernize toolchain): goal = transform Python→.NET 10 WPF, answers above, date 2026-11-25.
- Legacy stays runnable (`python legacy/python/randomizer.py`) for golden tests.

### Step 1 – Base project + DI + config
- Create 4 src + 2 test projects, references per dependency rule.
- App: Generic Host (`Microsoft.Extensions.Hosting`), DI, `IOptions<T>` binding of JSON files, file logger (Serilog or `ILogger` file sink).
- Ship all JSON config files (data copied from Python tables, file:line in comments of PR).
- Tests: config loads + validates (ranges, list lengths equal per edition).

### Step 2 – UI shell (no logic)
- `MainWindow` with TabControl (Main / Other / Even More), seed box, right-hand buttons, description panel,
  enemy-config combo. Options rendered from `randomizer-options.json` via `OptionViewModel` (radio group / slider) + DataTemplates.
- Enable-conditions (e.g. boss chance only if mode 3) from JSON.
- Hover → description panel (like old `uf/f/c` tags).
- Windows: Text config dialog, Enemy Config Editor (DataGrid), Progress dialog, About dialog
  (fork notice + thanks; the donation panel of v0.4.x was removed on request).
- `SettingsSerializer`: old `/-/` string (import/export kept for compat) + `lastConfig.json`.
- Buttons wired to stub services (`IRandomizerService` fake) → UI testable before logic.

### Step 3 – Game install detection + startup checks
- `GameEditionDetector` (SHA-256 of exe → state), `InstallValidator` (port `checkIfRightPlace`, `checkProperUnpack`, write test).
- Show startup errors in description panel; disable buttons (old R:857-974 texts in `messages.json`).
- Exe patcher + restore (`check_exe.py`) behind `IExePatcher`.

### Step 4 – Binary formats (Formats project, test-first)
Order by dependency: `EndianBinaryReader/Writer` → DCX (zlib via `System.IO.Compression.ZLibStream`) → BND3 → MSB → PARAM/NpcParam → LUAGNL/LUAINFO → EMEVD (+ dkscript compiler) → FFX.
- Each: reader + writer + round-trip test on files from a real install (tests skip if `DS_GAME_DIR` env not set) and on small checked-in samples.
- Golden test: Python writes file X, C# writes file X from same input → bytes equal (DCX may differ: zlib impl; game-accept test instead).
Port hazards to handle:
- `StructFormat` helper that copies Python `struct` **native `@` alignment** (MSB rows, EMEVD IAT args, `calcsize` offsets). Unit-test vs Python `struct.calcsize` outputs.
- Shift-JIS (`CodePagesEncodingProvider`, cp932) for all strings.
- DCX: mixed endian header (`BinaryPrimitives`), read = skip 2 bytes + `DeflateStream`, write = `ZLibStream` SmallestSize.
- BND3: LE, record 24 vs 20 bytes by flag; write `BND307D7R6`, flag 0x74, 16-byte align.
- PARAM: packed `<IHih` header, typed rows; bitfields via real bit ops (not bit strings).
- MSB heterogeneous rows → typed records generated from `msb-layouts.json`.
- dkscript compiler: regex port (`^` anchored, invariant culture parse). Event templates **cloned** per map (Python shares refs).
- `event_tools` template indices bound by event ID, not position.

### Step 5 – First-time setup + backup/revert
- Port `firstTimeSetup` (copies, add models to MSB, extract AI scripts, GameParam backup, FFX merge), `createBackup`, `revertToNormal`, `revertParam`, `revertEmevds`.
- Revert command in UI (async, progress).

### Step 6 – Data repositories
- Parsers for `valid_new.txt`, `valid_replacements.txt`, `original_enemies_ref/*.txt`, `airef.csv`, `customConfigs/*.txt`.
- `EnemyPool` (size × difficulty index) built in Core.

### Step 7 – Randomization engine
- `IRandomSource` (System.Random, string seed → stable hash via SHA-256 → int; blank seed → time).
- Strategies: difficulty, fit, pick (normal/boss/NPC), type replacement, unique cap.
- `CompositeRule` for banned pairings.
- Per-map pipeline steps: special cases → pick → write MSB → AI inject → idle anim → positions/events/tail/factions → save.
- Boss souls param patch, emevd copy, Darkroot copy.
- `IProgress<RandomizeProgress>` + `Task.Run`; no UI calls in Core (fixes old thread bug).
- Layout ref file + log file writers (same format as Python → old refs still usable).

### Step 8 – Copy placement from reference
- Port `CopyFromReference`; file picker in UI.

### Step 9 – Publish + parity check
- `dotnet publish src/EnemyRandomizer.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`.
- Copy `dist/enemyRandomizerData` + `config/` next to exe (MSBuild target).
- Parity: randomize with Python and C# on PTDE + Remaster, then feed the C# layout ref to Python CopyFromReference
  and vice versa → identical msb bytes. Play-test Asylum + 2 maps.
- README/CHANGELOG update, tag v0.5.0.

### Later (separate request) – refactor
Fix known Python bugs found (config validation slips R:1556, blank-seed crash RNG:1280, dead `scrollable_window.py`),
split per-map special cases into data, etc. Port keeps behavior; bugs only noted in CHANGELOG "Known issues" until then.

## Verification
- `dotnet build` + `dotnet test` green each step.
- Round-trip + golden tests for every format.
- Manual: launch exe in PTDE + Remaster dir, randomize, revert, copy placement, custom enemy config edit/save.
- Compare layout refs / msb bytes Python vs .NET via CopyFromReference.
