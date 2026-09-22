# Futball — Unity Juice Setup Guide
### GOAL Stack + Camera Juice + Full Sound Table

---

## 1) What You Got

This delivers **exactly what you asked for**:

**GOAL Sequence (one call):**
```
Hit stop (70ms) → Heavy shake (trauma 1.0) → Net stretch → Ball stars →
GOAAAL! + crowd roar pitched 1.1x → Team-color flash (0.18s) →
Slow-mo 0.32x 0.55s → Confetti + Heavy haptic + Goal Pan → Reset
```
Call: `GoalJuiceSequence.Instance.PlayGoal(ball.position, teamColor, scorer);`

**Camera:**
- `CameraJuice.SetKickCharge(0..1)` → lerps 10% closer (FOV/Size)
- `CameraJuice.SnapKick()` → snap back + light shake on kick release
- `CameraJuice.DoGoalPan(ball, scorer)` → follow ball into net, whip to scorer
- `CameraJuice.ShakeLight/Medium/Heavy()` → trauma decay system (2/5/10 mapped to 0.22/0.5/1.0)

**Sounds (full table, pitch & ducking built-in):**
| You asked | Method |
|---|---|
| Kick Soft/Med/Hard (pitch = power*0.2+random) | `AudioManager.Instance.PlayKick(power01)` |
| Charge rising whine | `StartCharge() / UpdateCharge(power) / StopCharge()` |
| Roll rumble loop | `UpdateRoll(speed)` every frame |
| Bounce plop | `PlayBounce(impact01)` auto on collision |
| Post CLANK + long ring + crowd gasp | `PlayPostHit()` |
| Net swish/hard | `PlayNet(hard)` |
| Crowd idle loop + gasp + cheer + ducking | `PlayCrowdIdle() / PlayCrowdCheer(pitch)` |
| Whistle | `PlayWhistle()` |
| UI Pop | `PlayUIPop(positive)` |
| Coin C-E-G cascade with pan | `PlayCoin(3)` |
| GOAL combo pitched up | `PlayGoalSequence(1.1f)` |

---

## 2) Installation (5 min)

### A. Create Managers
1. Create empty GameObject `_Managers` at (0,0,0)
2. Add components:
   - `TimeController`
   - `AudioManager`
   - `GoalJuiceSequence`
3. On **Main Camera** add `CameraJuice` (drag cam ref if not auto-found)

### B. Audio Setup
1. Select `_Managers` → `AudioManager` inspector
2. You'll see **Sound Library** list. Expand:
   - **Size = 14** (or click `+` to add SoundEntry)
   - Assign freely from freesound.org / Mixkit (see §4 for links)
   - If you leave clips empty, no errors — just silent (placeholders)
   - Set `Crowd Idle` loop clip if you have it (or leave silent until you add)

   Required clip mapping:
   ```
   KickSoft -> thud.wav
   KickMedium -> thwack.wav
   KickHard -> boom.wav
   Charge -> charge_whine.wav (loop)
   Roll -> roll_rumble.wav (loop)
   Bounce -> plop.wav
   PostHit -> clank.wav
   NetSoft -> swish_soft.wav
   NetHard -> swish_hard.wav
   CrowdIdle -> crowd_idle_loop.wav (loop)
   CrowdGasp -> gasp.wav
   CrowdCheer -> cheer.wav
   GoalShout -> goaaal.wav (optional, else uses cheer pitched)
   Whistle -> whistle.wav
   UIPop -> pop.wav
   Coin -> coin_ding.wav
   ```

3. AudioManager auto-creates child sources (SFX, CrowdIdle, Roll, Charge)

### C. Goal Setup
1. Select `GoalJuiceSequence` on `_Managers`:
   - `Star Burst Prefab` → drag `Assets/Prefabs/Juice/StarBurst` (create via `Futball > Create Juice Particles` menu)
   - `Confetti Prefab` → `Assets/Prefabs/Juice/Confetti`
   - `Screen Flash Image` → Create Canvas → Fullscreen Image (white, alpha 0) → drag here
   - `Net Juice` → drag your Net GameObject (add `NetJuice` to net)
   - `Ball Transform` → drag ball
2. On **Goal** GameObject (with BoxCollider IsTrigger):
   - Add `GoalTrigger`, set `Team Color` (e.g., blue for home, red for away), assign ball & net
3. On **Ball** prefab:
   - Add `BallJuice`, assign `Visual` (mesh child), `TrailRenderer` (add TrailRenderer if missing)
   - Tag ball as `Ball`, tag posts as `Post` (or set layer `Post`)
   - Assign `Post Spark Prefab` → reuse StarBurst with small scale, or leave empty

### D. Player Kick (example)
- On Player, add `KickChargeController`:
  - Assign `Ball Rb`, `Kick Point` (empty at foot), `Ball Juice`
- It already wires: charge → zoom + charge sound, kick → squash + kick SFX + shake

### E. UI Flash (one-time)
1. Create Canvas (Screen Space Overlay, Sorting Order 100)
2. Create Image child, anchor stretch full screen (0,0 - 1,1), color White α 0
3. Drag to `GoalJuiceSequence.screenFlashImage`

### F. Particles
- Top menu: **Futball → Create Juice Particles** → generates 2 prefabs in `Assets/Prefabs/Juice/`

---

## 3) How To Use In Code

```csharp
// KICK (from your input / character controller)
float power = charge01; // 0..1
AudioManager.Instance.PlayKick(power);
ballJuice.OnKick(kickDir, power);
CameraJuice.Instance.ShakeLight(); // or Medium if hard

// CHARGING (every frame while holding)
CameraJuice.Instance.SetKickCharge(power);
AudioManager.Instance.UpdateCharge(power); // pitch & volume rise
AudioManager.Instance.UpdateRoll(ballRb.velocity.magnitude); // call every Update

// ON RELEASE
CameraJuice.Instance.SnapKick();
AudioManager.Instance.StopCharge();

// POST HIT (auto via BallJuice collision, or manual)
ballJuice.OnPostHit(contactPoint);
AudioManager.Instance.PlayPostHit();

// NET
AudioManager.Instance.PlayNet(hard: true); // hard swish for powerful goals

// GOAL (trigger does this auto, or call manually)
GoalJuiceSequence.Instance.PlayGoal(ball.position, teamColor, scorerTransform);

// OTHER SFX
AudioManager.Instance.PlayWhistle();
AudioManager.Instance.PlayUIPop(positive: true);
AudioManager.Instance.PlayCoin(3); // C-E-G cascade

// HAPTICS
Haptics.Light(); Haptics.Medium(); Haptics.Heavy();
Haptics.Enabled = false; // user toggle in settings

// TRAUMA SHAKE directly
CameraJuice.Instance.AddTrauma(0.5f); // 0..1
CameraJuice.Instance.ShakeHeavy(); // Goal
```

---

## 4) Free Sound Sources (assign in 10 min)

Download then drag to AudioManager. All CC0 / free for commercial:

- **Mixkit** (mixkit.co/free-sound-effects/football) - kicks, whistle, crowd
- **Freesound** (freesound.org):
  - `thud` → search "football kick soft"
  - `thwack` → "punch thwack"
  - `BOOM` → "cinematic boom kick"
  - `clank` → "metal post hit"
  - `swish` → "net swish fabric"
  - `crowd` → "crowd cheer football", "crowd gasp"
- **Sonniss** - free GDC packs have great impacts

**Pro tip:** Normalize to -1dB, trim silence, add slight reverb to Post & Net in Audacity.

---

## 5) Tuning Cheatsheet

| Feel too... | Do this |
|---|---|
| Shake too wild | Lower `CameraJuice.maxShakeTranslation` 0.35 → 0.2 |
| Zoom too much | `zoomAmount` 0.10 → 0.07 |
| Hit stop jarring | `hitStopDuration` 0.07 → 0.05 |
| Slow-mo too long | `slowMoDuration` 0.55 → 0.35 |
| Charge whine annoying | Lower `chargeSource.volume` max 0.55 → 0.35 |
| Crowd duck too obvious | `CrowdDuckRoutine` duckTo 0.18 → 0.35 |

---

## 6) Quick Test

1. Add `FutballJuiceDemo` to empty GameObject, press Play
2. Keys:
   - **G** = Full GOAL, **P** = Post, **K** = Kick
   - **H/J/B** = Light/Med/Heavy shake
   - **1-0** = Sounds
   - On-screen buttons also work
3. Build to phone to feel haptics.

---

## 7) Performance Notes

- All time effects use `unscaledDeltaTime` → shake/zoom work during freeze.
- Trail disabled under 2.5 speed → zero cost when still.
- Particles pooled? For now Instantiate/Destroy (fine for <2 goals/sec). For production, swap to pooling (ask me).
- `TimeController` fixes `fixedDeltaTime` so physics stays stable.

---

## 8) Next Steps I Can Do For You

- Wire this to **Cinemachine** ( Impulse + virtual cam blend)
- Add **ghost replay / kill-cam**
- Convert to **Addressables + Object Pooling**
- Build the **Shop + Ball Pass** monetization you wanted earlier

Just say the word and I’ll push it.

*— Futball Juice v1 — 2026-09-22*
