# FUTBALL — Small Changes, Big Wins Playbook
### Game Juice • Effects • Sound • Monetization
> Philosophy: **Juice is cheap, feel is expensive.** 80% of "fun" comes from 20% of polish that costs <2 hours each.

---

## 🔥 TOP 12: Do These First ( < 1 day each, huge feel )

| # | Idea | Effort | Win | Category |
|---|---|---|---|---|
| 1 | **Hit Stop + Screen Shake on Goal/Save** | 30 min | Makes every goal feel massive | Juice |
| 2 | **Squash & Stretch on Ball + Players** | 1h | Instant cartoon quality | Juice |
| 3 | **Ball Trail + Speed Lines** | 1h | You *see* power | Effects |
| 4 | **Crowd + Announcer ONE-LINERS** | 2h | World feels alive | Sound |
| 5 | **Goal Net Physics (ripple + stretch)** | 2h | Satisfaction spike | Effects |
| 6 | **Perfect Kick sweet-spot + SFX pitch shift** | 2h | Skill expression | Gameplay |
| 7 | **Confetti + Slow-Mo on Winning Goal** | 1h | Shareable moment | Juice |
| 8 | **Haptics (light/medium/heavy)** | 1h | Feels premium on phone | Juice |
| 9 | **Daily Spin Wheel** | 3h | +18-30% D1 retention | Monetization |
| 10 | **Cosmetic Ball Trails (first monetized item)** | 4h | Proves monetization without P2W | Monetization |
| 11 | **Ghost Replay of Last Shot** | 3h | "One more try" loop | Gameplay |
| 12 | **Dynamic Camera Zoom & Pan** | 2h | TV broadcast feel | Juice |

Pick 3 per week. Ship every Friday.

---

## 1. GAMEPLAY - Small Tweaks, Massive Fun

### A. The "One More Try" Loop
1.  **Sweet Spot Kick** - Add a 100ms timing window indicator (shrinking circle like penalty). Hit green = 15% more power + curve. Miss = normal. Depth without complexity.
2.  **Curve After-Touch** - After shooting, swipe to add last-second curve (FIFA style). Costs nothing to add, makes every shot interactive.
3.  **Risk/Reward Charging** - Hold to charge power (bar fills + controller vibrates faster). Hold too long = wobble/miss. Tension for free.
4.  **Deflection Chaos** - Make keeper/players deflect, not just catch. Random rebounds = drama and funny moments (clip-worthy).
5.  **Last-Second Save Slow-Mo** - If keeper saves within 0.3s of goal line, trigger 0.4s 30% slow-mo. Feels cinematic.

### B. Tension & Flow
6.  **Momentum Bar** - 3 goals in a row = "ON FIRE" (ball on fire, +5% speed). Resets on miss. Players chase streaks.
7.  **Narrow Miss Feedback** - Ball grazing post shows spark + *CLANK* + camera shake. Makes misses feel *close* not frustrating.
8.  **Wind / Weather Event** - Every 5th match: wind arrow or rain puddle that slows ball. One variable changes whole meta.
9.  **Power-ups that DON'T break game** - Only 3: *Big Goal (5 sec), Icy Floor (sliders), Double Ball (chaos)* - spawn rarely, picked up by ball passing through.
10. **Ghost of Your Best Shot** - Show translucent ghost ball of your previous best goal. You naturally compete with yourself.

---

## 2. GAME JUICE - Make It Feel AAA in 2 Hours

This is where indie games win or lose.

### Camera (0 lines of art, pure code)
- **Kick Zoom:** Camera lerps 10% closer as player charges shot, snaps back + shakes on kick
- **Goal Pan:** On goal, camera follows ball into net, then whips to scorer
- **Hit Stop:** Freeze everything for 60-80ms on goal / crossbar / big save. Feels like impact.
- **Trauma Shake:** Not random shake - decaying shake. Light (shot) = 2, Medium (post) = 5, Heavy (goal) = 10

### Animation Juice (no new animations needed)
- **Squash & Stretch:**
  - Ball squashes 30% on kick and bonk, stretches on flight
  - Player squashes before jump, stretches at peak
- **Anticipation + Overshoot:** Before kick, pull leg back for 2 frames. After goal, player scales to 1.15 then settles to 1.0 with spring (DOTween/LeanTween)
- **Idle Wiggle:** Players never fully still - breathe, bounce. World feels alive.
- **Juicy UI:** Every button scales 1.1 on press, 0.95 on release + *pop* sound. Coins do parabolic hop to counter.

### Feedback Stack (layer them!)
Every meaningful action should trigger 3-5 of these *at once*:

> **Example: GOAL**
> 1. Hit stop (70ms) → 2. Heavy shake → 3. Ball explodes into stars → 4. Net stretches → 5. "GOAAAL!" + crowd roar + pitch-up ding → 6. Screen flashes team color → 7. Slow-mo 0.5s → 8. Confetti from top + haptic

> **Example: POST HIT**
> 1. Spark particle → 2. Metal *DING!* (high pitch) → 3. Light shake → 4. Ball spins faster → 5. Post wobbles

---

## 3. GAME EFFECTS (VFX) - Cheap & Cheerful

Prioritize **2D particles over 3D shaders** - they read better on mobile.

### Must-Have (use free packs - Kenney, Unity Particle Pack)
1.  **Ball Trail** - Thin white → team color gradient trail, fades in 0.3s. Faster shot = longer trail. (TrailRenderer, 5 min)
2.  **Kick Burst** - 8-point star at foot + dust puff. One sprite, scale + fade.
3.  **Run Dust** - Little puffs under feet when sprinting/changing direction
4.  **Net Ripple Shader** - Simple sine wave vertex displacement when ball hits. 10 lines of shader.
5.  **Grass Cut** - Dark streak where ball rolled (Decal, fades in 3s). Makes pitch feel physical.
6.  **Impact Ring** - Expanding circle on ground where ball lands/bounces
7.  **Speed Lines** - 2-3 white lines behind ball when velocity > threshold. Anime feel.
8.  **Goal Confetti** - Team-color rectangles + stars rain for 1.5s. Overkill is correct.

### Pro Tier (Week 2)
9.  **Chromatic Aberration + Vignette Pulse** on power shot
10. **Afterimage/Dash Echo** for super sprint
11. **Dynamic Shadows** - Blob shadow under ball scales with height (sells 3D)
12. **Weather Particles** - Rain streaks / dust motes (just an overlay, instant atmosphere)

**Performance rule:** Keep particles < 30 alive. One atlas, additive blending.

---

## 4. SOUNDS - 50% of Juice is Audio

> Player will forgive bad art, never bad sound. Add all SFX with pitch randomization ±0.1

### Core SFX Kit (You need ~15 sounds, that's it)
**Use freesound.org / Sonniss / Mixkit - no excuses**

| Action | Sound | Juice Trick |
|--------|-------|-------------|
| **Kick Soft / Medium / Hard** | `thud`, `thwack`, `BOOM!` | Pitch = power * 0.2 + random |
| **Charge** | Rising whine/whistle | Volume rises with power |
| **Ball Roll** | Soft rumble loop | Louder with speed |
| **Bounce** | Hollow `plop` | Short reverb |
| **Post Hit** | Metal *CLANK!* | Long ring + echo |
| **Net** | `swish` + fabric `flap` | Different for soft/hard goal |
| **Crowd** | Loop `crowd_idle` + `crowd_gasp` + `crowd_cheer` | Duck idle under cheer (sidechain fake) |
| **Whistle** | Ref whistle | Reverb, signals state change |
| **UI Pop** | `blip` / `pop` | Pitch up for positive, down for negative |
| **Coin** | `ding` cascade (C-E-G) | Pan left/right |

### Music & Announcer (Retention Hack)
- **Music:** 2 tracks only - `menu_lofi` (chill) + `match_tension` (98 BPM, drums enter at 1-1). Low volume, ducks during goals.
- **Announcer:** 10 lines is enough: "Ooooh!", "What a strike!", "UNBELIEVABLE!", "GOAAAL!", "Denied!". AI voice (ElevenLabs) = free. Triggers on events, not loop.
- **Dynamic Mix:** Muffle music with low-pass filter when slow-mo hits. Instant cinema.

### Audio Juice Tricks (1 hour each)
- **Whoosh on Camera Move** - Subtle wind when camera pans fast
- **Heartbeat on Final Seconds** - At 10s left & score tied, add heartbeat tick
- **Stinger on Win/Lose** - 2 sec musical sting: Major chord = win, minor = lose. Closes the loop.

---

## 5. MONETIZATION - Fun First, Money Second (Non-Pay-To-Win)

**Golden Rule:** Sell *expression* and *time*, not *power*. P2W kills football games.

### Tier 1: Frictionless First Dollars (Day 1)
1.  **Starter Pack ($0.99)** - Rare ball + trail + 3x daily coins. 5% conversion if shown after first win. "Welcome to the club"
2.  **Remove Ads ($2.99)** - Best ROI item. Offer after 3rd ad. "Support us + instant restarts"
3.  **Cosmetic Balls & Trails** - 20 balls (flame, galaxy, watermelon), 10 trails (fire, rainbow, lightning). Grid shop, 3 free, rest $0.49-$1.99 or coins. Pure status.
4.  **Goal Celebrations** - Buyable celebrations (floss, backflip, knee slide). Shown on replay. Players pay to be funny.

### Tier 2: Retention Monetization (Week 1)
5.  **Daily Login + Spin Wheel** - Day 7 = Epic Ball. Wheel has "AD for double spin". Ads become reward, not punishment.
6.  **Ball Pass (Season Pass $4.99 / 30 days)** - Free track: coins. Premium track: 1 exclusive ball + trail + skin per week. FOMO without pressure. Best LTV driver.
7.  **Coin Doubler ($3.99 permanent)** - 2x match coins forever. High perceived value, zero economy damage.
8.  **Gacha Crates (Soft)** - Common crate = free after 3 wins, Rare crate = 250 coins. 90% cosmetics, 10% coin packs. Never power.

### Tier 3: Advanced (Month 1)
9.  **Stadiums & Pitch Skins** - Beach, Snow, Neon Night, Old Trafford-style. $1.99 or grind 5000 coins. Changes whole vibe for streamer clips.
10. **Emotes & Quick Chat** - "GOLAZO!", "NT", crying emoji. $0.99 packs. Drives social.
11. **Challenge Tickets** - Daily challenge free, hard challenge needs ticket (1 free/day, buy 5 for $0.99). Hard = 5x rewards.
12. **Interstitial Strategy:** Never during play. Only: (1) after 3 matches, (2) before reward x2. Always give "No thanks" in <1s. Rewarded ads = 3x better eCPM, player chooses.

### Pricing Psychology
- Anchor: Show $9.99 bundle crossed out → $2.99
- Use coins (100 = ~$1) - spending 499 coins hurts less than $4.99
- First purchase bonus: 2x coins on first buy
- **Never** show more than 3 shop items at once. Curated > cluttered.

### What NOT to Do
❌ Pay for stats (+speed, +power) → instant churn
❌ Energy timers → kills "one more match" loop
❌ Lootboxes with power → app store complaints

---

## 6. RETENTION LOOP - Make Them Come Back Tomorrow

```
PLAY (2 min match) → JUICE REWARD (confetti/coins) → PROGRESSION (unlock 5% of next ball)
      ↑                                                    ↓
   SOCIAL ←――――――――――― DAILY HOOK (spin, quest) ←――――――――――
 (ghost/clip share)
```

- **Daily Quest (3 only):** "Score 3 curve goals", "Hit post 2x", "Win with rainy pitch" = 200 coins
- **Weekly Tournament (Ghosts):** Beat 5 ghost scores, leaderboard, top 10% gets Neon Trail
- **Clip Button:** Auto-save last goal as 3-sec replay. One-tap share. Free UA.

---

## Implementation Roadmap - 4 Weeks

**WEEK 1 - Feel:**
- [ ] Hit stop + shake + squash system (1 script, reusable)
- [ ] 15 SFX + pitch randomizer + crowd loop
- [ ] Ball trail + kick burst + net ripple
- [ ] Haptics wrapper

**WEEK 2 - Loop:**
- [ ] Sweet spot + curve after-touch
- [ ] Ghost replay + narrow miss VFX
- [ ] Daily spin + coins economy
- [ ] 10 cosmetic balls

**WEEK 3 - Polish & Sound:**
- [ ] Announcer + dynamic music ducking
- [ ] Confetti + slow-mo + camera zoom
- [ ] Stadium skins (recolor + overlay)
- [ ] Celebrations

**WEEK 4 - Money:**
- [ ] Shop UI + Starter Pack + Remove Ads
- [ ] Ball Pass (7-day MVP first)
- [ ] Rewarded ads (coins x2, extra spin)
- [ ] Share clip button

---

## Want Me to Build It?

I can prototype any of these right now in your repo. Tell me:

1. Engine? (Unity / Godot / Phaser / React Canvas / other)
2. Pick your top 3 from the Top 12 - I'll code them this session

Example: *“Do #1, #2, #3 for a web canvas game”* → I’ll ship shake, squash, and trails with preview.

---

*Built for `maliX0024/futball` - 2026-09-22 - Small Changes, Big Wins.*
