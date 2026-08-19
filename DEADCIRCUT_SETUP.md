# DEADCIRCUT foundation

This branch contains the first playable foundation for the game: Story and Survival mode flow, FishNet multiplayer scaffolding, X-Saw combat with diegetic timing warnings, downed/revive state, and a stalking monster brain.

## Setup

1. Open the project in Unity 2022.3+ or Unity 6.
2. Let Package Manager resolve FishNet from `https://github.com/FirstGearGames/FishNet.git?path=Assets/FishNet`. FishNet currently supports Unity 2022.3+ and the official project publishes this Package Manager URL. 
3. Import `GorillaLocomotion (1).unitypackage` from the repository. Keep that locomotion as the player movement base; do not replace it with a new joystick/gyro system.
4. In Unity, use **Dead Circuit > Build Foundation Scenes**.
5. Open `Assets/DeadCircuit/Scenes/MainMenu.unity` and add/configure your NetworkManager and FishNet transport.
6. Turn your existing locomotion player into a FishNet network prefab by adding `NetworkObject`, `DeadCircuitPlayer`, and `XSawCombat`, then register it with the FishNet spawner.

## Multiplayer

The lobby controller supports host-and-local-client and direct-address joining. For internet play, use a FishNet transport that can reach a hosted server or relay. FishNet is server-authoritative by design and can also run a listen-server for development.

For local multiplayer testing, Unity's Multiplayer Play Mode package or ParrelSync can run multiple editor clients.

## X-Saw combat

There are intentionally **no button prompts**. A clash gives warning cues first, then a short reaction window. The player reacts from audio/visual timing alone. Successful reactions produce the execution hit; misses fail the clash.

## Models

The repository currently uses generated greybox geometry so the codebase is testable without depending on random third-party assets. Character/monster models can be dropped into the generated prefabs later without changing the gameplay architecture.
