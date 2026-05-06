# Unity JSON Save System Demo

A small Unity gameplay demo built around a production-style JSON save/load system.

The game itself is intentionally simple: control a UFO, abduct animals, update counters and continue from saved state. The focus of the project is the save architecture around that gameplay: modular state capture, versioned data, migrations, autosave, save slots, async quick save/load and corrupted-save recovery.

## Demo

GIF/video here.
[gif]

Screenshots here.
[image]

## Features

- JSON-based save/load
- Quick save and quick load
- Multiple save slots with screenshot thumbnails
- Auto-save with a dedicated autosave file
- Async quick save/load
- Save schema versioning
- Migration examples across save versions
- Corrupted save fallback using backup files
- Modular `ISaveable` architecture
- ScriptableObject-based static animal config
- Separation between static config and runtime save state
- Restore/reset tools for clearing generated saves

## What This Demo Saves

- Save schema version
- Player position and movement state
- Camera position
- Stopwatch timer value
- Captured animal counters
- Runtime animals in the scene
- Animal type, position, rotation, scale, and abduction state
- Save slot screenshots

## Architecture Overview

The save system is centered around `SaveLoadController`, which coordinates state capture, JSON serialization, disk IO, migration and restore flow. Gameplay systems do not write files directly. Instead, each save-aware component implements `ISaveable` and contributes its own part of the runtime state.

```txt
SaveLoadController
  - discovers ISaveable components
  - captures runtime state into SaveData
  - serializes SaveData to JSON
  - reads JSON from disk
  - runs migrations
  - restores state back into ISaveable components

SaveData
  - stores global save metadata
  - stores player, camera, timer, counters, and animals

SaveDataMigrator
  - upgrades older save files to the current schema version

SaveSlotsPanelController
  - manages slot UI
  - writes slot JSON files
  - captures slot screenshot thumbnails

GameSessionController
  - restarts the current scene
  - deletes quick saves, autosaves, slots, backups, and temp files
```

## Core Interface

```csharp
public interface ISaveable
{
    int SaveOrder { get; }
    void CaptureState(SaveData saveData);
    void RestoreState(SaveData saveData);
}
```

`SaveOrder` keeps restore order explicit when one system depends on another. For example, world objects can be restored before player interaction state is resumed.

## Save Versioning & Migration

Every save file stores a `schemaVersion`. The current save version is defined in `SaveDataMigrator`.

This project includes migration examples:

- `v1 -> v2`: adds camera save support
- `v2 -> v3`: adds animal abduction runtime state
- `v3 -> v4`: migrates fixed animal counters into flexible data-driven animal counter entries

This makes old save files compatible after data structures change, which is one of the most important requirements for a real save system.

## Corrupted Save Handling

The system protects save files with backup and temp-file handling.

When saving:

- the previous save is copied to a `.bak` file
- new JSON is written through a temporary `.tmp` file
- the final save and backup are updated after the write succeeds

When loading:

- the main `.json` file is attempted first
- if the JSON is missing, empty, unreadable, or corrupted, the `.bak` file is attempted
- if the backup loads successfully, the main file is restored from backup
- quick load can fall back to autosave if quick save cannot be recovered

This keeps failures contained and avoids crashing gameplay because of a malformed save file.

## Async Quick Save / Load

Quick save and quick load use async file IO so UI-triggered save operations do not block the main thread while reading or writing JSON.

Unity object access still stays on the main thread:

- `ISaveable.CaptureState` runs before background file writing
- JSON file reading runs in a background task
- `ISaveable.RestoreState` runs after the file is loaded, back on the Unity thread

Autosave and slot saves currently use the same save pipeline but remain synchronous for predictable UI and screenshot behavior.

## Static Config vs Runtime State

Animal definitions are stored as ScriptableObjects:

- animal display name
- icon
- prefab reference
- spawn weight

Save files store runtime state only:

- animal type
- transform
- scale
- abduction state
- captured counters

This keeps save files stable even when prefab references, icons, or spawn tuning change in the editor.

## How to Run

1. Clone the repository.
2. Open the project with Unity `6000.4.5f1` or newer.
3. Open scene: `Assets/_Project/_Scenes/Gameplay.unity`.
4. Press Play.

## Controls

- `WASD` / left stick: move the UFO
- `Space`: abduct an animal when in range
- UI buttons: quick save, quick load, autosave load, slot save/load, restart, restore saves

## Project Structure

```txt
Assets/_Project/
  Art/
  Materials/
  Prefabs/
    UI/
  Resources/
  ScriptableObjects/
  Scripts/
    ISaveable.cs
    SaveData.cs
    SaveDataMigrator.cs
    SaveLoadController.cs
    SaveSlotsPanelController.cs
    SaveSlotView.cs
    GameSessionController.cs
  _Scenes/
    Gameplay.unity
```

## Design Goals

- Keep gameplay systems decoupled from file IO
- Make save data readable and easy to debug
- Support future data structure changes through migrations
- Keep static config out of runtime save files
- Provide safe fallback behavior for corrupted saves
- Demonstrate portfolio-ready Unity architecture, not just a tutorial implementation

## Known Limitations

- No cloud save support
- No encryption or anti-tamper protection
- No compression for large save files
- No platform-specific save UI
- No automated migration test suite yet
- Autosave and slot save paths are intentionally local-only
- Demo-scale gameplay content

## Future Improvements

- Unit tests for migrations and corrupted-save recovery
- Optional save compression
- Optional save encryption
- Cloud save integration
- Addressables-friendly prefab restoration
- Richer slot metadata
- Save compatibility validation tools for CI

## Screenshots

Add 2-3 images here.

## License

MIT
