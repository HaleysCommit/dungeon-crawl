# Dungeon Crawl — Unity Client

![Dungeon Crawl splash screen](Assets/Art/Splash/dungeon_crawl_splash_screen.png)

Unity game client for **Dungeon Crawl**, built on **TopDown Engine** (More Mountains) for
gameplay, with wallet connect and onchain reward tracking against **Robinhood Chain
Testnet** (chain id `46630`). This is the primary game client; it talks to the same
backend REST API used by the project's web frontend — see the main
[dungeon_crawl](https://github.com/HaleysCommit/dungeon_crawl) repo for `contracts/` and
`backend/`.

## Requirements

- Unity 2022.3 LTS or newer, Gamma color space, IL2CPP code stripping set to Minimal or
  lower (Reown AppKit requirements).
- TopDown Engine (already imported under `Assets/TopDownEngine/`).

## Structure

- `Assets/Scenes/` — `Splash.unity` (title/menu) and `Dungeon.unity` (gameplay).
- `Assets/Scripts/Chain/` — onchain/session bridge:
  - `RobinhoodChainConfig.cs` — network constants (chain id, RPC, explorer).
  - `SessionApiClient.cs` — wrapper around the backend's `/session/start`,
    `/session/:id/event`, `/session/:id/end` endpoints.
  - `WalletConnectBootstrap.cs` — initializes Reown AppKit and exposes the connected
    wallet address.
  - `RunSessionBridge.cs` — holds the active session id; exposes `ReportKill`,
    `ReportLoot`, `ReportFloorClear`, `EndRun` for wiring to TopDown Engine events from
    the Inspector.
  - `RunEndTrigger.cs` / `PersistAcrossScenes.cs` — run lifecycle and cross-scene state
    helpers.
- `Assets/Scripts/UI/` — `DungeonSplashScreen.cs` for the splash/title screen.
- `Assets/Scripts/Editor/` — editor-only tooling (e.g. splash art setup).

## Setup

1. Open the project in Unity Hub.
2. Install the Reown AppKit Unity packages via the OpenUPM scoped registry
   (`https://package.openupm.com`, scopes `com.reown`, `com.nethereum`) — see
   [docs.reown.com/appkit/unity/core/installation](https://docs.reown.com/appkit/unity/core/installation).
3. Create a free project at [dashboard.reown.com](https://dashboard.reown.com) for a
   Project ID and set it on `WalletConnectBootstrap` in the Inspector.
4. Point `SessionApiClient` at your running backend (see the main repo's `backend/`
   setup instructions).
5. Open `Assets/Scenes/Splash.unity` and press Play.

## Credits

- "Snarling Goblin Fighter" (`Assets/Art/Goblin/`) is based on
  ["Snarling Goblin Fighter [RapidAssets]"](https://sketchfab.com/3d-models/snarling-goblin-fighter-rapidassets-a4f7392d59c14d27a8836888eae4769b)
  by [RapidAssets](https://sketchfab.com/RapidAssets), licensed under
  [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/).

