# Modernization intent - ds-enemy-randomizer

- **Date:** 2026-11-25
- **Goal:** transform (rewrite in a different technology, step by step)
- **From:** Python 3 + Tkinter, ~6.3k LOC, no third-party deps, PyInstaller exe (v0.4.1.2)
- **To:** .NET 10, WPF + MVVM (CommunityToolkit.Mvvm), Generic Host + DI, JSON configuration
- **Source:** `legacy/python/`
- **Plan:** step 0-9 as listed in `CHANGELOG.md` ("Planned"); refactoring in a later request.

## User answers (verbatim)
- UI framework: "WPF + MVVM (Recommended)"
- Seed parity with Python: "No, new RNG ok (Recommended)"
- Code location: "Move Python to legacy/ now"
- Packaging: "Single-file self-contained (Recommended)"
- Request: "Solution should be an executeable with all current functionalities. Create plan for step by step
  migration, start with base project and UI and enrich with functionalities step by step. Refactoring should be
  done in later request. ... KEEP architecture principles and focus on SOLID, KISS, DRY and common code
  architecture patterns like composite, factory, bridge, proxy, decorator or others. Move configuration into json
  files to make it easy to access and read."

## Must stay true
- All current features: randomize, revert, copy placement, custom enemy configs, text config, PTDE + Remastered.
- Output files the game accepts; layout reference file format unchanged (old refs stay usable).

## Target versions
| Component | Version | Checked |
|---|---|---|
| .NET SDK / runtime | 10.0 (LTS) | not checked online; local machine has SDK 9.0.200 only |
