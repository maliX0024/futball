# Futball — Editor Wizard Guide
## One-Click Setup to Rule Them All

### Menu
```
Futball → Setup Wizard (One Click)   [F9]
Futball → Setup → Install Full Juice System (One Click)
Futball → Setup → Generate Particles Only
Futball → Setup → Generate Placeholder Audio Only
Futball → Help → Validate Scene
```

### What “One Click” Does (in your CURRENT scene)

**Step 0: Tags**
- Adds `Ball`, `Post`, `Player` to TagManager if missing.

**Step 1: _Managers (0,0,0)**
- Adds `TimeController` (HitStop 70ms, SlowMo 0.32× 0.55s)
- Adds `AudioManager` + creates child `AudioSources` (_SFX, CrowdIdle loop, CrowdOneShot, RollLoop, ChargeLoop)
- Adds `GoalJuiceSequence` (HitStop 70ms, Shake Heavy, Net, Stars, GOAAAL 1.1×, Flash 0.45α 0.18s, SlowMo, Confetti, Haptic)

**Step 2: Main Camera**
- Adds `CameraJuice` (zoom 10%, trauma decay 1.6, shake freq 22, goal pan 0.65s whip 0.28s)
- Calls `CaptureInitialFOV()` so zoom lerps correctly (persp FOV or ortho Size)

**Step 3: Flash Canvas**
- Creates `Futball_FlashCanvas` (ScreenSpaceOverlay, Sort Order 100) + fullscreen `Image` (raycast off, α0)
- Wires to `GoalJuiceSequence.screenFlashImage`

**Step 4: Particles (Prefabs)**
- `Assets/Prefabs/Juice/StarBurst.prefab` — 28 burst, 0.45s, size-over-lifetime, radial velocity
- `Assets/Prefabs/Juice/Confetti.prefab` — 65 burst, 1.8s, gravity 0.55, box shape 4×0.1×2, rotation z
- Wired to `GoalJuiceSequence` + `BallJuice`

**Step 5: Sound Table (16 entries)**
- In `AudioManager.sounds` list: KickSoft/Med/Hard, Charge, Roll, Bounce, PostHit, NetSoft/Hard, CrowdIdle/Gasp/Cheer, GoalShout, Whistle, UIPop, Coin
- Correct `volume` (CrowdIdle 0.55, Roll 0.45) + `loop` flags + `basePitch 1`
- **Placeholder WAVs** (optional but ON by default): generates 16 synthetic WAVs to `Assets/Audio/Placeholders/`:
  - `kick_soft.wav` (120Hz decay), `kick_medium.wav` (190Hz + click), `kick_hard.wav` (72Hz boom + sub drop), `charge.wav` (220→880Hz whine), `roll.wav` (45Hz rumble), `bounce.wav` (220Hz), `post_clank.wav` (980/1450/2100/3100), `net_soft/hard.wav` (fabric swish), `crowd_idle.wav` (loopable brown noise), `crowd_gasp/cheer.wav`, `goaaal.wav` (220→330Hz shout), `whistle.wav` (1.85kHz), `ui_pop.wav` (650Hz), `coin.wav` (880Hz A5 ding)
  - Auto-import settings: Vorbis 0.7, mono, preload (except loops), `Refresh` + `SaveAndReimport`
  - Wires to `AudioManager` only if clip slot empty (never overwrites your Mixkit files)

**Step 6: Ball**
- If no `Ball` (tag Ball) exists, creates `Ball` primitive Sphere (0.45 scale at 0,0.5,3) + `Rigidbody` (0.45kg, continuous dynamic, interpolate) + white material + `ShadowBlob` cylinder child + `TrailRenderer` (team-color gradient, 0.18s) + `BallJuice` (kickSquash 0.32, bounce 0.20, duration 0.18, flightStretch 0.18)

**Step 7: Goal + Net**
- Creates `Goal` at 0,1,10 with `BoxCollider` IsTrigger 7.32×2.44×1.2 (+ white frame cube) + `GoalTrigger` (teamColor, ballTag Ball)
- Creates `Net` child Cube at 0,0,0.35 scale 7×2.2×0.15 with transparent Standard material + `NetJuice` (stretch 1.25,0.85,1.25, ripple 0.18) + wired `GoalTrigger.net`

**Step 8: Player + Demo**
- Creates `Player` Capsule at 0,1,0 (tag Player) + `KickPoint` at 0,-0.7,0.6 + `KickChargeController` (charge 1.1s, force 6→18, lift) wired to ball RB + ballJuice
- Adds `FutballJuiceDemo` to `_Managers` (ball = Ball, scorer = Player, teamColor)

**Step 9: Wiring Pass**
- Re-wires all cross-refs: `GoalJuiceSequence` ← flashImage, netJuice, ballTransform, prefabs; `BallJuice` ← kick/post prefabs; `GoalTrigger` ← ball/net/teamColor; `CameraJuice.CaptureInitialFOV()`
- Marks scene dirty + `AssetDatabase.SaveAssets()`

---

### Use It

**First time:**
1. Open your match scene (or create New Scene).
2. `Futball → Setup Wizard → ☑ all → Team Color → ⚡ INSTALL`.
3. `File → Save Scene`.
4. Play → press **G** → full GOAL juice should fire.

**Re-run anytime:**
- It’s idempotent — checks `Find` before `Create`, uses `EnsureComponent`, only wires null slots, never duplicates. Use it to **repair** after you delete something.

**Fast test without window:**
- `Futball → Setup → Install Full Juice System (One Click)` (dialog confirm).

---

### Validation

- Window shows live Warnings box from `GatherIssues()` (managers, camera, flash, particles, audio clips, ball, goal trigger).
- `Futball → Help → Validate Scene` pops same list.
- Fixes button is just re-running Install.

---

### Replace Placeholders

Placeholders let you hear **everything instantly**. To ship with real sounds:

1. Download Mixkit/Freesound WAVs (see `SETUP_GUIDE.md` §4).
2. Drop into `Assets/Audio/` (any folder).
3. In `_Managers → AudioManager → sounds` list, drag your clip over the placeholder (or delete placeholder file — wizard won’t overwrite occupied slot).
4. Optionally delete `Assets/Audio/Placeholders/` — system keeps working.

---

### Custom Team Flash

- `GoalTrigger.teamColor` per goal (blue home / red away).
- Wizard `Team Color` is the default tint for all new Goals + Confetti + Flash fallback.

---

### No-Dependency Guarantee

- No Cinemachine, Timeline, or Addressables required.
- Works on Unity 2021.3 LTS, 2022 LTS, 6000.
- All editor code under `#if UNITY_EDITOR` — zero build overhead.

---

*Wizard: `Assets/Editor/Futball/FutballJuiceSetupWizard.cs` (1,306 lines) + `FutballMenuQuickActions.cs`*
*Runtime still 100% in `Assets/Scripts/Juice/` + `Audio/` — wizard just wires it.*
