# 🕵️‍♂️ ShadowHeist (`Raylib-cs`)

A **Gritty 2D Top-Down Tactical Stealth Heist** game built in C# (`.NET 8` + `Raylib-cs`) following **SOLID Architecture, Design Patterns, and Strict GitHub Pull Request Workflows**.

📄 **Full 5-Page Engineering & Sprint Plan:**
* [docs/ShadowHeist_Team_Engineering_Plan.pdf](docs/ShadowHeist_Team_Engineering_Plan.pdf)
* [docs/ARCHITECTURE_AND_PLAN.md](docs/ARCHITECTURE_AND_PLAN.md)

---

## 🎮 Default Controls

| Action | Control Key | Description |
| :--- | :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` / Arrow Keys | Top-down directional movement |
| **Aim / Orient** | Mouse Cursor | Smooth 360° mouse-aim rotation |
| **Crouch / Sneak** | `Left Ctrl` (Toggle) | Lowers footstep noise and speed |
| **Sprint** | `Left Shift` (Hold) | Increases speed, creates loud noise |
| **Interact / Hold** | `E` | Open vaults, loot crates, bypass keypads |
| **Use Gadget** | `Q` / `Space` / Right Click | Throws coin / EMP grenade / fires pistol |

---

## 👥 Team Subsystem Ownership

To prevent merge conflicts and enforce clean separation of concerns, each engineer owns **one dedicated C# namespace and directory**:

| Engineer | Owned Module | Key Responsibilities |
| :--- | :--- | :--- |
| **Guindy** | `src/Core/` & `src/HeistWorld/` | `GameContext`, `GameEventBus`, `VaultDoor`, `KeypadTerminal`, `LootCrate`, `FuseBox`, `HudRenderer`, `GameStateManager` |
| **Mahmoud** | `src/PlayerSystem/` | `IInputProvider`, `Player`, Stance State Machine (`SneakStance`, `WalkStance`, `SprintStance`), `PlayerInventory` (LINQ), `IGadget` (`CoinGadget`, `EmpGadget`, `SuppressedPistol`) |
| **Hatem** | `src/GuardAI/` | `Guard`, AI State Machine (`PatrolState`, `SuspiciousState`, `AlertChaseState`), `SecurityCamera`, Detection Meter, 2D Grid `AStarPathfinder` |
| **Ali** | `src/WorldLighting/` | JSON `MapLoader`, `CollisionHelper`, `VisionRaycaster`, `LaserTripwire`, `BrokenGlassTile`, `ShadowRenderer`, GLSL Bloom Shader |

---

## 🚀 Quick Start (Build, Run & Test)

```bash
# 1. Build the solution
dotnet build ShadowHeist.sln

# 2. Run the game
dotnet run --project src/ShadowHeist.csproj

# 3. Run automated xUnit tests
dotnet test ShadowHeist.sln
```

---

## 🌿 Daily Git & Pull Request Routine

**Direct pushes to `main` are disabled.** Before starting any ticket, run these 5 commands:

```bash
# 1. Switch to main & pull latest merged team code
git checkout main
git pull origin main

# 2. Create a fresh branch for ONE ticket (<type>/<owner>-<feature>)
git checkout -b feat/mahmoud-player-stances

# 3. Verify build & tests pass locally
dotnet build && dotnet test

# 4. Stage & commit with a Conventional Commit message
git add .
git commit -m "feat(player): implement SneakStance and SprintStance"

# 5. Push branch & open a Pull Request on GitHub
git push -u origin feat/mahmoud-player-stances
```

### Mandatory Peer Review Matrix (2 Approvals Required per PR)

| PR Author | Primary Reviewer | Secondary Reviewer |
| :--- | :--- | :--- |
| **Mahmoud** | Hatem | Guindy |
| **Hatem** | Guindy | Ali |
| **Ali** | Mahmoud | Guindy |
| **Guindy** | Hatem | Ali |
