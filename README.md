# RougelikeUni: 2D Roguelike

**Assignment option: Option 1, 2D Roguelike Game.**

This is a turn-based 2D roguelike built in Unity, based on Unity Learn's 2D Roguelike course and extended to meet the Option 1 requirements.

## Controls

| Action | Key |
|---|---|
| Move | Arrow keys or WASD |
| Attack a wall or enemy | Move into it |
| Restart after game over | R or Enter |

## How to play

- You start each run with **20 food**. Every move or attack costs **1 food**.
- **Food** items restore food. Small food gives +10, big food gives +30.
- **Walls** block your path and take 3 hits to break.
- **Enemies** chase you each turn and attack when they're next to you:

  | Enemy | Health | Damage |
  |---|---|---|
  | Enemy | 3 | 3 food |
  | Enemy 2 | 5 | 15 food |

- Reach the **exit** in the top-right corner to move on to the next day. The board is generated randomly each time.
- The game ends when food reaches 0, and shows how many days you survived.

## Running the project in Unity

1. Clone the repository:
   ```bash
   git clone https://github.com/aalthukair/RougelikeUni.git
   ```
2. Open the project folder in **Unity Hub** with Unity **6000.6.0f1** (Unity 6.6).
   The project uses URP (2D) and the Input System package, which Unity installs automatically on first open.
3. Open the scene **`Assets/Scenes/Main.unity`**.
4. Press **Play**.

## Making a build

1. Go to **File → Build Profiles**.
2. Make sure **`Assets/Scenes/Main.unity`** is in the scene list and ticked.
3. Pick the target platform (Windows or Web), then click **Build**.

## Option 1 requirements

| Requirement | Where it's implemented |
|---|---|
| **Enemy object:** blocks the player, takes damage when bumped, and is removed at 0 HP | `Enemy.cs` (same pattern as `WallObject.cs`) |
| **Background music:** AudioSource on the Main Camera, looping at reduced volume | `MusicPlayer.cs` |
| **Sound effects with `PlayOneShot()`:** movement, wall attack, food pickup, enemy attack, enemy death, game over | `SoundManager.cs` |
| **Particle effects:** wall destruction, food collection, enemy death, controlled with `.Play()` / `.Stop()` and pooled | `EffectsManager.cs` |
| **Smooth movement:** a coroutine lerps the player between cells, and input is blocked while moving | `PlayerController.cs` |
| **Object pooling:** food, walls, and enemies are pre-instantiated at startup and recycled with `SetActive` on level change | `CellObjectPool.cs`, `BoardManager.cs` |
| **Code standards:** `[SerializeField]` private fields, `[Range]` on tuning values, `[Tooltip]` on Inspector fields | All scripts |

### Extras

- Two enemy types, chosen at random for each spawn.
- Animator-driven attack and hurt animations for the player, and walk and attack animations for enemies.
- Enemies move smoothly between cells, like the player.
- Restart key (R) on the game-over screen.

## Project structure

```
Assets/
├── Animation/   Animator controllers and clips (player, enemies)
├── Prefabs/     Food, wall, exit, enemies, particle effects
├── Scenes/      Main.unity (the game scene)
├── Scripts/     Gameplay code (see below)
└── Roguelike2D/ Tutorial art, audio, and font
```

| Script | Purpose |
|---|---|
| `GameManager.cs` | Game state, food, levels, game over |
| `BoardManager.cs` | Generates the board and places objects from the pool |
| `PlayerController.cs` | Input, smooth movement, player animations |
| `TurnManager.cs` | Turn tick event that drives enemies and food cost |
| `CellObject.cs` | Base class for anything that occupies a board cell |
| `WallObject.cs`, `FoodObject.cs`, `ExitCellObject.cs`, `Enemy.cs` | Board objects |
| `CellObjectPool.cs` | Object pool for board objects |
| `SoundManager.cs`, `MusicPlayer.cs` | Sound effects and background music |
| `EffectsManager.cs` | Pooled particle effects |
