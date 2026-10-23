## 🎯 Ticket & Summary
- **Ticket Number:** #
- **Owner:** (`Guindy` / `Mahmoud` / `Hatem` / `Ali`)
- **Subsystem Namespace:** (`HeistWorld` / `PlayerSystem` / `GuardAI` / `WorldLighting` / `Core`)

### What does this PR do?
- 

---

## ✅ 6-Point Clean Code & Cross-Platform Checklist (Required Before Approval)
Reviewers: please verify each item before clicking **Approve**:

- [ ] **1. Compiles & Passes Tests:** `dotnet build` and `dotnet test` succeed with 0 errors.
- [ ] **2. Single Responsibility (SRP):** Every new class is under ~150 lines and has one clear responsibility (logic vs. rendering separated).
- [ ] **3. No Magic Numbers:** Speeds, radii, timers, and dimensions use named constants (e.g., `private const float MaxVisionDist = 240f;`).
- [ ] **4. C# Naming Standards:** Classes, methods, and properties use `PascalCase`; private fields use `_camelCase`; interfaces start with `I`.
- [ ] **5. Strict Module Boundaries:** Only files inside the author's assigned folder were modified (unless agreed in a `Core` contract update).
- [ ] **6. Windows + Linux Path Safety:** Asset paths use forward slashes (`/`) and **exact lowercase filenames** (e.g., `"assets/sprites/player_sneak.png"`, never `"assets\\sprites\\Player_Sneak.png"`).

---

## 👥 Peer Review Rotation Reminder
| PR Author | Primary Reviewer | Secondary Reviewer |
| :--- | :--- | :--- |
| **Mahmoud** | Hatem | Guindy |
| **Hatem** | Mahmoud | Ali |
| **Ali** | Mahmoud | Guindy |
| **Guindy** | Hatem | Ali |
