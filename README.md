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
| **Crouch / Sneak** | `Left Ctrl` (Toggle) | Lowers footstep noise radius and movement speed |
| **Sprint** | `Left Shift` (Hold) | Increases speed and emits wider acoustic ripples |
| **Interact / Hack** | `E` (Press / Hold) | Unlock keypad terminals, grab vault loot, open vault door |
| **Camera View** | `F6` | Toggle between smooth Isometric Follow and Tactical Overview |
| **Camera Zoom** | `Mouse Scroll Wheel` | Smoothly zoom camera in and out |
| **Restart Mission** | `R` | Reset heist level, guards, interactables, and camera |
| **Debug Noise Pulse** | `Left Mouse Click` | Emits an acoustic test noise pulse on the clicked 3D floor point |

---

## 👥 Team Subsystem Ownership

To prevent merge conflicts and enforce clean separation of concerns, each engineer owns **one dedicated C# namespace and directory**:

| Engineer | Owned Module | Key Responsibilities |
| :--- | :--- | :--- |
| **Guindy** | `src/Core/` & `src/HeistWorld/` | `GameContext`, `GameEventBus`, `Core/Rendering3D/` (`TacticalCameraController`, `ToonModelLibrary`, `CharacterMeshDrawer`), `VaultDoor`, `KeypadTerminal`, `LootCrate`, `HeistWorldRenderer`, unified `HudRenderer`, upcoming `FuseBox` & `GameStateManager` |
| **Mahmoud** | `src/PlayerSystem/` | `IInputProvider`, `Player`, Stance FSM (`SneakStance`, `WalkStance`, `SprintStance`), `PlayerRenderer`, `PlayerSoundRippleRenderer`, upcoming `PlayerInventory` (LINQ) & `IGadget` (`CoinGadget`, `EmpGadget`, `SuppressedPistol`) |
| **Hatem** | `src/GuardAI/` | `Guard`, FSM (`PatrolState`, `SuspiciousState`, `AlertChaseState`), `IGuardPathState`, `GuardSensorHelper`, `AStarPathfinder` & `GridGraph`, `GuardRenderer`, `GuardPathGizmo3DRenderer`, upcoming `SecurityCamera` & Detection Meter |
| **Ali** | `src/WorldLighting/` | JSON `MapLoader`, `CollisionHelper`, `VisionRaycaster`, `LaserTripwire`, 3D renderers (`Architecture3DRenderer`, `Furniture3DRenderer`, `BankDecorRenderer`, `LaserTripwire3DRenderer`, `VisionCone3DRenderer`), upcoming lighting/glass hazards |

---

## 🚀 Quick Start (Build, Run & Test)

```bash
# 1. Build the solution (0 warnings, 0 errors)
dotnet build ShadowHeist.sln

# 2. Run the 3D game
dotnet run --project src/ShadowHeist.csproj

# 3. Run all 97 headless xUnit tests
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
