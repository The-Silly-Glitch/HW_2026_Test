# Run Doofus Game

Assignment submission for the **Hitwicket Game Developer Assignment (VIT 2026)**.

This is a Crossy Road-style survival game where you must help Doofus (the slime)
survive by crossing an endless series of disappearing platforms called "Pulpits".


**Engine:** Unity 6.3 (6000.3.21f1), Universal 3D

## Gameplay preview

| Start | In-game | Paused | Game Over |
|---|---|---|---|
| ![Start Screen](Files/1.png) | ![Gameplay](Files/2.png) | ![Pause Menu](Files/3.png) | ![Game Over](Files/4.png) |

🎥 **Full gameplay recording:** [`Files/submission.mp4`](Files/submission.mp4)

---

## How to run

1. Open the project folder in Unity Hub (Unity 6+).
2. Open the main scene under `Assets/Scenes/`.
3. Press Play. Click **Start**, then use **WASD / Arrow keys** to move.
4. Press **Esc** at any time to pause/resume.

A standalone build can also be produced via `File → Build Settings → Build`
(see `GUIDE.md` for the full walkthrough).

---

## Controls

| Input | Action |
|---|---|
| `W` `A` `S` `D` or Arrow keys | Move Doofus |
| `Esc` | Pause / Resume |

Movement only — no jump, by design (matches the brief exactly).

---

## Implemented Features

### Level 1 – Movement + JSON-driven Pulpit placement
- Doofus' movement occurs continuously in 4 directions with the speed
  fully taken from `doofus_diary.json` – no gameplay elements are
  hardcoded at all.
- There are always **2 Pulpits** only. The next one spawns nearby the
  current one when its timer reaches the JSON-defined threshold and it
  can never respawn back on top of previous Pulpits (including current).
- Doofus' sprite changes its orientation to match the direction of
  movement.
### Level 2 – Scoring
- +1 point per each newly landed **Pulpit**.
- The scoring system protects from accidental double scoring on the
  same Pulpit from standing still on it over several frames.

### Level 3 – Start Screen + Game Over Screen
- Menu flow complete: **Start → Playing → (Pause) → Game Over → Restart**.
- **Exit** option available from the Start, Pause, and Game Over menus.

### Further polishing beyond the brief
- **Pause/Resume**: tapping `Esc` pauses everything (physics, Pulpit timers,
  and input) and displays a Resume button.
- **3 Lives + fair respawn system**: falling off causes loss of a life rather than an instant game-over. The character gets teleported up to the top of the sky and lands right into the middle of the **fresh** Pulpit; any outdated and expiring Pulpit is removed, and then the timer on the landing Pulpit is reset fully, making every respawn a fair one regardless of which Pulpit Doofus fell from.
- **Following camera**: smooth third-person camera following Doofus.
- **Animated character**: Doofus is an animated, rigged slime character (Idle / Idle-break / Move).

---

## Config-driven design

All gameplay tuning values live in `Assets/Resources/doofus_diary.json`:
```json
{
  "doofusSpeed": 5.0,
  "minPulpitLifetime": 3.0,
  "maxPulpitLifetime": 6.0,
  "spawnThresholdSeconds": 1.5
}
```
No gameplay numbers are hardcoded anywhere in the scripts — changing this
file changes the game's behavior without touching a single line of code.
`DoofusDiaryLoader` also validates and sanitizes the file (falls back to
safe defaults on missing/malformed/negative values) rather than crashing.

---

## Architecture

| Script | Responsibility |
|---|---|
| `DoofusDiary.cs` | Loads + validates the JSON config |
| `GameManager.cs` | Owns game state (Start/Playing/Paused/Respawning/GameOver), score, lives |
| `DoofusController.cs` | Player movement, facing rotation, fall detection |
| `SlimeAnimationController.cs` | Drives the Idle/Move/Idle-break animation states |
| `Pulpit.cs` | One platform's countdown, freeze/reset, and scoring/landing trigger |
| `PulpitSpawner.cs` | Spawns/limits Pulpits, adjacency placement, respawn-target selection |
| `CameraFollow.cs` | Smooth third-person camera tracking |
| `UIManager.cs` | Start / HUD / Pause / Game Over screens |

Each script has only one responsibility to accomplish using a few public methods of communication, not by accessing each other’s inner workings. This design is done in a purposefully modular way, which allows for each component to be modified independently from the others (for example, the spawning rule or animations).
---

## Design decisions and assumptions

Some items in the brief were a bit unclear. Documented below so the rationale
is clear, instead of left in a comment somewhere:

- **"x is a random number between y and z seconds"** - taken to mean that
  there was a set, configurable threshold (`spawnThresholdSeconds`), not
  a random one every time. This makes it more consistent and predictable.
  Still totally configurable.
- **"Not in the same position as the previous one"** - taken to mean the
  stronger version: no Pulpit can spawn in the same place as any previously
  spawned Pulpit in the series (not just the currently active one).
- **Respawning onto the correct Pulpit** (own idea, not in the original
  brief) - if there are two Pulpits and Doofus dies, he will respawn onto the
  **most recent one**, while the other is forcefully cleared out. Respawning
  randomly could lead to respawn onto a Pulpit about to expire, which doesn't
  seem fair.

---

## Known edge cases handled

- Invalid JSON values default to safe values and log warnings without crashing.
- A Pulpit with its “spawn next” request issued when both concurrent slots
  are busy will be added to the queue and satisfied immediately once a slot
  becomes available, rather than ignored (a previous race-condition issue,
  now fixed).
- Falling causes loss of life and respawn if lives exist, else Game Over at 0 lives.
- Transitions between respawn, Game Over, and Pause states cannot be triggered multiple times.

---

## Repo structure

```
HW_2026_Test/
├── Assets/
│   ├── Scripts/           # all C# scripts listed above
│   ├── Resources/
│   │   └── doofus_diary.json
│   ├── Prefabs/
│   └── Scenes/
├── Files/
│   ├── submission.mp4     # full gameplay recording
│   ├── 1.png              # start screen
│   ├── 2.png              # gameplay
│   ├── 3.png              # pause menu
│   └── 4.png              # game over
├── GUIDE.md                # full build walkthrough
└── README.md                # this file
```
