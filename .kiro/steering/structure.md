# Project Structure

## Root Layout
```
Assets/                  # All game content
Packages/                # Unity package manifest
ProjectSettings/         # Unity project settings
Assembly-CSharp.csproj   # Main C# project
DoAnGame.sln             # Visual Studio solution
IMPLEMENTATION_SUMMARY.md
```

## Assets/ Breakdown
```
Assets/
├── Script/                          # All C# game logic
│   ├── MathManager.cs               # Question generation & answer validation
│   ├── LevelManager.cs              # Level progression, wave-layout UI
│   ├── LevelGenerate.cs             # Level config data (ScriptableObject)
│   ├── DragAndDrop.cs / DragQuizManager.cs  # Drag-quiz mechanics
│   ├── Script_multiplayer/          # Multiplayer, auth, and UI systems
│   │   ├── AuthManager.cs           # Auth orchestration (singleton)
│   │   ├── FirebaseManager.cs       # Firebase Auth + Database ops
│   │   ├── FirebaseInit.cs          # Firebase SDK initialization
│   │   ├── RelayManager.cs          # Unity Relay connection handling
│   │   ├── PlayerMovement.cs        # Networked player sync
│   │   ├── UIManager.cs             # Global UI state (SelectedGrade, etc.)
│   │   └── 1Code/CODE/              # 60+ UI controllers & services
│   │       ├── UserValidationService.cs
│   │       ├── SessionManager.cs
│   │       ├── PlayerDataService.cs
│   │       ├── UILoadingIndicator.cs
│   │       ├── GameRecord.cs
│   │       ├── BasePanelController.cs
│   │       ├── FlowPanelController.cs
│   │       ├── UIScreenRouter.cs
│   │       ├── UIAuthPanelController.cs
│   │       ├── UILoginPanelController.cs
│   │       ├── UIRegisterPanelController.cs
│   │       ├── UIMainMenuController.cs
│   │       └── [40+ more UI controllers]
│   └── Script_Space/                # Spaceship mini-game
│       ├── SpaceShipPhysics.cs
│       ├── SpaceShipManager.cs
│       └── QuestionZone.cs
├── Scenes/                          # Unity scene files
│   ├── ChonDA.unity                 # Level selection
│   ├── KeoThaDA.unity               # Rope/drag game
│   ├── PhiThuyen.unity              # Spaceship game
│   ├── GameUIPlay 1.unity           # Main gameplay
│   ├── Lop1.unity                   # Grade 1 scene
│   └── Test_FireBase_multi.unity    # Firebase/multiplayer test scene
├── Prefabs/                         # Reusable GameObjects
├── Animation/                       # Animator controllers & clips
├── Firebase/                        # Firebase Unity SDK (do not modify)
│   ├── Editor/                      # Dependency XML files
│   ├── Plugins/                     # Firebase DLLs
│   └── m2repository/                # Android Maven repo
├── ExternalDependencyManager/       # Google EDM4U (do not modify)
├── Resources/                       # Runtime-loaded assets
├── Settings/                        # URP renderer assets
├── TaiNguyen/                       # UI sprites and art assets
└── TextMesh Pro/                    # TMP fonts and shaders
```

## Namespaces
| Namespace | Scope |
|---|---|
| `DoAnGame.Auth` | Authentication services (UserValidationService, SessionManager, PlayerDataService) |
| `DoAnGame.UI` | UI controllers and shared UI utilities (UILoadingIndicator, BasePanelController) |
| `DoAnGame.Firebase` | Firebase wrapper operations |
| *(global)* | Legacy game logic — MathManager, LevelManager, DragQuizManager |

New code should use the appropriate `DoAnGame.*` namespace. Avoid adding to the global namespace.

## Architectural Patterns
- **Singleton MonoBehaviour** — all manager/service classes use `public static T Instance`. Singletons call `DontDestroyOnLoad` in `Awake`.
- **BasePanelController / FlowPanelController** — base classes for all UI panels; use `Show()` / `Hide()` for visibility.
- **UIScreenRouter** — central navigation; use it for panel transitions instead of direct `SetActive` calls.
- **Events** — use `System.Action<T>` delegates on managers for UI callbacks (e.g., `OnLoginDataLoaded`, `OnErrorOccurred`).
- **Async/Await** — all Firebase and network calls are `async Task`; propagate async up the call chain.
- **PlayerDataService** — single source of truth for player data; reads from Firebase and caches locally via PlayerPrefs.

## Key Data Models
- `PlayerData` — uid, characterName, level, totalXp, totalScore, rank, gamesPlayed, gamesWon, winRate, lastUpdated
- `UserData` — Firebase auth user record with emailVerified, timestamps
- `GameRecord` — gameId, mode, difficulty, score, level, result, timestamp
- `ValidationResult` — result of input validation with error codes
- `LevelDataConfig` (ScriptableObject) — per-grade, per-level min/max numbers and allowed operators
