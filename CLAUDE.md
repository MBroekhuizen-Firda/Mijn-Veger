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

## Current State of the Repository

### What exists

| Path | Description |
|------|-------------|
| `Assets/Character/` | Character FBX models (`IdleSkinned.fbx`, `Walk.fbx`, `Die.fbx`), textures, materials, `AnimatorController.controller` |
| `Assets/Mine/MijnPrefab.prefab` | Mine 3D prefab (hidden until triggered) |
| `Assets/Mine/Mijn.fbx` + materials | Mine mesh and textures |
| `Assets/TerrainTexture/` | Tile/ground material (`Terrain.mat`) and PBR textures |
| `Assets/Scenes/SampleScene.unity` | Main (and only) scene |
| `Assets/Settings/` | URP renderer and render-pipeline assets for PC and Mobile |
| `Assets/InputSystem_Actions.inputactions` | Input System action map |

### What does NOT exist yet

- **No gameplay scripts** (`Assets/Scripts/` does not exist)
- No tile prefab or grid setup in the scene
- No game logic, UI, or camera control scripts

---

## Unity Project Details

- **Engine**: Unity (check `ProjectSettings/ProjectVersion.txt` for exact version)
- **Render Pipeline**: Universal Render Pipeline (URP) v17.3.0
- **Key packages** (`Packages/manifest.json`):
  - `com.unity.inputsystem` 1.18.0 – new Input System for click/keyboard input
  - `com.unity.ai.navigation` 2.0.10 – NavMesh for pathfinding
  - `com.unity.render-pipelines.universal` 17.3.0 – URP
  - `com.unity.ugui` 2.0.0 – UI Toolkit / uGUI
  - `com.unity.timeline` 1.8.10 – optional cinematic sequences
  - `com.unity.modules.particlesystem` – available for mine explosion VFX

---

## Architecture – Systems to Implement

All scripts go in `Assets/Scripts/`. Suggested namespace: none (keep flat for simplicity).

### 1. `GameManager.cs`
**Singleton** that owns overall game state.

Responsibilities:
- States: `Setup`, `Playing`, `GameOver`, `Win`
- Holds reference to grid size setting (width × height)
- Triggers `GridGenerator` to build the grid on game start
- Listens for player death → transition to `GameOver`
- Listens for player reaching goal tile → transition to `Win`
- Controls UI overlays via `UIManager`

### 2. `GridGenerator.cs`
Builds the tile grid at runtime.

Responsibilities:
- Instantiate `Tile` prefabs in a W×H grid (1 m spacing, world origin centered or at 0,0)
- Randomly distribute mines, ensuring the mine density is reasonable (e.g. 15–20 % of tiles)
- **Guarantee a mine-free path** from left edge to right edge (use BFS/DFS after mine placement; if no path exists, remove blocking mines)
- Assign `adjacentMineCount` to every non-mine tile
- Spawn character on a random left-edge tile; mark a random right-edge tile as the goal
- Expose a 2D array `Tile[,] Grid` for lookups by coordinate

### 3. `Tile.cs` (MonoBehaviour on each tile GameObject)
Data and visual state for one tile.

Properties:
- `bool IsMine`
- `bool IsRevealed`
- `bool IsFlagged`
- `int AdjacentMineCount`
- `Vector2Int GridPosition`

Visual states (swap materials or child objects):
- Hidden (default grey)
- Revealed – clear (terrain texture)
- Revealed – numbered (terrain texture + text/decal showing count)
- Revealed – mine (mine prefab becomes visible + explosion)
- Flagged (flag indicator)

Methods:
- `Reveal()` – flip state, trigger flood-fill via `GridGenerator` if count == 0
- `Flag()` / `Unflag()`
- `TriggerMine()` – show mine prefab, play explosion particle, notify `GameManager`

### 4. `PlayerController.cs`
Handles input and character movement.

Responsibilities:
- Listen for mouse click (via Input System `InputSystem_Actions`) → raycast against tile layer → get `Tile`
- Validate click: tile must be reachable (not already revealed mine, not out of range for flagging)
- Hand off movement target to `CharacterMover`
- After movement completes, call `tile.Reveal()`
- Flagging: right-click (or designated button) on adjacent tiles calls `tile.Flag()`

### 5. `CharacterMover.cs`
Moves the character to a target tile using NavMesh.

Responsibilities:
- Uses `NavMeshAgent` on the character GameObject
- Sets `destination` to the clicked tile's world position
- Drives `CharacterAnimator`: set `Speed = 1` when moving, `Speed = 0` when stopped
- Fires `OnArrived` event when agent reaches destination
- Fires `OnStartedMoving` event for camera switch

### 6. `CharacterAnimator.cs`
Thin wrapper around the `Animator` component.

Responsibilities:
- `SetSpeed(float v)` → `animator.SetFloat("Speed", v)`
- `TriggerDeath()` → `animator.SetBool("Death", true)`

Animator parameters (already set up in `Assets/Character/AnimatorController.controller`):
- `Speed` (float) – Idle ↔ Walk blend
- `Death` (bool) – transitions to die animation

### 7. `CameraController.cs`
Switches between two virtual cameras (or manually repositions one camera).

Responsibilities:
- **Top-down mode**: orthographic or high-angle perspective, full grid visible, active while player is idle
- **Follow mode**: third-person close-up behind/above character, active while character is walking
- Switch triggered by `CharacterMover.OnStartedMoving` / `OnArrived`
- Smooth blend between modes (Lerp or Cinemachine if added)

### 8. `UIManager.cs`
Handles all UI panels.

Responsibilities:
- **Setup screen**: slider/input for grid width and height (10–100), "Start Game" button
- **HUD**: mine counter, flag counter
- **Game Over overlay**: shown on player death, "Retry" button
- **Win overlay**: shown on reaching goal, "Play Again" button
- Communicates with `GameManager` for state transitions

### 9. `MineExplosion.cs` (optional, can be part of `Tile.cs`)
Manages mine reveal sequence.

Responsibilities:
- Enable the `MijnPrefab` child object on the tile
- Play a `ParticleSystem` explosion effect
- Optionally shake the camera
- Notify `GameManager` of player death after a short delay (animation time)

---

## Implementation Steps (in order)

### Step 1 – Project Setup
- [ ] Create `Assets/Scripts/` directory
- [ ] Create a `Tile` prefab: a flat 1 m × 1 m quad/plane with a `Tile.cs` component; add child objects for number text (TextMeshPro), mine prefab slot, flag object
- [ ] Bake NavMesh on a placeholder flat plane to verify AI Navigation works

### Step 2 – GridGenerator
- [ ] Implement `GridGenerator.cs` with configurable width/height
- [ ] Implement mine placement with the guaranteed-path algorithm (BFS after placement)
- [ ] Verify grid spawns correctly in the scene with correct world positions

### Step 3 – Tile Logic
- [ ] Implement `Tile.cs` with all states and `Reveal()` / `Flag()` / `TriggerMine()`
- [ ] Implement flood-fill (auto-reveal adjacent clear tiles) when `AdjacentMineCount == 0`
- [ ] Display adjacent mine count numbers on tiles (TextMeshPro or world-space canvas)

### Step 4 – Character Setup
- [ ] Create character prefab from existing FBX assets; attach `NavMeshAgent`, `CharacterMover.cs`, `CharacterAnimator.cs`
- [ ] Verify `AnimatorController.controller` transitions work with `Speed` and `Death` parameters
- [ ] Test NavMesh movement on the grid

### Step 5 – Player Input
- [ ] Implement `PlayerController.cs` using the existing `InputSystem_Actions` action map
- [ ] Raycast from camera to tile on left-click → move character
- [ ] Right-click on adjacent tile → flag/unflag
- [ ] Block input while character is moving

### Step 6 – Camera System
- [ ] Implement `CameraController.cs`
- [ ] Wire up to `CharacterMover` events
- [ ] Tune top-down height and follow distance

### Step 7 – Game State & UI
- [ ] Implement `GameManager.cs` singleton with state machine
- [ ] Implement `UIManager.cs` with setup, HUD, game-over and win screens
- [ ] Wire up death and win conditions end-to-end

### Step 8 – Mine Reveal & VFX
- [ ] Implement mine reveal sequence in `Tile.cs` / `MineExplosion.cs`
- [ ] Use the existing `MijnPrefab.prefab` and a `ParticleSystem` for the explosion
- [ ] Trigger character death animation via `CharacterAnimator.TriggerDeath()`

### Step 9 – Polish & Tuning
- [ ] Adjust mine density and grid size defaults
- [ ] Add sound effects (Unity Audio module is available)
- [ ] Camera shake on explosion
- [ ] Win/loss animation or transition
- [ ] Test on various grid sizes (10×10, 50×50, 100×100)

---

## Key Conventions

- **All scripts** → `Assets/Scripts/`
- **Scene** → `Assets/Scenes/SampleScene.unity`
- **Tile layer** → create a `Tile` layer in Unity for raycasts
- **NavMesh** → bake on the grid plane; character uses `NavMeshAgent`
- **URP materials** → use URP/Lit shader; existing materials in `Assets/TerrainTexture/` and `Assets/Mine/` are already URP-compatible
- **Input** → use the existing `Assets/InputSystem_Actions.inputactions`; add `Point` and `Click` (mouse) + optional `RightClick` actions if not present

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
