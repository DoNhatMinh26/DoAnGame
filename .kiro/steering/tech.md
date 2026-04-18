# Tech Stack

## Engine & Language
- **Unity 2022.3.62f3** (LTS)
- **C# 9.0**, targeting `netstandard2.1`
- **Universal Render Pipeline (URP) 14.0.12**

## Key Packages (Packages/manifest.json)
| Package | Version | Purpose |
|---|---|---|
| com.unity.netcode.gameobjects | 1.12.2 | Multiplayer (NGO) |
| com.unity.transport | 2.7.2 | Network transport |
| com.unity.services.relay | 1.2.0 | Relay for P2P connections |
| com.unity.services.lobby | 1.3.0 | Matchmaking lobbies |
| com.unity.textmeshpro | 3.0.7 | All in-game text |
| com.unity.cinemachine | 2.10.7 | Camera control |
| com.unity.timeline | 1.7.7 | Cutscene/animation |
| com.unity.test-framework | 1.1.33 | Unit testing |
| com.unity.visualscripting | 1.9.4 | Visual scripting |
| com.veriorpies.parrelsync | git | Multi-client local testing |

## Firebase SDK (v13.9.0)
- **Firebase Authentication** — email/password login & registration
- **Firebase Realtime Database** — player data, game history, leaderboards
- **Firebase Installations** — device identification
- Android: `firebase-common:22.0.1`, `firebase-analytics:23.0.0`
- iOS: `Firebase/Core 12.10.0`

## IDE Support
- JetBrains Rider (`com.unity.ide.rider 3.0.36`)
- Visual Studio (`com.unity.ide.visualstudio 2.0.22`)

## Build System
Unity's standard build pipeline. No custom build scripts or CI/CD configuration exists.

- **Solution file**: `DoAnGame.sln`
- **Main assembly**: `Assembly-CSharp.csproj`
- **Editor assembly**: `Assembly-CSharp-Editor.csproj`

## Common Commands

All commands are run from within the **Unity Editor** — there is no CLI build script.

| Task | How |
|---|---|
| Build for Android | File → Build Settings → Android → Build |
| Run Play Mode tests | Window → General → Test Runner → Play Mode |
| Run Edit Mode tests | Window → General → Test Runner → Edit Mode |
| Open Package Manager | Window → Package Manager |
| Reload Firebase deps | Assets → External Dependency Manager → Android Resolver → Resolve |
| Multi-client testing | Use ParrelSync: ParrelSync → Clone Manager |

## Async Pattern
Firebase and networking operations use `async Task` / `await`. Always propagate async up the call chain; avoid `.Result` or `.Wait()` on Tasks.
