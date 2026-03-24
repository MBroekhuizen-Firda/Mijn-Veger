# Mijn-Veger – Project Overview

## What Is This Project?

**Mijn-Veger** (Dutch for "Mine-Sweeper") is a 3D reimagining of the classic Minesweeper game built in Unity with the Universal Render Pipeline (URP). Instead of a flat grid of squares, the player controls a character that physically walks across a 3D tile map. The tension mechanic is a dual-camera system: top-down for strategic tile selection, close-up follow camera as the character walks toward a chosen tile.

Full concept specification: [`docs/CONCEPT.md`](docs/CONCEPT.md)

---

## Game Rules (from CONCEPT.md)

- The playing area is a flat grid of 1 m × 1 m tiles, configurable from **10×10 to 100×100**.
- A **character** spawns on a random tile on the **left edge**; a **goal tile** spawns on a random tile on the **right edge**.
- There is always **at least one mine-free path** from start to goal.
- The player clicks a tile → the character walks there.
- Tiles adjacent to the player (including diagonals) can be **flagged**.
- When the character arrives on a tile:
  - **Mine tile** → mine becomes visible, explodes, player dies (game over).
  - **Clear tile** → all adjacent clear tiles are auto-revealed (flood-fill, like classic Minesweeper).
  - **Numbered tile** → shows count of adjacent mines.
- **Camera**: top-down while selecting a tile; switches to a close-up follow camera while the character is walking.
- **Character Animator parameters**:
  - `Speed` (float, 0–1): 0 = Idle, 1 = Walking
  - `Death` (bool): set to `true` to trigger death animation

---

## Unity Project Details

- **Engine**: Unity **6000.3.9f1** (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP) v17.3.0
- **Key packages** (`Packages/manifest.json`):
  - `com.unity.inputsystem` 1.18.0 – new Input System for click/keyboard input
  - `com.unity.ai.navigation` 2.0.10 – NavMesh for pathfinding
  - `com.unity.render-pipelines.universal` 17.3.0 – URP
  - `com.unity.ugui` 2.0.0 – UI Toolkit / uGUI
  - `com.unity.timeline` 1.8.10 – optional cinematic sequences
  - `com.unity.modules.particlesystem` – mine explosion VFX

---

## Repository Structure

### Assets

| Path | Description |
|------|-------------|
| `Assets/Scripts/` | All 9 gameplay scripts (see Architecture section below) |
| `Assets/Prefabs/` | `TilePrefab.prefab`, `Character.prefab`, `Flag.prefab`, `Goal.prefab` |
| `Assets/Character/` | Character FBX models (`IdleSkinned.fbx`, `Walk.fbx`, `Die.fbx`), textures, materials, `AnimatorController.controller` |
| `Assets/Mine/` | `Mijn.fbx`, `MijnPrefab.prefab`, textures, materials (`Mine.mat`, `MineLamp.mat`) |
| `Assets/TerrainTexture/` | Tile/ground material (`Terrain.mat`) and PBR textures |
| `Assets/Materials/` | Additional materials: `Flag.mat`, `Goal.mat`, `Pole.mat` |
| `Assets/Scenes/SampleScene.unity` | Main (and only) scene – fully configured with all GameObjects and references wired |
| `Assets/Settings/` | URP renderer and render-pipeline assets for PC and Mobile, volume profiles |
| `Assets/InputSystem_Actions.inputactions` | Input System action map (Player actions: Move, Look, Attack, etc.) |

### Other

| Path | Description |
|------|-------------|
| `docs/CONCEPT.md` | Game design specification |
| `Packages/manifest.json` | Unity package dependencies |
| `ProjectSettings/` | Unity project settings |

---

## Architecture – Implemented Systems

All scripts are in `Assets/Scripts/` with no namespace (flat structure).

### 1. `GameManager.cs` – Game State Singleton
- States: `Setup`, `Playing`, `GameOver`, `Win`
- Holds grid size settings (width, height) and mine density
- Triggers `GridGenerator` to build the grid on game start
- Listens for player death → `GameOver`, player reaching goal → `Win`
- Tracks flag count; controls UI overlays via `UIManager`

### 2. `GridGenerator.cs` – Grid Builder
- Instantiates `TilePrefab` in a W×H grid (1 m spacing)
- Randomly distributes mines (~15–20% density)
- **Guarantees a mine-free path** from left edge to right edge using BFS; removes blocking mines if needed
- Calculates `AdjacentMineCount` for every non-mine tile
- Spawns character on a random left-edge tile; marks a random right-edge tile as the goal
- Sets up mine prefabs on mine tiles
- Bakes NavMesh at runtime
- Exposes `Tile[,] Grid` for coordinate lookups

### 3. `Tile.cs` – Tile Data & Visuals (MonoBehaviour)
- Properties: `IsMine`, `IsRevealed`, `IsFlagged`, `IsGoal`, `AdjacentMineCount`, `GridPosition`
- Visual states via material swapping: hidden (default), revealed clear, revealed numbered (TextMeshPro), revealed mine, flagged
- `Reveal()` – flips state, triggers flood-fill via `GridGenerator` when `AdjacentMineCount == 0`
- `Flag()` / `Unflag()` – toggles flag child object
- `TriggerMine()` – shows mine prefab, triggers explosion, notifies `GameManager`

### 4. `PlayerController.cs` – Input Handler
- Uses Input System (`Mouse.current`) for left/right click
- Left-click: raycasts from camera to tile layer → moves character to target tile
- Right-click: flags/unflags adjacent tiles only
- Blocks input while character is moving or game is not in `Playing` state
- On arrival: calls `tile.Reveal()`

### 5. `CharacterMover.cs` – NavMesh Movement
- Uses `NavMeshAgent` to move character to target tile position
- Drives animator: `Speed = 1` when moving, `Speed = 0` when stopped
- Events: `OnStartedMoving`, `OnArrived` (used by camera and player controller)
- Stops movement on mine trigger

### 6. `CharacterAnimator.cs` – Animator Wrapper
- `SetSpeed(float v)` → `animator.SetFloat("Speed", v)`
- `TriggerDeath()` → `animator.SetBool("Death", true)`
- Uses existing `AnimatorController.controller` with `Speed` (float) and `Death` (bool) parameters

### 7. `CameraController.cs` – Dual Camera System
- **TopDown mode**: high-angle perspective, full grid visible, active while player is idle (height scales with grid size)
- **Follow mode**: third-person close-up behind/above character, active while walking
- Switches triggered by `CharacterMover.OnStartedMoving` / `OnArrived`
- Smooth blending via Lerp/Slerp

### 8. `UIManager.cs` – UI Panels
- **Setup screen**: width/height sliders (10–100), "Start Game" button
- **HUD**: mine counter, flag counter
- **Game Over overlay**: "Retry" button
- **Win overlay**: "Play Again" button
- Communicates with `GameManager` for state transitions

### 9. `MineExplosion.cs` – Mine Reveal VFX
- Enables `MijnPrefab` child object on the tile
- Creates orange particle explosion effect at runtime
- Triggers character death animation via `CharacterAnimator.TriggerDeath()` after delay
- Notifies `GameManager` of player death

---

## Scene Setup (`SampleScene.unity`)

The scene is fully configured with:
- **GameManager** GameObject – singleton with references to GridGenerator, UIManager, PlayerController, CameraController
- **GridGenerator** – with prefab references and material assignments
- **PlayerController** – with main camera reference and tile layer mask
- **CameraController** – with target assignment
- **UIManager** – with all panel references and button wiring
- **Main Camera** – configured for raycasting

---

## Implementation Status

### Complete
- [x] Project setup – scripts directory, prefabs, NavMesh
- [x] GridGenerator with configurable width/height and guaranteed-path algorithm
- [x] Tile logic with all states, reveal, flood-fill, flagging, mine trigger
- [x] Character prefab with NavMeshAgent, CharacterMover, CharacterAnimator
- [x] Player input (left-click to move, right-click to flag, input blocking)
- [x] Camera system with top-down and follow modes
- [x] GameManager state machine with full game loop
- [x] UIManager with setup, HUD, game-over, and win screens
- [x] Mine reveal sequence with particle explosion and death animation
- [x] Scene fully wired with all references

### Remaining Polish (optional)
- [ ] Sound effects (Unity Audio module is available)
- [ ] Camera shake on explosion
- [ ] Win/loss transition animations
- [ ] Performance testing on large grid sizes (50×50, 100×100)
- [ ] Gameplay balance tuning (mine density, grid size defaults)

---

## Key Conventions

- **All scripts** → `Assets/Scripts/`
- **All prefabs** → `Assets/Prefabs/`
- **Scene** → `Assets/Scenes/SampleScene.unity`
- **Tile layer** → used for raycasts in `PlayerController`
- **NavMesh** → baked at runtime by `GridGenerator`; character uses `NavMeshAgent`
- **URP materials** → URP/Lit shader; materials in `Assets/TerrainTexture/` and `Assets/Mine/` are URP-compatible
- **Input** → `Mouse.current` from Input System for left/right click handling

---

## Verification Checklist

| Feature | How to test |
|---------|-------------|
| Grid generation | Enter Play mode; verify W×H tiles appear, mine count matches setting |
| Safe path guarantee | Add debug gizmo in `GridGenerator` drawing the guaranteed path |
| Tile reveal / flood-fill | Click a clear tile; adjacent clear tiles should auto-reveal |
| Flagging | Right-click adjacent tile; flag icon appears; right-click again to unflag |
| Character movement | Click a tile; character walks there using NavMesh |
| Animator | Observe `Speed` and `Death` params in Animator window during play |
| Camera switch | Watch camera switch to follow mode on movement, back to top-down on arrival |
| Mine detonation | Walk onto a mine tile; mine appears, explodes, death animation plays, game-over overlay shown |
| Win condition | Walk character to goal tile; win overlay appears |
| UI settings | Change grid size in setup screen; new game generates correct grid |
