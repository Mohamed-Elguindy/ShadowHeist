# 🕵️‍♂️ ShadowHeist (`Raylib-cs`)

A **Native 3D Isometric Tactical Stealth Heist** game built in C# (`.NET 8` + `Raylib-cs`) featuring cel-shaded 3D visuals, A* guard pathfinding, wall-clipped vision raycasting, and strict **SOLID Clean Code Architecture (<200 lines per source file)**.

📄 **Engineering & Sprint Plan Docs:**
* [docs/ShadowHeist_Team_Engineering_Plan.pdf](docs/ShadowHeist_Team_Engineering_Plan.pdf)
* [docs/ARCHITECTURE_AND_PLAN.md](docs/ARCHITECTURE_AND_PLAN.md)

---

## 🎮 Default Controls

| Action | Control Key | Description |
| :--- | :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` / Arrow Keys | Directional movement on the bank floor plane |
| **Aim / Orient** | Mouse Cursor | 3D ground-plane raycast aiming (`ScreenToGroundPoint`) |
| **Tactical Peek** | `Right Mouse Button` (Hold) | Pan camera smoothly ahead in the aim direction |
| **Crouch / Sneak** | `Left Ctrl` (Toggle) | Lowers footstep noise radius and movement speed |
| **Sprint** | `Left Shift` (Hold) | Increases speed and emits wider acoustic ripples |
| **Interact / Hack** | `E` (Press / Hold) | Unlock keypad terminals, grab vault loot, cut power, toggle lasers, open vault door |
| **Camera View** | `F6` | Toggle between smooth Isometric Follow and Tactical Overview |
| **Camera Zoom** | `Mouse Scroll Wheel` | Smoothly zoom camera in and out |
| **Fullscreen** | `F11` | Toggle borderless fullscreen window |
| **Pause / Menus** | `Esc` / `Enter` / `L` | Pause mission (`Esc`), start or pick level (`Enter` / `L`) |
| **Restart Mission** | `R` | Reset heist level, guards, interactables, alarms, and camera |
| **Debug Noise Pulse** | `Left Mouse Click` | Emits an acoustic test noise pulse on the clicked 3D floor point |

---

## 🏆 Mission Summary & Stealth Rank (`S / A / B / C`)

At mission end (`Escaped` or `Caught`), `SummaryScreen` displays elapsed time, stolen loot progress, total alarms triggered, and your Stealth Rank:
* **Rank `S` (`SHADOW MASTER`):** Escaped with full `$500,000` loot, `0` alarms, and time ≤ `01:30.0`
* **Rank `A` (`SILENT PROFESSIONAL`):** Escaped with full loot and `0` alarms (over `01:30.0`), or `1` alarm in ≤ `01:30.0`
* **Rank `B` (`LOUD GETAWAY`):** Escaped with full loot despite multiple or slow alarms
* **Rank `C` (`COMPROMISED`):** Caught by security or escaped under target loot

---

## 👥 Team Subsystem Ownership

To prevent merge conflicts and enforce clean separation of concerns, each engineer owns **one dedicated C# namespace and directory**:

| Engineer | Owned Module | Key Responsibilities |
| :--- | :--- | :--- |
| **Guindy** | `src/Core/` & `src/HeistWorld/` | `GameContext`, `GameEventBus`, `Core/Rendering3D/` (`TacticalCameraController`, `ToonModelLibrary`, `CharacterMeshDrawer`), `VaultDoor`, `KeypadTerminal`, `LootCrate`, `FuseBox`, `LaserSwitch`, `HeistWorldRenderer`, `HudRenderer`, `SummaryScreen`, `GameStateManager` |
| **Mahmoud** | `src/PlayerSystem/` | `IInputProvider`, `Player`, Stance FSM (`SneakStance`, `WalkStance`, `SprintStance`), `PlayerRenderer`, `PlayerSoundRippleRenderer`, `PlayerInventory` (LINQ) & `IGadget` (`CoinGadget`, `EmpGadget`, `SuppressedPistol`) |
| **Hatem** | `src/GuardAI/` | `Guard`, FSM (`PatrolState`, `SuspiciousState`, `AlertChaseState`), `IGuardPathState`, `GuardSensorHelper`, `AStarPathfinder` & `GridGraph`, `GuardRenderer`, `GuardPathGizmo3DRenderer`, `SecurityCamera` & Detection Meter |
| **Ali** | `src/WorldLighting/` | JSON `MapLoader`, `CollisionHelper`, `VisionRaycaster`, `LaserTripwire`, 3D renderers (`Architecture3DRenderer`, `Furniture3DRenderer`, `BankDecorRenderer`, `LaserTripwire3DRenderer`, `VisionCone3DRenderer`), lighting/glass hazards |

---

## 🚀 Quick Start (Build, Run & Test)

```bash
# 1. Build the solution (0 warnings, 0 errors)
dotnet build ShadowHeist.sln

# 2. Run the 3D game
dotnet run --project src/ShadowHeist.csproj

# 3. Run all headless xUnit tests
dotnet test ShadowHeist.sln
```

---

## 🌿 Daily Git & Pull Request Routine

**Direct pushes to `main` are disabled.** Before starting any ticket, run:

```bash
# 1. Switch to main & pull latest merged team code
git checkout main
git pull origin main

# 2. Create a fresh branch for ONE ticket (<type>/<owner>-<feature>)
git checkout -b feat/mahmoud-inventory-linq

# 3. Verify build & tests pass locally
dotnet build && dotnet test

# 4. Stage & commit with a Conventional Commit message
git add .
git commit -m "feat(player): implement PlayerInventory with LINQ weight calculation"

# 5. Push branch & open a Pull Request on GitHub
git push -u origin feat/mahmoud-inventory-linq
```

### Mandatory Peer Review Matrix (2 Approvals Required per PR)

| PR Author | Primary Reviewer | Secondary Reviewer |
| :--- | :--- | :--- |
| **Mahmoud** | Hatem | Guindy |
| **Hatem** | Guindy | Ali |
| **Ali** | Mahmoud | Guindy |
| **Guindy** | Hatem | Ali |
