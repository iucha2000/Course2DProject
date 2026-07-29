# PLAN.md — Frog Ninja

Final-exam game built on top of the Unity 2D course project. See `CLAUDE.md` for conventions.

**Concept:** a 3-level pixel platformer. Run, jump, ride moving platforms, dodge saws, **stomp**
enemies and **throw shuriken** at the ones you can't stomp, collect cherries and coins, reach the
exit. Progress and settings persist between sessions.

**Target playtime:** 8–12 minutes.

---

## ▶ START HERE — current state

*Last updated: end of the M3 session.*

| | |
|---|---|
| **Done** | M0 (foundation), M1 (player refactor + combat), M2 (enemies, hazards, health, camera bounds), M2.5 (convention cleanup), **M3 (audio, saving, HUD, pause, level flow)** |
| **Next** | **M4 — level design & build** |
| **Playable now** | All three levels load and chain. Move, jump (with coyote time), **dash in the air and slide on the ground (Shift)**, dive-stomp, throw shuriken at the mouse, take damage, fall out of the level and die, collect coins / cherries / ammo, watch the HUD, claim checkpoints, pause with Esc, set volume, reach the exit and unlock the next level. Checkpoint, ammo and coins all survive death, level changes and a full relaunch. |
| **Not built yet** | The actual level designs — `Level2` and `Level3` are still copies of `Level1`. Audio *clips* (the system is wired; see *Deferred manual steps*). |

**To resume, just say:** *"Read PLAN.md and continue with M4."*

### Things to hand over at the start of the next session
- **Close Unity before saying go.** Scene, prefab and controller YAML get edited directly, and the
  Editor will overwrite those edits when it saves. Commit first too.
- Nothing is blocked.
- `Level1` has hand-placed test objects (a Saw, a SpikedEnemy) and the `LevelExit` was dropped at
  `x = 24` without knowing whether there is floor under it. **Move it onto solid ground before
  playtesting the level flow.** `Level2` and `Level3` are still plain copies.
- **Level1 is no longer safe to copy over the others** — they get designed properly in M4.

### Checklist items owed: one left
Six were owed going into M3 — the five that were tracked, plus `yield return null`, which the
checklist had ticked but which was not actually in any script. Five are now repaid:

| Item | Repaid by | Where |
|---|---|---|
| `SetActive` | M3 | pause panel, HUD hearts, the Continue button on the intro screen |
| `SetParent` | M3 | heart icons instantiated under the hearts container (`Hud.BuildHearts`) |
| `Time.deltaTime` | M3 | banner fade, and the pause menu's two clocks vs `Time.unscaledDeltaTime` |
| `OnCollisionExit2D` | M3 | coyote time (`Player.OnCollisionExit2D`) |
| `yield return null` | M3 | the per-frame step of the banner fade (`PickupBanner.HideAfterDelay`) |
| `Transform.Translate` | **M4** — still owed | scrolling background element |

`Physics2D.Raycast` was in the same position and was repaid in M2.5 — it is used three times in the
enemy's ledge, blocked and clearance checks.

### Known placeholders to revisit in M4
- `CameraBounds` polygon is sized to the current test tilemap, not to a designed level.
- The three levels are identical copies with no real layout yet.
- `CubeObstacle.prefab` still points at a sprite inside `Library/PackageCache/` *(defect #10)*.
- `LevelExit` is placed at `x = 24` in all three levels, and the two `Checkpoint`s at `x = -14`
  and `x = 10`. All were guesses — they need putting on real ground once the levels exist.
- The fall-death line is derived from each level's `CameraBounds`, so it needs no per-level
  tuning — but every designed level must actually **have** camera bounds, or it falls back to the
  fixed `Fall Death Height` of −20.
- **M4 must design at least one low tunnel** to make the ground slide worth having, and size air
  gaps against the single air dash (`Dash Speed` 20 × `Dash Duration` 0.16 ≈ 3.2 units).

---

## Core design decisions

| Decision | Choice | Why |
|---|---|---|
| Attack — melee | **Stomp** (land on an enemy's head) | Needs no new art — reuses the Jump/Fall animations. Detected with `OnCollisionEnter2D` + `GetContact(0).normal.y > 0.5f`, the same contact-normal technique already used for knockback. |
| Attack — ranged | **Thrown shuriken**, `Fire1` | Reuses `Assets/Art/Saw.png` (8 frames, already sliced but previously unused). Demonstrates `Instantiate`/`Destroy`, which nothing in the project did before. |
| Ammo economy | Stomped enemies **drop shuriken ammo** | Creates a risk/reward loop: get close to earn ranged ammo. |
| Collectibles | **coin = score, cherry = health, shuriken = ammo** | One collectible, one meaning. No double duty. |
| Why ranged matters | **Spiky Enemy** variant can't be stomped | Forces the player to actually spend ammo. |
| Saved data | Level unlock + music/SFX volume | Gives the game a sense of persistence across launches. |
| Extras | Pause menu (`Time.timeScale`), saw hazards | Cut: Light2D, results screen. |

---

## Concept checklist = acceptance criteria

The finished game must demonstrate **every** box below.

### Already demonstrated — must survive into the final game
- [x] URP 2D Renderer
- [x] Custom tags (`Obstacle`, `Item`, `Platform`, `Enemy`)
- [x] Sprite import at 16 PPU, `Multiple` mode + Sprite Editor slicing
- [x] `SpriteRenderer` — sprite, `flipX`, `color` tint
- [x] Tile Palette → Grid → Tilemap, `TilemapRenderer`, `TilemapCollider2D`
- [x] Sprite-swap `.anim` clips, `m_LoopTime`, custom sample rates
- [x] AnimatorController state machines, default state, **AnyState** transitions, exit time
- [x] Animator parameters of all three types — Float, Bool, Trigger
- [x] `SetFloat` / `SetBool` / `SetTrigger`
- [x] `Rigidbody2D` — Dynamic **and** Kinematic, `gravityScale`, constraints, mass, collision detection
- [x] Box / Circle / Capsule / Tilemap Collider2D
- [x] Trigger vs solid colliders
- [x] `OnCollisionEnter2D` / `OnCollisionStay2D` / `OnCollisionExit2D` + **contact normals**
      *(Exit restored in M3 as coyote time; Stay added in M3 so a solid hazard re-hits)*
- [x] `OnTriggerEnter2D`
- [x] `Physics2D.Raycast` / `RaycastHit2D` — *M1 removed the player's raycast ground check;
      **restored in M2.5** as the enemy's ledge check (`Enemy.HasGroundAhead`)*
- [x] `AddForce` + `ForceMode2D.Impulse`, `linearVelocity`
- [x] MonoBehaviour lifecycle, Inspector-serialized fields
- [x] `GetComponent<T>`, `CompareTag`, `FindGameObjectWithTag`
- [ ] `Transform.Translate` / `SetParent`, `Time.deltaTime` — `SetParent` restored in M3
      (`Hud.BuildHearts`, with `worldPositionStays: false`), `Time.deltaTime` restored in M3
      (banner fade + pause clocks). ⚠️ **`Translate` is still missing** — went in the M1 player
      refactor, **owed by M4** on a scrolling background element
- [x] Coroutines — stored handle + `StopCoroutine`, `WaitForSeconds`, `yield return null`
      *(`yield return null` was missing until M3 despite this box being ticked; the banner fade
      in `PickupBanner` now uses it)*
- [x] `Vector2.Distance`, `Mathf.Abs`, `MoveTowards`
- [x] `Dictionary<string,int>` (`Player.collectedItemCounts`, read by the HUD through
      `Player.CollectedCount`), `SetActive` — restored in M3 (pause panel, HUD hearts,
      intro Continue button)
- [x] AI state machine with hysteresis
- [x] Legacy Input Manager — `GetAxis`, `GetKeyDown`
- [x] Orthographic camera
- [x] Cinemachine 3 — Brain, CinemachineCamera, TrackingTarget, PositionComposer damping
- [x] Canvas Overlay, CanvasScaler, GraphicRaycaster, EventSystem
- [x] RectTransform anchors & pivots
- [x] uGUI `Image` runtime `.sprite`, `Graphic.enabled`
- [x] `TextMeshProUGUI` runtime text
- [x] `Button.onClick.AddListener` from code
- [x] `SceneManager.LoadScene`
- [x] Prefabs, instances, instance overrides, added/removed components on an instance

### Being added
- [x] **Physics layers + Layer Collision Matrix** — M0
- [x] **Sorting Layers** (Background / Default / Foreground) — M0
- [x] **`LayerMask` in physics queries** — M1 (`Player.CheckGround`, `Enemy.HasGroundAhead`)
- [x] **`Instantiate` / `Destroy`** — M1 (shuriken, ammo drop)
- [x] **Player offense** (dive-stomp + shuriken) — M1
- [x] **Prefab Variants** (`SpikedEnemy`) — M2
- [x] **PlayerPrefs** (`SetInt`/`GetInt`, `SetFloat`/`GetFloat`, `DeleteKey`) — M3 (`SaveSystem`)
- [ ] **Audio** (`AudioSource`, `AudioClip`, `PlayOneShot`, music vs SFX, volume) — M3 built the
      whole system and every call site is live. ⚠️ **Not satisfied until clips are assigned** —
      see Deferred manual steps
- [x] **`Time.timeScale`** (pause menu) — M3
- [x] **uGUI `Slider`** + `onValueChanged.AddListener` — M3 (volume sliders)
- [x] **`CanvasGroup`** for fading a UI subtree — M3 (pickup banner)
- [x] **`DontDestroyOnLoad` singleton** — M3 (`AudioManager`)

---

## Milestones

### M0 — Foundation & cleanup ✅ DONE
- [x] Layers 6–11 added: `Ground`, `Player`, `Enemy`, `Projectile`, `Hazard`, `Pickup`
- [x] Sorting layers added: `Background`, `Default`, `Foreground`
- [x] Layer collision matrix configured (6 pairs disabled — see `CLAUDE.md`)
- [x] `GameScene` renamed to `Level1` (GUID preserved via `git mv` of the `.meta`)
- [x] Build settings: `IntroScene` (0), `Level1` (1)
- [x] **Defect #5 fixed** — `class CanvasHandler` in `IntroCanvasHandler.cs` renamed to
      `IntroCanvasHandler`; the scene's `m_EditorClassIdentifier` updated to match
- [x] Archived to `Assets/_Archive/`: `Test.cs`, `Player Movement.inputactions`, `hunter_idle_512.png`
- [x] Deleted `Assets/TextMesh Pro/Examples & Extras/` (284 files; TMP `Resources/` + `Fonts/` kept)
- [x] `CLAUDE.md` and `PLAN.md` written

**Acceptance:** project compiles, both scenes open with no missing-script warnings, new layers and
sorting layers appear in the Inspector dropdowns.

### M1 — Player refactor & combat core ✅ DONE
- [x] `Player.cs`: horizontal movement → `rb.linearVelocity` in `FixedUpdate` *(defect #1)*
- [x] `Player.cs`: ground check → `Physics2D.OverlapCircle` with a `Ground` LayerMask *(defect #2)*
- [x] `Player.cs`: `rigidbody2D` → `rb`, components resolved in `Awake` *(defects #4, #9)*
- [x] **Stomp** — `OnCollisionEnter2D`, `normal.y > 0.5f` → kill enemy + bounce
- [x] `PlayerCombat.cs` — `Fire1` throws, ammo count, cooldown
- [x] `Projectile.cs` — `Destroy` on hit + `Destroy(gameObject, lifetime)`
- [x] `Shuriken.prefab` + `Shuriken_Spin.controller` / `.anim` from the 8 `Saw.png` frames
- [x] `AmmoPickup.prefab` — what a killed enemy drops
- [x] `Item.cs` gains an `ItemType` enum (Coin / Cherry / Shuriken)
- [x] `Player.controller`: fixed the overlapping `Run` thresholds *(defect #6)* and deleted the
      unreachable `Player_Fall → Run` transition *(defect #7)*
- [x] Layers assigned: Player→`Player`, Tilemap+Platform→`Ground`, Enemy instance→`Enemy`,
      Cherry/Coin/AmmoPickup→`Pickup`, Shuriken→`Projectile`

**Deviations from the original plan (deliberate):**
- `Enemy.Die()` and the ammo drop were pulled forward from M2, because stomping is pointless
  without something to kill and the ammo loop is what makes stomping worth doing.
- The shuriken uses a **Dynamic** Rigidbody2D with `gravityScale 0`, not Kinematic. A kinematic
  body does not generate trigger contacts against the static tilemap collider unless
  `useFullKinematicContacts` is on, so a kinematic shuriken would fly straight through walls.
- The Enemy is a Player-prefab instance, so it inherited the new `PlayerCombat` component and
  would have started throwing shuriken. `PlayerCombat` was added to the instance's
  `m_RemovedComponents`. **M2 promotes Enemy to its own prefab and this hack goes away.**

**Acceptance:** player moves without tunneling; ground check never self-hits; `Fire1` spawns a
spinning shuriken that despawns on hit or after its lifetime; stomping an enemy kills it, bounces
the player and drops ammo; walking into an enemy still hurts.

#### M1 follow-up (after first playtest)
- [x] Thrown shuriken scaled down `0.3 → 0.2`
- [x] **Aiming now follows the mouse cursor.** `Camera.main.ScreenToWorldPoint(Input.mousePosition)`
      gives a world-space aim point; `Launch` takes a `Vector2` direction instead of a left/right
      float. The player also turns to face the throw.
- [x] **Ammo drop made visually distinct from the damaging projectile.** The standard fix is to
      change *motion language* and *palette* rather than the sprite, so the player reads "safe to
      touch" at a glance:
      | | Thrown shuriken | Ammo drop |
      |---|---|---|
      | Colour | white / steel | **gold** — the same tint Cherry and Coin already use |
      | Spin | full speed | **0.3× speed** (`Shuriken_Pickup.controller`, same clip reused) |
      | Motion | flies in a straight line | **bobs gently in place** (`Bobber.cs`) |
      | Scale | 0.2 | 0.26 — deliberately a bit larger, easier to spot and collect |
- [x] **Shuriken despawns at the edge of the screen.** It travelled 14 u/s × 3 s = 42 units while
      the camera only shows ~17.8 units, so it could kill enemies well outside the view. `Update`
      now converts the position with `WorldToViewportPoint` and destroys it once it leaves the
      0..1 range. Done by hand rather than with `OnBecameInvisible`, because that message counts
      the Scene view as a camera and so misbehaves while testing in the Editor.
- [x] **`Bobber` on Cherry and Coin** so every pickup in the game shares one floating motion

### M2 — Enemies, hazards, camera bounds ✅ DONE
- [x] `Enemy.cs`: `Die()` → `Instantiate` ammo drop + `Destroy`; `canBeStomped` flag *(landed in M1)*
- [x] `Enemy.controller` — its own state machine (Idle / Run on the `Run` float). **Reuses
      `Player_Idle.anim` and `Player_Run.anim` rather than duplicating them**: sprite-swap clips
      target the root SpriteRenderer via an empty path, so they work on any object that has one
- [x] **Enemy promoted to a real prefab.** It was a Player prefab instance with the `Player` script
      stripped and `Enemy` bolted on — which is why it kept inheriting new Player components
- [x] **`SpikyEnemy` as a prefab variant** — purple tint, `canBeStomped: false`, slower but with a
      longer chase range. Must be killed with a shuriken, which is what makes ammo matter
- [x] `Hazard.cs` + `Saw.prefab` — spinning trigger that hurts on contact and cannot be killed
- [x] `PatrolMover.cs` — offset of zero means "stay put", so **one Saw prefab covers both the fixed
      and the moving saw**; the level decides by setting the offset per instance
- [x] `PlayerHealth.cs` — 3 hearts, cherry heals, death reloads the current level by build index
- [x] `CinemachineConfiner2D` + a `CameraBounds` PolygonCollider2D in every level

**Design notes:**
- All damage now goes through one public entry point, `Player.TakeHit(direction)`. Collisions pass
  the contact normal; hazards have no contact point, so they pass the direction away from their own
  centre. `TakeHit` ignores calls while already hurt, which is what gives the invulnerability window.
- `Hazard` uses `OnTriggerStay2D` as well as `OnTriggerEnter2D`. Without Stay, a player knocked back
  but still overlapping the blade when invulnerability ends would never be hit again.
- The Saw has a **Kinematic** Rigidbody2D. A moving collider with no rigidbody forces Unity to
  rebuild the static collider tree every frame.
- `Player` now declares `[RequireComponent]` for `PlayerCombat` and `PlayerHealth` instead of
  null-checking them, so the dependency is enforced when the component is added.

**Acceptance:** both enemy types behave correctly, saws hurt, hearts deplete and refill, the camera
never shows past the tilemap.

> ⚠️ **`CameraBounds` polygon is a placeholder** covering the current tilemap (x −28..26, y −5..7).
> M4 resizes it per level once the levels are actually designed.

#### M2 follow-up (after playtest) — combat feel

**1. Damage timing.** `hurtDuration` was doing two jobs at once, which is why it felt wrong. Split
into two values, which is how platformers normally handle it:

| | Before | After | Why |
|---|---|---|---|
| Lose control (hit stun) | 0.5 s | **0.25 s** | Long control loss feels sluggish and unfair |
| Cannot be hit again (i-frames) | 0.5 s | **1.2 s** | 0.5 s meant a saw could drain all 3 hearts in 1.5 s |
| Sprite blinks while invulnerable | — | **yes, 0.08 s** | Without it the player cannot tell they are safe, so damage feels random |

`TakeHit` is now gated on `isInvulnerable` rather than `isHurt`, so control returns long before you
can be hurt again.

**2. Dive stomp.** Landing on an enemy no longer kills it — that just hurts you. Holding **S or Down
in the air** slams the player down at `diveSpeed`, and a kill needs all three of: coming down on top,
actively diving, and a currently-stompable enemy. Killing by landing is now a deliberate, committed
act rather than something that happens by accident.

**3. Spiked Enemy is beatable without ammo.** It cycles **3 s armoured / 1 s vulnerable** and changes
colour while vulnerable — that colour change is the player's only telegraph, so it doubles as the
warning. You can always eventually kill one with a well-timed dive, but the 1-in-4 window plus the
dive requirement keeps ammo clearly the easier answer.

> Note: `SpikyEnemy.prefab` (hand-written) was replaced by an Editor-made variant, renamed to
> **`SpikedEnemy.prefab`** in M2.5. Prefab variant YAML needs a generated anchor id, not
> `&100100000` — see `CLAUDE.md`.

> Correction (M2.5 audit): the bullet above originally said the Spiked Enemy was "slower but with a
> longer chase range". The actual prefab overrides **`moveSpeed: 6`** (*faster* than the base 4) and
> does **not** override `chaseRange` at all. That was a playtest tuning decision, so the asset was
> left alone and this text corrected to match it.

#### M2 follow-up 2 — two bugs found in playtest

**Shuriken despawned instantly.** A regression from adding the camera confiner: the `CameraBounds`
PolygonCollider2D is a trigger on the Default layer, and Projectile (9) vs Default (0) is enabled in
the collision matrix. Every shuriken spawned *inside* that level-sized trigger, so
`OnTriggerEnter2D` fired on the first physics step and destroyed it. Fixed twice over, because each
part was wrong on its own:
- `Projectile` now **ignores trigger colliders** — a projectile should be stopped by solid geometry
  and enemies, not by marker volumes
- The `CameraBounds` collider now sets **`m_ExcludeLayers` to everything**, so the camera hint takes
  no part in physics at all

**Stomp bounce was invisible.** The bounce was applied and then immediately erased: the stomp sets
`isDiving = false`, so on the next frame `ReadInput` saw "airborne, not diving, Down still held",
requested another dive, and `Dive()` overwrote the `+11` bounce with `-20`. Diving now needs a
**fresh key press** rather than a held key. The press is detected before the hit-stun early-return,
otherwise releasing the key while stunned would go unnoticed and swallow the next dive.

### M2.5 — Convention cleanup ✅ DONE
A short audit pass before M3, fixing things that had drifted from `CLAUDE.md` and from this file.

**1. Enemies no longer move by writing `transform.position`.** `Enemy.prefab` has a **Dynamic**
Rigidbody2D, and `Enemy.cs` was assigning `transform.position` every `Update`. That is the exact
anti-pattern M1 removed from the player: it teleports the body past the solver, so the enemy can end
up overlapping the tilemap and get shoved back out, which reads as jitter. `Enemy` now sets
`rb.linearVelocity` in `FixedUpdate`, the same movement model as `Player`.

**2. Enemies stop at ledges — and this is where `Physics2D.Raycast` comes back.**
`Enemy.HasGroundAhead` casts a short ray straight down from a point in front of the enemy's feet,
masked to `Ground`. No hit means the floor ends there, so the enemy holds position instead of
walking off. It still turns to face the player while waiting at the edge, so it reads as deliberate
rather than stuck. This makes platform layouts in M4 actually designable.

**3. Kinematic movers use `MovePosition`.** `PatrolMover` (saw) and `Platform` both moved a Kinematic
Rigidbody2D by transform, which teleports the collider instead of sweeping it through the physics
system. Both now use `rb.MovePosition` on `WaitForFixedUpdate`.

**4. `Platform` only carries the player when they are on top.** It used to parent the player on *any*
contact, so brushing the side of the platform in mid-air dragged them along. Now gated on the
contact normal (`normal.y < -0.5f` — the normal points from the other object back towards the
platform, so the player being above gives a downward normal).

**5. Style fixes.** Deleted the empty `Update()` stub in `GameCanvasHandler`. `Platform` and
`PatrolMover` converted to `[SerializeField] private` + `[RequireComponent]`.

**6. Renames** to match the `CLAUDE.md` PascalCase rule: `Spiked Enemy.prefab` → `SpikedEnemy.prefab`,
`Cube Obstacle.prefab` → `CubeObstacle.prefab` (file, `.meta`, and the `m_Name` overrides in all
three levels). GUIDs preserved, so every scene reference survived.

**7. Player rigidbody** set to `Interpolate` + `Continuous` collision detection — the camera tracks
the player, so interpolation is a free smoothness win, and the dive moves at 20 u/s.

**Acceptance:** enemies chase smoothly without jitter and stop at platform edges; the saw and the
moving platform still move; riding the platform still works; nothing in the Console.

#### M2.5 follow-up (after playtest) — two real bugs

**1. The player juggled on a descending platform.** Parenting was the cause, and it had been the
cause all along — M2.5 only made it more visible by switching the player to `Interpolate`.

Parenting a **Dynamic** Rigidbody2D to a moving transform drives it *twice* per physics step: once
because the parent transform drags the child's world position down by Δ, and again because the
player's own gravity integration adds its own fall on top. The player therefore ends each step
buried inside the platform, gets depenetrated back out, and the cycle repeats — which reads as
bouncing. It is worst on the way down, because on the way up gravity happens to cancel part of it.

The fix is to stop moving the player by two mechanisms at once. `Platform` now measures its own
velocity and hands it to the player; `Player.Move` adds it:

| | Before | After |
|---|---|---|
| Horizontal carry | `transform.SetParent(platform)` | `velocity.x += carrier.Velocity.x` |
| Descending | player free-falls, re-lands every step | `velocity.y` matched to the platform's |
| Who owns the player's position | platform **and** the player's own body | the player's body alone |

`Platform` still decides *whether* the player is riding, because it is the side that can see the
contact normal and tell "standing on top" from "brushing a side". That check moved from
`OnCollisionEnter2D` to **`OnCollisionStay2D`**: sliding off the side onto the top never raises a
second Enter, so an Enter-only test could miss the moment the player actually gets on.

> ⚠️ **This removed the last `SetParent` and the last `Time.deltaTime` from the project.**
> Both are on the "must survive" checklist and are now owed by M3 — see the checklist above.

**2. Enemies were stopped by a knee-high cube.** The M2.5 ledge check made them cautious but not
capable: anything solid in front simply blocked them. They now **jump obstacles they can actually
clear**, using two forward rays:

| Ray | Height above feet | Meaning |
|---|---|---|
| Blocked | +0.1 | something is in the way |
| Clearance | `maxJumpableHeight` (1.5) | if this one is **clear**, it is short enough to hop |

Both blocked = a wall, so the enemy stops instead of head-butting it forever. Blocked low but clear
high = a step, so it jumps. `jumpForce` 10 against `gravityScale` 2 reaches about 2.5 units, so the
1.5 it is willing to attempt has a comfortable margin — it never commits to a jump it cannot make.

The three checks share one **`solidLayers`** mask instead of the old `groundLayer`, because
`CubeObstacle` sits on **Default**, not `Ground` — a Ground-only mask cannot see the very obstacle
this is meant to solve. The mask is Ground + Default (`m_Bits: 65`).

While airborne the enemy keeps the direction it jumped in and re-runs none of the three checks,
otherwise it would stall halfway over the obstacle.

> Note: the enemy has no jump animation — `Enemy.controller` only has Idle and Run. It slides
> through the air in its run pose. Adding a jump state means giving the controller a Bool parameter
> and reusing `Player_Jump.anim`; say the word if it looks bad in motion.

#### M2.5 follow-up 2 — three bugs from the second playtest

**1. Enemies stopped moving entirely.** A self-inflicted regression from follow-up 1. `solidLayers`
was set to Ground **+ Default** so the enemy could see `CubeObstacle`, which sat on Default. But
`CameraBounds` is a **level-sized trigger that also sits on Default**, and `Physics2DSettings` has
both `m_QueriesHitTriggers: 1` and `m_QueriesStartInColliders: 1`. So every enemy query started
inside that trigger and reported a hit:

| Check | Result | Consequence |
|---|---|---|
| `IsBlockedAhead` | always true | something is always in the way |
| `CanClear` | always false | and it is always too tall to jump |

…which lands on `moveDirection = 0f` every frame. The enemy was frozen, not failing to chase.

Fixed at the source rather than by special-casing:
- `solidLayers` back to **Ground only** (`m_Bits: 64`), with a comment on the script field saying
  why Default must never go in it.
- **`CubeObstacle` moved to the `Ground` layer**, which is where a solid piece of scenery belonged
  anyway — that is what `Ground` means in `CLAUDE.md`.
- **`CameraBounds` moved to layer 2 (`Ignore Raycast`)** in all three levels. Its `m_ExcludeLayers`
  already kept it out of *collisions*, but that does nothing for *queries*. This is the second bug
  that shape has caused (the first ate every shuriken in M2), so it is now off Default for good.

**2. The player stuck to walls when jumping into them.** Nothing to do with the platform work: the
project has no default physics material (`m_DefaultMaterial: {fileID: 0}`), so every collider was
using Unity's built-in **friction of 0.4**. Holding a direction into a wall while airborne generated
enough friction against the tilemap to hold the player up.

Added `Assets/Materials/NoFriction.physicsMaterial2D` (friction 0, bounciness 0) on the player's
`CapsuleCollider2D`. Zero friction is safe here precisely because horizontal movement is set as a
velocity every `FixedUpdate` rather than driven by friction — nothing about walking or standing
depends on it.

**3. The player still bounced on the platform.** The velocity carry from follow-up 1 was correct in
principle but almost certainly never ran: it depended on `Platform` reading a contact normal to tell
"on top" from "at the side", and which way that normal points depends on which of the two colliders
Unity reports first — the one thing in follow-up 1 that could not be verified without playing.

Rather than guess the comparison a second time, the question is now answered by something that
already knows: **`Player.CheckGround` reports what is under the player's feet**, so it sets `carrier`
directly. One source of truth, no normals involved, and the 0.15-unit ground-check radius gives
tolerance that a hard contact test does not.

Two supporting fixes:
- **Gravity is now subtracted in advance** when matching a descending platform. Unity applies gravity
  during the physics step *after* `FixedUpdate`, so setting velocity to exactly the platform's speed
  still ended the step slightly faster than it — sinking in and being pushed back out every step.
- **The platform's Rigidbody2D is now `Interpolate`**, matching the player. A rider rendered on the
  interpolated clock and a carrier rendered on the fixed clock drift apart visually between physics
  steps, which looks like bouncing even when the physics is correct.

`Platform` is now purely a mover that publishes its own velocity — it has no collision callbacks at
all, which is what cost the project its last `OnCollisionExit2D` (see the owed table at the top).

### M3 — Audio, PlayerPrefs, HUD, pause, flow ✅ DONE
- [x] `AudioManager.cs` — `DontDestroyOnLoad` singleton, separate music + SFX `AudioSource`s,
      `PlayOneShot`, volumes backed by `SaveSystem`, track chosen from `SceneManager.sceneLoaded`
- [x] `SaveSystem.cs` — static wrapper over `PlayerPrefs` (unlock level, volumes, reset)
- [x] HUD rework — `GameCanvasHandler` **renamed to `PickupBanner`** and reduced to just the
      banner; the counters moved to the new `Hud`. That is what fixes *defect #3*: the two fields
      that pointed at the same TMP object are gone entirely rather than being re-pointed
- [x] `PauseMenu.cs` — Esc, `Time.timeScale = 0`, Resume / volume sliders / Quit to Menu
- [x] `LevelExit.cs` — exit trigger → save unlock → load next scene
- [x] Intro screen — Continue vs New Game driven by the saved unlock
- [x] All owed checklist items except `Transform.Translate` *(see the table at the top)*
- [x] **Fall death** — nothing killed a player who fell into a pit; they fell forever
- [x] **`Enemy` physics queries moved to `FixedUpdate`** — all four ran in `Update`, which
      `CLAUDE.md` forbids and which M2.5 had already fixed on the player
- [x] **`CubeObstacle` re-hits** — see below

**Design notes:**

**1. The audio calls are live, not commented.** This is a deliberate change to the original
agreement, and it is strictly less work: `AudioManager.PlaySfx` is **static** and swallows both a
null clip and a null `Instance`, so every call site is one plain line that does nothing until a
clip is assigned. Finishing the audio is now "drop clips into the Inspector" with no code editing
at all, instead of "drop in clips *and* find and uncomment eight lines". The honesty marker moves
with it: the Audio box stays unticked until clips are actually assigned.

**2. `CubeObstacle` is a static hazard, and now behaves like one.** It is the fixed counterpart to
the Saw — touch it and you get hurt. It kept the `Obstacle` tag and the `Ground` layer (it *is*
solid, and enemies need to see it to hop it), but only `OnCollisionEnter2D` was checking it, so the
player could stand on top of a damaging block indefinitely: once the invulnerability expired no
second Enter was ever raised. `Player` now has **`OnCollisionStay2D`** for exactly the reason
`Hazard` has `OnTriggerStay2D`. Its prefab was also **Dynamic** with all three levels overriding it
to Kinematic; the prefab is now Kinematic and the overrides are gone.

**3. The HUD reads, it is never told.** Hearts, coins and ammo are all already public read-only
state on the player, so `Hud` polls them in `Update` and compares against what is currently drawn
before writing. Nothing in the gameplay code knows a HUD exists, and the display cannot get out of
sync with the game the way an event-driven HUD can when one call site is forgotten.

**4. The HUD and pause menu are separate prefabs, each with its own Canvas.** Two reasons. A canvas
is rebuilt as a unit, so the pause menu changing cannot force the HUD to rebuild. And a prefab with
its own canvas drops into a scene as a plain root object, which is far less fragile to write by
hand than re-parenting into an existing canvas. Sorting orders: level canvas 0, HUD 1, pause 2.

**5. Pause has to gate input explicitly.** `Time.timeScale = 0` stops `FixedUpdate` but *not*
`Update`, so `Player.ReadInput` and `PlayerCombat.Update` both check `PauseMenu.IsPaused`. Without
it the mouse click that presses Resume would also throw a shuriken.

**6. Coyote time uses the velocity, not a flag.** `OnCollisionExit2D` opens the window only if the
player is moving downwards or level — after a jump they are moving up, so it does not arm and
cannot grant a second jump. That replaces the "did I leave the ground by jumping?" bool that this
otherwise needs.

**Acceptance:** volume and unlock survive a full quit and relaunch; pause freezes everything;
finishing a level unlocks the next.

#### M3 follow-up (after first playtest) — HUD size, Options screen, intro rebuild

**1. HUD icons doubled.** Hearts 48 → 96 px, coin and shuriken 40 → 80 px, counters 32 → 48 pt,
rows re-spaced to match. All four numbers are RectTransform sizes in the Inspector, so they are
easy to keep tuning:

| Thing | Where |
|---|---|
| Heart size | `Heart.prefab` → RectTransform Width/Height |
| Coin / shuriken size | `Hud.prefab` → `CoinIcon` / `AmmoIcon` → Width/Height |
| Gap between hearts | `Hud.prefab` → `Hearts` → Horizontal Layout Group → Spacing |
| Counter text size | `Hud.prefab` → `CoinCounter` / `AmmoCounter` → Font Size |

**2. Volume moved behind an Options button, shared by both menus.** The pause menu is now
**Continue / Options / Quit to Menu**, and the intro screen is **New Game / Continue / Options**.
Pressing Options hides the button column and shows the sliders in the same place; Back returns.

`OptionsPanel.cs` is written so it knows nothing about either menu — whoever opens it passes in the
screen it should replace, and Back puts that screen back. That is what lets one script serve both.
The panel's *objects* are emitted into both the prefab and the scene rather than being one nested
prefab, because a nested `PrefabInstance` inside a prefab and a re-parented instance inside a scene
are both markedly more fragile to write by hand than two copies of a generated subtree. The
behaviour lives in the one shared script, which is the part that matters.

> `OptionsPanel` ships **active** and switches itself off in `Awake`. An object that starts inactive
> never receives `Awake` at all, so its sliders would still be null the first time something called
> `Open()`.

**3. The intro screen was rebuilt, and that is what fixes the unclickable Continue button.** The
cause was sibling order: uGUI draws and raycasts children in order, and the new Continue button had
been added as the **first** child of the Canvas, so the full-screen translucent `Panel` — second in
the list, with Raycast Target on — sat on top of it and swallowed every click. New Game worked only
because it happened to be third, above the panel.

Rather than re-order one button, every child of the Canvas was regenerated: title and subtitle on
the left, one large frog on the right (the eight compass-point copies are gone), a dark band behind
a centred button column, and a version label. Generating it means the order is correct by
construction and every decorative element has **Raycast Target off**, so this class of bug cannot
come back.

**Acceptance:** the HUD is readable at a glance; Options opens and closes from both menus and the
volume set in one is what the other shows; all three intro buttons respond.

#### M3 follow-up 2 — menus rebuilt on layout groups

**1. Both menus are now laid out by uGUI instead of by hand.** Every button column is an
`Image` + `VerticalLayoutGroup` + `ContentSizeFitter`:

| Was | Now |
|---|---|
| each button at a written-down `anchoredPosition` | the layout group spaces them |
| each button 420 px wide | width comes from the column (`Child Control Width`) |
| the band a fixed 1180 × 470 | the fitter derives its height from what is inside |

That is what makes **hiding Continue close the gap**: `SetActive(false)` removes it from the
layout, so New Game and Options move up and the band shrinks around them. There is no second set
of positions for the no-save case, because there are no positions at all.

What is left as a plain number is deliberate and is one field each: the column's **width**, the
group's **Spacing** and **Padding**, and each button's **height**. `Child Control Height` is off on
purpose so a button's height stays a single editable value rather than needing a `LayoutElement`.

**2. Intro button order is now Continue / New Game / Options** — the returning player's button
first. Order is hierarchy order, so it is changed by dragging in the Hierarchy, not in code.

**3. The vertical "Options" text.** The four buttons are byte-for-byte identical in the asset, so
nothing was wrong with that button specifically — it was a layout-timing fault, and it needed two
fixes:

- **Root cause.** `OptionsPanel` used to ship *active* and switch itself off in `Awake`, which
  meant that when the pause panel was enabled the options panel was activated and deactivated
  inside the same frame. That queues a layout rebuild which is then discarded, and TextMeshPro can
  come back with stale zero-width geometry — a label wrapping at zero width renders one letter per
  line, which is exactly what "vertical" looks like. It now **ships inactive** and wires itself on
  first `Open()` instead, so the churn never happens. (An inactive object never receives `Awake`,
  which is why the wiring had to become lazy rather than just moving.)
- **Second, independent guard.** Every button label and menu label now has **text wrapping off**.
  These are single-line labels by definition, so this is right on its own terms — and with wrapping
  off, a narrow rect can only ever clip, never stack letters vertically. The failure mode is gone
  rather than merely fixed.

> The HUD was left on its existing layout. Its hearts already use a `HorizontalLayoutGroup`, the
> two counter rows work, and rebuilding a working part of the UI is risk without a request behind
> it. Say the word if you want the counters on a layout group too.

**Acceptance:** with no save, the intro shows New Game and Options with no gap above them; after
finishing a level it shows all three. No label renders vertically in either menu.

#### M3 follow-up 3 — checkpoints, carried state, clocks removed

**1. The checkpoint *is* the save.** There is no separate "saved game" record. Touching a
checkpoint writes where you were and what you were carrying, and that one record answers all three
questions the game ever asks:

| Situation | What happens |
|---|---|
| You die | reload the level, stand at the checkpoint, restore ammo and coins |
| You finish a level | same record written for the *next* level, with no position |
| You quit and press Continue | load the level the record names, then the same restore runs |

One piece of state means the three can never disagree. All of it lives in
`SaveSystem`, and `Player.RestoreFromSave` in `Start` is the single place it is read back — so it
covers dying, level transitions and relaunching without three separate code paths.

**2. Touching a checkpoint heals you to full.** That is what makes "full hearts" the honest answer
everywhere, and it is why the save does **not** store a heart count at all: the answer is always
"max", so there is nothing to remember and no way to end up stranded at a hard checkpoint on one
heart. Ammo and coins *are* stored, and rewind with you — so coins collected after the checkpoint
come back into the level and cannot be double-counted.

**3. Coins are a running total across the whole game.** `LevelExit` records the carried ammo and
coins against the *next* level before loading it, with no position, so the next level uses its own
spawn point but you keep what you earned.

**4. `PlayerCombat` now sets its starting ammo in `Awake`, not `Start`.** `Player.Start` restores
the saved ammo over the top, and the order of `Start` between two components on the same object is
undefined — so this was a genuine race that would have worked or not depending on script order.
Awake always runs before any Start, which makes it deterministic.

**5. The checkpoint's feedback is deliberately not the pickups'.** Pickups bob gently and forever,
which reads as *come and take me*. A checkpoint does a single sharp scale pulse and then holds its
lit colour, which reads as *done, claimed*. Same reasoning as the M1 decision to give the ammo drop
different motion from the thrown shuriken: one glance should say which is which. Dormant
checkpoints are grey; the one you respawn at shows itself already lit, without replaying the pulse.

**6. The pause menu's two clocks are gone**, as requested — the panel is now just the title and the
three buttons.

> ⚠️ **Checklist note.** The clocks were where `Time.unscaledDeltaTime` was demonstrated, and it is
> now **not used anywhere**. No checklist box breaks: the tracked item is `Time.deltaTime`, which is
> still used twice — the pickup-banner fade and the new checkpoint pulse — and `unscaledDeltaTime`
> only ever appeared as supporting detail in the explanation of `timeScale`. Worth knowing if you
> get asked about it in the exam: the honest answer is that `timeScale = 0` is demonstrated by the
> pause itself, and unscaled time is what you would reach for if something had to keep moving
> while paused.

**Acceptance:** dying returns you to the last checkpoint on full hearts with the ammo and coins you
had when you touched it; finishing a level carries ammo and coins into the next; quitting entirely
and pressing Continue puts you back at the same checkpoint with the same inventory; New Game clears
all of it.

#### M3 follow-up 4 — sorting order, platform bounce, banner removed, bigger cherries

**1. Sprite sorting order is now explicit.** Every sprite in the game was on sorting layer
`Default` at order **0**, including the tilemap — so when two overlapped, which one drew in front
was left to Unity's fallback ordering. That is why the checkpoint flag cut through the player. The
project now has a stated ladder, all still on `Default`:

| Order | What | Why |
|---|---|---|
| 0 | tilemap, platforms, cube, checkpoint, level exit | the world and the props standing in it |
| 5 | coins, cherries, ammo drops | pickups read on top of scenery |
| 10 | enemies, saws | actors above pickups |
| 20 | **player** | always in front of everything it walks past |
| 25 | shuriken | in flight, above everything |

**2. The platform bounce at the top: `MovePosition` was the cause.** Follow-up 4 fixed carrying
riders by velocity, but the *platform itself* was still moved with `rb.MovePosition`, and that is
not a neutral way to move a kinematic body — it hands the body a velocity for that step so the
solver can sweep it. A rider in contact with an upward-moving body gets pushed upward too, and
that borrowed velocity was still on the player when the platform stopped, so it kept going.

`Platform` now assigns `rb.position` directly. The platform's velocity stays zero, the contact
transfers nothing, and the rider is moved by exactly one thing — the explicit position carry.
Sweeping bought nothing here anyway: the platform moves a few centimetres per physics step.

> This is the same lesson as the original rework, one level down: *anything* that gives the rider
> a second source of motion reintroduces the bounce, and `MovePosition` was a second source hiding
> in plain sight.

**3. The "item collected" banner is gone entirely**, along with `PickupBanner.cs`, the `ItemInfo`
objects and — since it was the only thing left on it — the now-empty `Canvas` in all three levels.
The HUD already shows every count.

That removal took two demonstrated APIs with it, and both are repaid by changes that stand on
their own merits rather than as box-ticking:

| Lost with the banner | Repaid by |
|---|---|
| `StopCoroutine` + stored handle | `Player.EndHurtEarly` — a checkpoint heals you, so it now also cuts the hurt-blink short. Without it a checkpoint could leave you flashing, or worse, leave the sprite switched off mid-blink |
| `Image.sprite` at runtime | `Hud` takes the coin and shuriken icons from the pickup prefabs' own `Item.itemSprite`, so the art is recorded in one place instead of two that can disagree |
| `Graphic.enabled` | HUD hearts now hide via `Image.enabled` instead of `SetActive` — see below |

> `CanvasGroup` also went with the banner. It was never on the course checklist; it was an extra
> the fade happened to use, so nothing is owed.

**4. Empty hearts hide the Image, not the GameObject.** `SetActive(false)` would remove the heart
from the `HorizontalLayoutGroup` as well, so the remaining hearts would slide sideways every time
one was lost. Disabling just the `Image` keeps the slot and stops it drawing, so the row never
moves. `SetActive` is still demonstrated in eleven other places.

**5. Cherries enlarged again**, 96 → **132 px**, with the counter rows pushed down to clear them.
They are the resource that decides whether you live, so they are now the biggest thing on the HUD.

**Acceptance:** the player always draws in front of the checkpoint flag; riding the platform to the
top no longer pops the player off it; no pickup banner appears; the heart row does not shift as
hearts are lost.

#### M3 follow-up 5 — fall death made responsive, dash and slide

**1. Fall death was never missing — it was invisible.** `PlayerHealth` has killed the player below
a set height since M3, but that height was a fixed `-20` while `CameraBounds` in Level1 bottoms out
at `y = -5`. So the player dropped out of sight and then fell silently for another 15 units, about
**1.2 seconds of nothing**, before the level reloaded. It read as broken.

The death line is now **measured from the level's own camera bounds** —
`BoundingShape2D.bounds.min.y - fallDeathMargin` — because those bounds already describe where the
level is, and writing a second number for the same thing means keeping two things in step by hand.
Falling out of view now kills in about a fifth of a second, and when M4 resizes a level's bounds
the death line follows on its own. The old fixed height survives only as a fallback for a level
with no bounds.

**2. Dash (Shift).** In the air it is a straight horizontal dash in the facing direction; on the
ground it is a **slide**. Gravity is switched off for the duration, so the dash covers the same
distance every time instead of a shorter, drooping one when entered while already falling. It
grants **no invulnerability**, so being hit cancels it — which it has to, or `TickDash` would keep
overwriting the knockback velocity.

Two different limits, because they solve different problems:

| | Limit | Why |
|---|---|---|
| Air | **once per jump**, restored on landing | a chain of air dashes would cross gaps the level never intended; one dash makes the maximum air distance a fixed number M4 can design around |
| Ground | **cooldown** (0.6 s) | nothing to bound here, it just should not be spammable |

**3. The slide shortens the player, so low gaps become passable.** The whole object is scaled to
`slideHeightScale`, which shortens the capsule with it. *(Superseded in follow-up 10: the collider
is now stated outright as `slideColliderSize` and taken from the Slide artwork.)* Scaling happens around the transform origin, so the body is moved down by exactly
how far the bottom of the capsule moved, which keeps the feet planted. The sprite pivot is centred
and the capsule bottom sits exactly at the sprite bottom, so one correction fixes both.

**Getting out again is the part that needed care.** A slide that simply ended would pop the player
back to full height inside a ceiling. Instead:

- `TryStand` runs every physics step and only stands up when there is **headroom**.
- The headroom test is an `OverlapBox` covering *only the band between the crouched head and the
  standing head*. Testing the whole standing capsule would find the floor the player is stood on
  and decide there was never room.
- While still stuck low, the player keeps walking at `crouchSpeedScale` (half speed). That is what
  makes a tunnel **longer than one slide** crossable — otherwise they would stop dead inside it
  with no way out. Jumping is blocked while crouched, since there is by definition a ceiling.

**4. Afterimages.** `AfterImage.prefab` is a bare SpriteRenderer that copies whichever animation
frame the player was on, then fades and destroys itself. Because it copies the sprite, the facing
and the scale, the trail crouches too when the dash is a slide. Nothing keeps a reference to it -
the player spawns and forgets.

#### M3 follow-up 6 — dash art, and dashing through enemies

**1. `Art/Player/Dash.png` is now the dash pose**, which removed a workaround. The sprite is a
flattened frog on the same 32×32 frame as every other player animation with the same centred
pivot, so its feet line up with Idle and Run for free.

Because the sprite itself now reads as *low*, the slide no longer squashes the transform. It
shortens the **collider only**: the capsule height is scaled and its offset moved by half of
whatever was removed, which keeps the bottom — the feet — exactly where it was. That is simpler
than the old scale-and-compensate, and it removes an entire class of problem, since the transform
scale is never touched at all now.

**2. Animator.** `Player_Dash.anim` plus a `Dash` **bool** on `Player.controller`, driven by
`isDashing || isCrouched` — the flat pose is the right shape for shuffling under a ceiling too,
not just for the dash itself. It follows the pattern already in the controller: **AnyState →
Player_Dash** while `Dash` is true, and two ways out so the pose does not outstay its welcome:

| Leaving the dash | Goes to | Why |
|---|---|---|
| `!Dash && IsGrounded` | `Idle` | landed, or the slide ended with headroom |
| `!Dash && !IsGrounded` | `Player_Fall` | an air dash that ended mid-air would otherwise show the *idle* pose all the way down |

**3. Dashing passes through enemies, and only enemies.** Done with **`Rigidbody2D.excludeLayers`**
rather than `Physics2D.IgnoreLayerCollision`, for two reasons: it is per-body, so it cannot leak
into anything else in the scene, and it is restored automatically when the dash ends or the object
is destroyed. Excluding the layer switches off contacts entirely, so no damage is taken on the way
through either — which is consistent with the dash having no invulnerability, because it is not
invulnerability, it is simply not touching them.

The mask is a serialized `Dash Pass Through Layers` set to **Enemy** only. Scenery is deliberately
not in it — dashing through walls would let the player leave the level.

> If a dash *ends* while still overlapping an enemy, contacts resume and the player takes the hit.
> That is intended: the dash covers about 3.2 units against an enemy roughly 1.5 wide, so stopping
> inside one means you dashed into it rather than through it.

#### M3 follow-up 7 — afterimage tuning, and no damage while dashing

**1. The afterimage was too sparse and lasted too long.** The numbers made this inevitable rather
than it being a matter of taste:

| | Was | Now | Why |
|---|---|---|---|
| Spawned from | `FixedUpdate` | **`Update`** | the physics step is a fixed 0.02 s, so the trail was capped at one ghost per step. Update also uses the *interpolated* transform, which is where the sprite is actually drawn |
| Interval | 0.035 s | **0.018 s** | 4 ghosts 0.7 units apart became ~9 about 0.36 units apart — a trail rather than a row of clones |
| Lifetime | 0.28 s | **0.2 s** | it outlived the 0.16 s dash, so every ghost was still hanging in the air after the player had stopped |
| Fade | linear | **squared** | a linear fade spends half its life at half opacity, which is exactly where a ghost reads as a second character standing there |
| Start alpha | 0.55 | **0.45** | |

All five are Inspector fields — `After Image Interval` on the Player, `Lifetime` and `Tint` on
`AfterImage.prefab` — so the look stays tunable without touching code.

**2. Dashing no longer takes damage from enemies.** `excludeLayers` stops the contact arising, but
a contact already in progress when the dash *began* can still be delivered for a step, which is
enough to lose a heart on the way in. The rule is now also stated directly in
`Player.TryTakeContactDamage`: while dashing, an `Enemy` contact is ignored outright. Relying on
the physics engine to imply a gameplay rule was the mistake; the engine gives the pass-through, the
guard guarantees the consequence.

**Acceptance:** the dash leaves a smooth trail that is gone by the time the player stops; dashing
into and through an enemy costs no health.

#### M3 follow-up 8 — the slide floated, and the dash still cost a heart

Both of these were real bugs in follow-up 5/7, and both had the same shape: a rule that was *stated*
somewhere but not *true* everywhere.

**1. The slide floated and then dropped, because a vertical capsule cannot be shorter than it is
wide.** The player's capsule is 1.5 × 2 and **vertical**. Crouching asked for 1.5 × 0.9 — but Unity
clamps a vertical capsule to a circle of its own width, so the shape stayed **1.5 tall** while the
offset had already been lowered by 0.55 to keep the feet planted. Net result: the collider sat
**0.3 units inside the floor**, the solver pushed the player up out of it, and standing again
dropped them back down.

| | Intended | What actually happened |
|---|---|---|
| Crouched shape | 1.5 × 0.9 | 1.5 × **1.5** (clamped) |
| Bottom of collider | −1.0 | **−1.3** |

The fix is one line, and it is the right shape anyway: the capsule is switched to
**`CapsuleDirection2D.Horizontal`** while crouched. A horizontal capsule 1.5 wide and 0.9 tall is
perfectly valid — wide and flat is what a slide *is* — and the bottom lands back on −1.0 exactly.

**2. Dashing through an enemy still cost a heart, because the pass-through ended too early.** The
guard added in follow-up 7 was keyed on `isDashing`, and `EndDash` restored collisions immediately.
So a dash that *finished inside* an enemy — easy, since enemies chase toward you — handed the
player a contact and a heart on the very next physics step. The guard was correct; its window was
too short.

The pass-through now **outlives the dash**. `UpdateEnemyPhase` ends it only once the player is
genuinely clear of every enemy (`Physics2D.OverlapCapsule` against the same mask — `excludeLayers`
filters contacts, not queries, so it can still see them), or once `Enemy Phase Grace` (0.4 s)
expires. The grace is what stops a player who parks inside an enemy from being permanently
untouchable. The damage guard is keyed on that phase rather than on the dash.

**Acceptance:** the ground slide stays flat on the floor with no float and no landing drop; dashing
into, through and out the far side of an enemy costs no health.

#### M3 follow-up 9 — the dash animation sometimes did not play

**Symptom:** occasionally a ground dash moved the player but left the *idle* pose on screen, with no
obvious pattern to when.

**Cause: transition blend times longer than the dash.** Four transitions in `Player.controller`
carried Unity's default **0.25 s** blend — `Idle → Run`, `Run → Idle`, `AnyState → Player_Hurt` and
`Player_Hurt → Idle` — and all of them had `Interruption Source: None`, which means *a transition
already in progress cannot be interrupted at all*.

The dash lasts **0.16 s. The blend lasts 0.25 s.** So pressing Shift during an `Idle ↔ Run` blend —
which is running every time the player starts or stops moving, i.e. constantly — blocked the
`AnyState → Player_Dash` transition outright. By the time the blend finished, `Dash` was already
false again, so the dash state was never entered. That is the whole of the "sometimes".

**Fix: every transition duration is now 0.** This is not a workaround, it is what this project
should always have had — **every clip in the game is a sprite swap**, and crossfading a sprite swap
blends nothing. There is no in-between sprite to interpolate to; Unity simply picks a moment during
the blend to switch. So the 0.25 s bought no visual smoothing whatsoever, and paid for it with a
quarter-second window in which the state machine ignored input.

The same two 0.1 s blends were found and cleared on `Enemy.controller` for the same reason.

> Worth remembering for M4: the Animator's defaults are tuned for blended humanoid rigs. For
> sprite-swap 2D, **Has Exit Time off and Transition Duration 0** is the correct default for every
> transition, and anything else is a latency bug waiting to happen.

**Acceptance:** the dash pose appears every single time, including when Shift is pressed in the
instant the player starts or stops running.

#### M3 follow-up 10 — `Art/Player/Slide.png`, and a collider that matches it

**1. The ground slide has its own art now, so it has its own animator state.** The two moves are
genuinely different pictures, so they are no longer sharing one:

| Move | Parameter | State | Sprite |
|---|---|---|---|
| Air dash | `Dash` = `isDashing && !isCrouched` | `Player_Dash` | `Dash.png` |
| Ground slide | `Slide` = `isCrouched` | `Player_Slide` | `Slide.png` |

They are mutually exclusive by construction — a ground dash crouches, an air dash does not — so no
ordering problem between the two AnyState transitions. `Slide` is keyed on `isCrouched` rather than
on the dash, which means the flat pose correctly stays on while shuffling along under a low ceiling
after the dash itself has finished.

**2. The collider is now taken from the artwork instead of from a scale factor.** `Slide.png` draws
the frog across the full 32 px width and 14 px tall at the bottom of the frame; at 16 PPU that is
exactly **2 × 0.875 units**. So the field is no longer a fraction to reason about — it states the
size outright:

```
slideColliderSize = (2, 0.875)     // the sprite, in world units
```

The capsule is anchored by its **bottom** rather than its centre, so the feet stay on the floor
whichever size is in use — that is the one thing that has to hold regardless of what the art does,
and it is now the only piece of arithmetic left in the crouch. It also stays a valid *horizontal*
capsule, since 2 > 0.875.

Note the slide is **wider** than the standing capsule (2 vs 1.5) as well as much shorter. That is
faithful to the drawing — the frog really does stretch out — and it means the player stops against
a wall exactly where the sprite touches it.

**Acceptance:** the ground dash plays the slide sprite, the body underneath is the same size and
shape as the drawing, the feet stay on the floor, and a one-tile gap is still passable.

**Acceptance:** falling off the map kills you almost as soon as you leave the screen; Shift in the
air dashes once per jump; Shift on the ground slides you under a one-tile gap and you keep shuffling
until there is room to stand; taking a hit cancels a dash; dashing carries you through an enemy but
never through scenery.

### M4 — Level design & build
- [ ] Layout spec for **Level 1 (teach)**, **Level 2 (test)**, **Level 3 (twist)** — ASCII grid on
      tilemap cell coordinates, the specific `Terrain (16x16)_N` tile for every cell, exact prefab
      world coordinates, and a per-section note on why it plays well
- [ ] User paints the tilemaps; prefabs placed via scene YAML from the spec
- [ ] `CubeObstacle.prefab` sprite repointed off `Library/PackageCache/` *(defect #10)*
- [ ] **Scrolling background element using `Transform.Translate`** — repays the checklist item lost
      in the M1 player refactor. A non-physics decorative object is the honest place for `Translate`,
      since the whole reason it left `Player` is that it must not be used on a Rigidbody2D
- [ ] Balance pass from playtest notes

**Acceptance:** 8–12 min playtime, every checklist box ticked, playable start to finish.

---

## Manual steps for the user

Running list. Newest section at the bottom.

### After M0
1. **Open the project in Unity** and let it reimport. Check the Console is clean — in particular
   there should be **no** "missing script" warning on `IntroScene`'s Canvas.
2. **Verify in `Edit → Project Settings → Tags and Layers`** that layers 6–11 read
   `Ground`, `Player`, `Enemy`, `Projectile`, `Hazard`, `Pickup`, and that Sorting Layers lists
   `Background`, `Default`, `Foreground` in that order.
   - ⚠️ Unity initially rejected the hand-written sorting layers (only `Default` showed). The file
     has been rewritten in Unity's exact format. **If only `Default` still appears, just add
     `Background` and `Foreground` via the `+` button** — it takes 20 seconds and is guaranteed
     correct. Tell Claude afterwards so it can read back the IDs Unity generated.
3. **Verify in `Edit → Project Settings → Physics 2D → Layer Collision Matrix`** that exactly these
   are unchecked: Player✗Projectile, Projectile✗Projectile, Projectile✗Pickup, Enemy✗Pickup,
   Hazard✗Pickup, Pickup✗Pickup. *(This was written as a raw hex bitfield — worth eyeballing.)*
4. **Verify `File → Build Profiles`** lists `IntroScene` at index 0 and `Level1` at index 1.
5. **Duplicate the levels:** select `Assets/Scenes/Level1.unity`, press `Ctrl+D` twice, and rename
   the copies to **`Level2`** and **`Level3`**. *(Needed before build settings can be finalised.)*
6. **Save and commit** once Unity has reimported, so any re-serialisation is captured.

> Reminder: **close Unity before the next milestone**, since scene and prefab YAML will be edited
> directly. Commit first.

### After M1
1. **Open Unity and check the Console.** Everything below was written as raw YAML, so this is the
   first real test of it.
2. **⚠️ Highest-risk item — select `Assets/Prefabs/Player.prefab` and check the `Player` component's
   `Ground Layer` field shows `Ground` ticked.** A `LayerMask` is serialised as a nested
   `m_Bits` value and that format was written by hand. **If it reads `Nothing`, set it to `Ground`
   manually** — otherwise `CheckGround()` always returns false and the player can never jump.
3. On the same prefab, confirm `PlayerCombat` is present and its `Shuriken Prefab` field points at
   `Shuriken`.
4. Select the `Enemy` in `Level1` and confirm: layer is `Enemy`, `Ammo Drop Prefab` is `AmmoPickup`,
   `Can Be Stomped` is ticked, and there is **no** `PlayerCombat` component on it.
5. **Playtest `Level1`:**
   - move and jump — should feel tighter than before, and you should not be able to clip into tiles
   - **throw a shuriken with left Ctrl or left mouse button** (`Fire1`). You start with 3.
   - **jump on the enemy's head** → it dies, you bounce, and it drops a shuriken pickup
   - **walk into the enemy from the side** → you get hurt and knocked back, as before
   - shuriken should stop on walls and vanish after ~3 s if it hits nothing
6. **Report how it feels** — move speed, jump height, throw cooldown, stomp bounce, shuriken speed.
   These are all single numbers I can tune.

### After M2
1. **Open Unity and check the Console.**
2. **⚠️ Highest-risk item — open `Assets/Prefabs/SpikedEnemy.prefab`.** It is a **prefab variant**,
   written by hand as a `PrefabInstance` pointing at `Enemy.prefab`. It should open showing a purple
   enemy with `Can Be Stomped` unticked, and the Inspector header should say it is a variant.
   **If it fails to open or looks wrong, delete it and make one in the Editor instead:** right-click
   `Enemy.prefab` → `Create` → `Prefab Variant`, rename to `SpikyEnemy`, then set the tint to purple,
   untick `Can Be Stomped`, and set Move Speed 3 / Chase Range 6. Tell me either way.
3. Check the `Enemy` in `Level1` is now an instance of **`Enemy.prefab`** (blue prefab icon), sits at
   x = 20, and is red — not a Player prefab any more.
4. Select the `CinemachineCamera` and confirm `CinemachineConfiner2D` is there with
   `Bounding Shape 2D` set to the new **`CameraBounds`** object in the scene.
5. **Playtest `Level1`:**
   - walk into the enemy → lose a heart, get knocked back, brief invulnerability
   - lose all 3 hearts → the level reloads
   - collect a cherry → a heart comes back
   - drag a **`Saw`** prefab into the scene and walk into it → it hurts and cannot be killed.
     Set its `Patrol Mover → Offset` to something like `(0, 3)` to make it move
   - drag a **`SpikyEnemy`** in → jumping on it should hurt you rather than kill it; a shuriken kills it
   - walk to the far left and right edges → the camera should stop instead of showing empty space
6. **Report feel:** heart count, knockback strength, saw size, whether the camera bounds feel right.

### After M2.5
1. **Open Unity and check the Console.** Two prefabs were renamed on disk and several scripts
   changed shape, so this is the reimport that proves it.
2. **⚠️ Highest-risk item — select `Assets/Prefabs/Enemy.prefab` and check the `Enemy` component's
   `Ground Layer` field shows `Ground` ticked.** Same hand-written `m_Bits` format as the Player's
   was in M1. **If it reads `Nothing`, set it to `Ground` manually** — otherwise the ledge check
   thinks there is never any floor ahead and the enemy will refuse to move at all.
3. Confirm the Project window shows **`SpikedEnemy`** and **`CubeObstacle`** (no spaces), that
   `SpikedEnemy` still opens as a variant of `Enemy`, and that the `Level1` Hierarchy still lists
   both with no "missing prefab" icon.
4. **Playtest `Level1` and check the four behaviours that changed:**
   - **Enemy chase** — should be smooth, no jitter or shuddering when it reaches you or presses
     against a wall. This is the main thing M2.5 fixed.
   - **Enemy ledge check** — walk the enemy toward the end of a platform. It should stop at the
     edge, still facing you, rather than walking off. Select it in the Scene view while playing
     and you will see the yellow gizmo ray it is using.
   - **Moving platform** — still moves, and still carries you when you stand on it.
   - **Saw** — still patrols when its `Patrol Mover → Offset` is non-zero.
5. **Report feel:** enemy speed now that it is velocity-driven, and whether the ledge stop distance
   (`Ledge Check Ahead`, default 1) looks right — too small and it walks half off the edge, too
   large and it stops well short.

### After the M2.5 follow-up
1. **⚠️ Highest-risk item — select `Assets/Prefabs/Enemy.prefab` and check `Solid Layers` shows
   `Default` *and* `Ground` both ticked.** The field was renamed from `Ground Layer`, so the old
   value does not carry over and this was hand-written as `m_Bits: 65`. **If it reads `Nothing`,
   tick `Default` and `Ground` manually** — otherwise the enemy sees no floor anywhere and will not
   move at all.
2. **Platform, descending.** Stand on the moving platform and ride it *down*. It should carry you
   smoothly with no bouncing or juggling. Then ride it *up*, and walk left/right while riding —
   your walking speed should feel normal, not doubled or cancelled.
3. **Jump off the platform mid-travel** — you should get a normal jump, not a stunted or boosted one.
4. **Enemy vs the cube.** Stand on the far side of `CubeObstacle` so the enemy chases you into it.
   It should hop over instead of grinding against it. Select the enemy while playing to see the
   three gizmo rays: yellow = ledge, red = blocked, green = clearance.
5. **Enemy vs a real wall.** Stand behind a tall bit of tilemap. The enemy should stop in front of
   it rather than jumping repeatedly into it.
6. **Report:** whether the hop height looks right (`Jump Force` 10, `Max Jumpable Height` 1.5), and
   whether the enemy sliding through the air in its run pose looks acceptable or wants a jump
   animation.

#### M2.5 follow-up 4 — moving platforms reworked

Follow-ups 1–3 all failed, and they failed for the same reason: **they all carried the rider by
velocity.** Each fix removed one symptom and exposed the next, which is the signature of treating
symptoms rather than the cause. Velocity carrying cannot be made exact, for three independent
reasons:

| # | Problem | Why it cannot be patched away |
|---|---|---|
| 1 | A rider that is **pushed** has its velocity chosen by the contact solver | it still holds that velocity when the platform stops, so it pops off the top |
| 2 | A rider that is **given** the platform's velocity then has gravity applied | the engine integrates gravity *after* `FixedUpdate`, so whatever we set is immediately wrong |
| 3 | The platform's velocity can only be **measured a step late** | the order of `FixedUpdate` between two scripts is undefined, so the rider may read it before the platform has updated it |

**The rework: carry by position instead.** Position and velocity are independent channels, so this
sidesteps all three at once. Every physics step, `Platform` moves itself *and* everything standing
on it by the same delta, before the simulation runs:

```csharp
rb.MovePosition(currentPosition);   // us
foreach (rider) rider.position += delta;   // and everything riding us, by the same amount
```

Because the rider is moved by exactly the platform's own delta, it ends the step in the same place
*relative to the platform* that it started. Nothing overlaps, so the solver has nothing to correct
and never generates an impulse — which is what the bounce was.

**What this deleted.** The measure of a correct design here is how much it removed:

| Removed | Was there to work around |
|---|---|
| `platformStickSpeed` | riders floating instead of settling |
| gravity pre-compensation | gravity being applied after `FixedUpdate` |
| `isJumping` flag | the platform swallowing a jump on its first frame |
| `velocity.x += carrier.Velocity.x` | horizontal carry |
| `Platform.Velocity` (now `Delta`) | being read a step late |

`Player.Move` is one line again and contains nothing about platforms at all. The player does not
know it is being carried, which is exactly right — its gravity, jumping, dive and knockback are
untouched by the platform system, so none of them can be broken by it.

**Other structural changes:**
- `Platform` moves in **`FixedUpdate`, not a coroutine**, so its own movement and the rider carry
  are one atomic operation. It tracks `currentPosition` itself rather than reading the body back,
  so a step never depends on what the engine did with the previous one.
- **Riders register** (`AddRider` / `RemoveRider`) from the player's ground check, which already
  knows what is underfoot. Registration lag is harmless — unlike velocity lag — because the platform
  is the one applying the movement. `Player.OnDisable` deregisters so level reloads leave nothing
  dangling.
- The rider list is `List<Rigidbody2D>`, so enemies or crates can ride a platform later with no
  change to `Platform`.
- **`Assets/Prefabs/Platform.prefab` created**, since these are going to be placed a lot. The
  existing hand-built platforms in the three levels are left alone; M4 replaces them with instances.

#### M2.5 follow-up 3 — the platform bounce, attempt 3 (superseded by follow-up 4)

The previous two attempts treated symptoms. The playtest detail that identified the real cause was
**"when it stops going up, the character does a little bounce"** — that is stored energy, and it can
only be stored if something other than the player is deciding the player's vertical velocity.

**Root cause.** Both directions were the same bug:

| Direction | What was happening | Result |
|---|---|---|
| Up | the platform **pushed** the player; the contact solver decided the upward velocity | the player still had that velocity when the platform stopped, so it popped off the top |
| Down | the player fell at *gravity's* rate, not the platform's | a gap opened underneath and the player landed on it again |

**Fix: the player owns its vertical velocity while riding.** The platform is treated as a frame of
reference — walking speed is relative to it, and its motion is added rather than reacted to:

```csharp
velocity.x += carrierVelocity.x;
velocity.y  = carrierVelocity.y - gravityThisStep - platformStickSpeed;
```

Three parts, each doing one job:
- **`carrierVelocity.y`** — move with the platform instead of being moved by it. Nothing is ever
  stored in the solver, so there is nothing to release when the platform stops.
- **`- gravityThisStep`** — Unity applies gravity during the physics step *after* `FixedUpdate`, so
  it is taken off in advance. Without this the player ends every step slightly faster than the
  platform, sinks in, and is pushed back out.
- **`- platformStickSpeed`** (1 u/s) — a gentle constant press into the surface. Matching exactly
  would leave the player free-floating at whatever gap it happened to have; this settles it. It is
  the same resting contact as standing on ordinary ground.

**Supporting fixes:**
- **`isJumping` flag**, set on jump and on stomp bounce, cleared on landing. Without it the platform
  logic would overwrite a jump on the frame it starts.
- **The ground check moved from `Update` to `FixedUpdate`.** It is a physics query, and Unity runs a
  frame's `FixedUpdate`s *before* its `Update`, so the platform logic had been reading last frame's
  answer about what the player was standing on.
- **`IsFalling` now requires `!isGrounded`.** Riding a platform down is a genuinely negative
  velocity, so without this the fall animation played the whole way down.

**Platform reworked for heavy use.** Since the game will use static, moving, disappearing and
damaging platforms, `endPosition` (an absolute world position) was the wrong knob — duplicate a
platform and it flies back to the original's destination; leave it unset and it flies to world
origin. It is now a **relative `offset`**, matching the convention `PatrolMover` already used, and
**zero means static** so one component covers both cases. The variants compose rather than subclass:

| Variant | How |
|---|---|
| Static | `offset` zero, or just plain Ground-layer geometry |
| Moving | set `offset` |
| Damaging | add the existing `Hazard` component |
| Disappearing | add a component that switches collider + renderer off *(M4)* |

`Platform.Velocity` is *measured* from the body's actual movement rather than derived from
`moveSpeed`, so it stays correct while paused at each end — and would still be correct if something
other than `Platform` were doing the moving.

### After M2.5 follow-up 2
1. **Open Unity and check the Console.** A new folder `Assets/Materials/` and a new asset
   `NoFriction.physicsMaterial2D` were written by hand, so confirm the asset imports and shows
   `Friction 0` / `Bounciness 0` in the Inspector.
2. **⚠️ Highest-risk items — two hand-written references:**
   - `Assets/Prefabs/Player.prefab` → `CapsuleCollider2D` → **`Material` should say `NoFriction`**.
     If it says `None`, drag `Assets/Materials/NoFriction` onto it. Without it the wall-sticking
     comes straight back.
   - `Assets/Prefabs/Enemy.prefab` → **`Solid Layers` should now be `Ground` only** (it was
     briefly Ground + Default, which is what froze the enemies). If `Default` is still ticked,
     untick it.
3. Confirm `CubeObstacle` is on the **`Ground`** layer and `CameraBounds` is on **`Ignore Raycast`**
   in all three levels.
4. **Playtest the three fixes:**
   - **Enemies chase again**, and hop over the cube.
   - **Jump into a wall while holding the direction into it** — you should slide down normally,
     not stick.
   - **Ride the platform down** — no bouncing, no juggling. Ride it up, walk while riding, and
     jump off mid-travel.
5. **Also re-check, because zero friction touches everything the player stands on:** that you can
   still stand still on a slope-free floor without drifting, and that being knocked back by an enemy
   still stops rather than sliding forever. Both should be unaffected — movement is velocity-driven
   — but they are the two places friction could have mattered.

### After M2.5 follow-up 4
1. **Nothing to wire.** No new serialized fields, and `platformStickSpeed` was removed from the
   Player prefab. Just check the Console and that `Platform.prefab` opens.
2. **Ride the platform through a full cycle**, checking each phase separately:
   - going **up**, and the moment it **stops at the top** — the bounce
   - going **down** — you should descend *with* it, not fall behind and land on it
   - **paused** at either end
   - **walking left and right while riding**, in every phase
   - **jumping off** while it moves up, and again while it moves down
   - **landing on it** from a height while it is moving
3. **Re-check the things that were previously entangled with the platform code and now are not** —
   normal jump height, dive-stomp bounce off an enemy, and knockback. All three used to share the
   vertical-velocity path that the platform logic was overriding; none of them do any more.
4. **`Platform.prefab` is new.** Drag one into `Level1` and set its `Offset` to something like
   `(4, 0)` to confirm a horizontally-moving platform carries you sideways too — that path was
   never actually tested, since the existing platform only moves vertically.

### After M2.5 follow-up 3 *(superseded — follow-up 4 replaced this work)*
1. **The platform is the whole point of this pass.** Ride it through a full cycle and check each
   phase separately, since each was its own failure mode:
   - going **up**, then the moment it **stops at the top** — this is where the bounce was
   - going **down** — you should descend with it, not fall behind and re-land
   - **paused** at either end — you should just stand there
   - **walking left and right while riding** in every phase
   - **jumping off** while it is moving up, and again while it is moving down
   - **landing on it** from a height while it is moving
2. **Check the animation while riding down** — the player should look like they are standing, not
   falling.
3. ⚠️ **The platform's field changed from `End Position` to `Offset`, and it is now relative.**
   Select the `Platform` in `Level1` and confirm `Offset` reads `(0, 2.25)` and that it travels the
   same path as before. If it reads `(0, 0)` the field did not carry over — set it to `(0, 2.25)`.
4. **Tuning knob:** `Player → Platform Stick Speed` (default 1). If landing on a platform feels
   heavy, lower it; if the player ever looks like it is floating a hair above one, raise it.
5. **Confirm nothing else regressed** — normal jumping height, dive-stomp bounce off an enemy, and
   knockback all touch the same vertical velocity path that this pass rewrote.

### After M3
Everything below was written as raw YAML with Unity closed, so this is the first real test of it.
The scripts were compiled against Unity's own assemblies before hand-off and build clean, so any
problem here will be an *asset* problem, not a compile error.

1. **Open Unity and check the Console.** Five new scripts and five new prefabs were added.

2. **⚠️ Highest-risk item — open `Assets/Prefabs/PauseMenu.prefab`.** It is by far the biggest
   thing ever hand-written in this project (105 objects, including two uGUI `Slider`s with their
   full Background / Fill Area / Handle Slide Area sub-trees). It should open showing a dark
   full-screen panel with a **PAUSED** title, two clock lines, a `ButtonGroup` of Continue /
   Options / Quit to Menu, and an `OptionsPanel` holding the Music and Sound sliders and a Back
   button.
   **If it is broken or looks wrong, delete it and rebuild it in the Editor.** Every lookup is by
   name, so only the names matter and the layout is yours to arrange:

   ```
   PauseMenu            <- Canvas + CanvasScaler + GraphicRaycaster + PauseMenu.cs
     PausePanel         <- full-screen dark Image
       Title
       ClockText
       ButtonGroup      <- empty RectTransform
         ContinueButton / OptionsButton / QuitButton
       OptionsPanel     <- empty RectTransform + OptionsPanel.cs
         MusicLabel / MusicSlider / SfxLabel / SfxSlider / BackButton
   ```
   Tell me either way. The intro screen uses the same `OptionsPanel` shape under its Canvas.

3. **Check `Assets/Prefabs/Hud.prefab`** opens with a `Hearts` container, `CoinIcon` +
   `CoinCounter`, and `AmmoIcon` + `AmmoCounter`. Its `Heart Prefab` field should point at
   `Heart`. The heart icon reuses the **Cherry** sprite deliberately — cherries are what heal you,
   so the HUD and the pickup match without needing new art.

4. **Playtest `Level1` and check the new systems:**
   - **HUD** — three hearts, coin count, ammo count. Take a hit and a heart should disappear;
     collect a cherry and it should come back.
   - **Pickup banner** should now *fade out* rather than blink off, and the counter text that used
     to fight with it is gone.
   - **Esc** — the game freezes, the panel appears with **Continue / Options / Quit to Menu**.
     Check the two clocks: *Level time* should stop dead while *Session time* keeps counting.
   - **Options** — the buttons are replaced by the Music and Sound sliders. Move them, press Back,
     press Esc to unpause. Reopening the pause menu should show the *buttons* again, not the
     sliders you left it on. Esc while the sliders are up should back out one step, not unpause.
   - **Click Continue with the mouse** — you should **not** also throw a shuriken.
   - **Walk off the very edge of a platform and press jump slightly late** — coyote time should
     still give you the jump. You should **not** be able to jump twice.
   - **Fall into a pit** — you should die and the level reload, rather than falling forever.
   - **Touch the `CubeObstacle` and stay against it** — it should hurt you repeatedly (about once
     every 1.2 s, the invulnerability window), not just once.
   - **Walk into the `LevelExit`** (the flag, currently at `x = 24`) — Level2 should load.
     ⚠️ **It was placed blind and may be floating.** Drag it onto solid ground first.

5. **Test that the save actually persists — this is the M3 acceptance test:**
   - Reach the exit in Level1, then **quit the game completely** (stop Play mode *and* close and
     reopen Unity, or make a build).
   - On the intro screen a **Continue** button should now be there, and take you to Level2.
     **New Game** should wipe that and start at Level1 again.
   - Volume set in the pause menu should still be where you left it, and the intro screen's
     **Options** should show the same values.

6. **The intro screen was rebuilt from scratch** — the eight corner frogs are gone, replaced by
   one large frog beside the title, with a dark band behind the button column and a version label
   bottom-right. Check it opens without errors and that **all three buttons respond** (Continue is
   hidden until there is progress to continue from).

7. **Checkpoints.** Two `Checkpoint` prefab instances were dropped into each level at
   `x = -14` and `x = 10`, **placed blind** like the LevelExit — move them onto real ground first.
   Then check:
   - Walking into one turns it from grey to full colour with a single scale pulse, and refills
     your hearts.
   - Take damage, collect a coin or two, then die. You should reappear **at the checkpoint, on
     full hearts**, with the ammo and coins you had *when you touched it* — the coins collected
     afterwards are back in the level.
   - Touch a checkpoint, then **quit the game completely** and press **Continue**. You should
     resume at that checkpoint with that inventory.
   - **New Game** must wipe all of it — back to Level1, 3 hearts, 3 shuriken, 0 coins.

8. **Dash and slide (Shift).**
   - **In the air** — one dash per jump, restored when you land. Check the fading afterimages
     appear, and that you cannot dash twice without touching the ground.
   - **On the ground** — you slide, and the character visibly shortens. Build a one-tile-high gap
     out of tilemap tiles and check you can slide through it.
   - **In a tunnel longer than one slide** — you should keep shuffling along at half speed while
     still low, and pop back up the moment there is headroom. You should *not* get stuck, and you
     should *not* stand up inside the ceiling.
   - **Take a hit mid-dash** — the dash should stop and the knockback should work normally.
   - **Dash into an enemy** — you should pass straight through it and take no damage. Dash into a
     wall or the `CubeObstacle` and you should be stopped as normal.
   - **⚠️ Check the dash sprite actually appears.** `Player_Dash.anim` and the `Dash` state on
     `Player.controller` were written as raw YAML. If the player turns invisible or keeps the idle
     pose while dashing, open `Player.controller` and confirm the `Player_Dash` state's Motion is
     `Player_Dash`, and that the clip's sprite is `Dash`. Both are one drag to fix.

9. **Fall death** should now trigger almost as soon as you drop out of view, rather than after a
   long silent fall. It is measured from the level's `CameraBounds`, so it needs no tuning — but a
   level with no bounds falls back to `Fall Death Height`.

10. **Report feel:** coyote time length (`Coyote Time`, default 0.12 s), whether the HUD is now the
   right size, whether the checkpoint pulse (`Pulse Scale` 1.4, `Pulse Duration` 0.35) reads well,
   and whether the intro layout wants moving — the title, frog and menu column are RectTransforms
   and can be nudged freely in the Editor without touching code.

#### M4 follow-up 1 — three fixes from the first Level 1 playtest

**1. Everything was buried in the ground: the Tilemap had a transform offset.** The `Tilemap` child
of the `Grid` sat at `m_LocalPosition (0, 2, 0)`, so cell y=−1 actually rendered and collided at
world y=1..2 — the ground surface was at **world y=2, not y=0**. Every prefab was placed against the
cell grid and therefore landed exactly 2 units low, inside the terrain.

Fixed by **zeroing the Tilemap transform** rather than adding 2 to fifty-odd positions, so from now
on **cell y == world y** and there is no second coordinate system to remember. The offset was a
leftover from the original test level, whose `CameraBounds` had been written to match it.

> Lesson worth keeping: a Tilemap can be offset from its Grid, and nothing about the cell
> coordinates hints at it. Check `Grid`/`Tilemap` transforms before trusting cell = world.

**2. A slide that ran out under a ceiling left the player parked in the slide pose.** `isCrouched`
correctly stayed true — that part was by design, since the flat sprite is right for shuffling — but
with no input the player simply stopped, and a stationary character in a stretched-out *sliding*
sprite reads as a bug rather than as a crouch.

`Player.Move` now falls back to `dashDirection` when crouched with no input, so the shuffle
continues the way the slide was already going and a tunnel always carries the player out of itself.
Holding a direction still steers, and still reverses, so no control is taken away.

**3. The map was mostly flat ground and gaps.** The ground now rolls between y=−2 and y=3 —
hills, a raised enemy shelf, a dip under the dash line, a staircase up to a plateau — plus pillar
stubs, an overhang, floating rocks and two tunnel roofs. **Every gap width is unchanged**, because
those are the measured numbers the mechanics depend on; only the silhouette around them changed.
1142 tiles, up from 1056.

#### M4 follow-up 2 — slide speed, grid alignment, and props

**1. The slide keeps its own speed under a ceiling.** Follow-up 1 made the player keep moving, but
at `crouchSpeedScale` (half of walking), which looked wrong: the sprite is a stretched-out *slide*,
so a slow crawl reads as the animation being stuck. Crouched movement is now `dashSpeed` — the
slide simply keeps its speed until there is headroom. The dash proper has already ended, so gravity
is back on and the player stays on the floor; only the horizontal speed carries over. That deleted
`crouchSpeedScale` entirely, from the script and from `Player.prefab`.

**2. Everything was half a tile to the left.** A tile at cell `x` spans world `x..x+1`, so the
centre of the column is **x+0.5** — but the placements were authored at whole numbers, which is the
tile's *left edge*. The build script now adds the half-tile itself, so the design file stays in
readable whole cells and objects land centred. Pickup `y` is `surface + 0.5` for the same reason:
a coin's 0.5 radius then fills its cell exactly.

> Together with follow-up 1's tilemap offset, that is two separate coordinate mistakes from the
> same cause — assuming a cell index *is* a world position. It is neither centred nor unshifted.

**3. `CubeObstacle` is gone from Level 1, replaced by real props.** Its sprite still points into
`Library/PackageCache/` *(defect #10)*, which is why it drew as a black box. Rather than repoint art
nothing needed, the level now uses more of what already works:

| Prop | Was | Now | Doing what |
|---|---|---|---|
| `Platform` | 1 | **3** | the ferry, plus a **static stepping stone** (offset zero) and a **vertical elevator** up to a new floating rock |
| `Saw` | 3 | **5** | one sweeps the arena approach, one patrols above the plateau |
| `Enemy` | 3 | **5** | populated the tunnel approach and the final gauntlet |

The "enemies hop obstacles" lesson the cube used to carry now comes from the terrain's pillar stubs
at x=30, 57 and 186, which are exactly the 1–1.5 units `Enemy.maxJumpableHeight` allows.

> `CubeObstacle.prefab` still exists and is still referenced by `Level2`/`Level3`, so defect #10 is
> still owed — it just no longer affects Level 1.

### After M4 — Level 1 built
`Level1.unity` was rebuilt from `Design/Level1.txt`: the whole tilemap, every gameplay prefab, and
the `CameraBounds` polygon. Written as raw YAML with Unity closed, so this is the first real test.

1. **Open Unity and check the Console.** The scene now has 1056 tiles and 56 prefab instances.
2. **⚠️ Highest-risk item — check the camera still follows the player.** Deleting the old Player
   instance orphaned the *stripped* Transform that `CinemachineCamera → Tracking Target` points at;
   it was repointed by hand at the new instance. **If the camera does not follow, drag the `Player`
   from the Hierarchy onto `CinemachineCamera → Tracking Target`.**
3. **Check `CameraBounds`** is a rectangle from (0, −7) to (200, 10) and that `CinemachineConfiner2D
   → Bounding Shape 2D` still points at it. This also sets the fall-death line (−7 − margin).
4. **Playtest the teaching order** — each of these is a designed beat, in this order:
   | x | What should happen |
   |---|---|
   | 22 | fall in the shallow basin and walk straight back out — no death |
   | 34 | first real pit, 5 tiles |
   | 44 | dive-stomp the Enemy (hold Down in the air), it drops ammo |
   | 48 | the CubeObstacle hurts you, and the Enemy hops it |
   | 50–66 | jump to the ledge, **air-dash 10 tiles** to the coins. Falling costs nothing |
   | 79 | SpikedEnemy across a pit — shuriken only |
   | 86–91 | **slide tunnel**, 6 tiles: dash covers 3.2, so you must shuffle the rest |
   | 101 | Checkpoint 1 |
   | 105–114 | **10-tile chasm — jump+dash is mandatory** (jump alone reaches 8.56) |
   | 125–138 | ride the Platform across, timing the rising Saw |
   | 145–157 | terraces; the y=6 coin stash is an optional detour |
   | 169 | hardest pure jump, 6 tiles |
   | 180–183 | second slide tunnel, under saws |
   | 195 | LevelExit → Level2 |
5. **Report feel:** whether the 10-tile chasm is fair, whether the 6-tile tunnel shuffle drags, and
   whether the enemy arena (x 39–68) is too much flat walking.

> Not yet done for Level 1: decorative tiles (vines/chains on a second non-collidable Tilemap) and
> the `Transform.Translate` scrolling background. Level 2 and Level 3 are still Level1 copies.

---

## Deferred manual steps

Things intentionally left for the user to finish later in development. **These are checklist items —
the game is not complete until they are done.**

### 🔊 Audio clips — deferred by user request
The audio *system* is built, wired and running (`AudioManager`, volume sliders, saved volume prefs),
and **every call site is live** — there is no code left to edit. `AudioManager.PlaySfx` is static
and ignores a null clip, so the game plays silently until clips are assigned and starts making
noise the moment they are.

**To finish — Inspector work only:**
1. Create `Assets/Audio/` and add clips — roughly 8: jump, throw, stomp/kill, hurt, death, pickup,
   level exit, UI click, plus 1–2 looping music tracks. (Kenney.nl "Impact Sounds" /
   "Music Jingles", or freesound.org.)
2. Assign them to the `AudioClip` fields, which are grouped under an **Audio** header on each
   component:

   | Component | Fields |
   |---|---|
   | `Player` (on `Player.prefab`) | Jump Clip, Stomp Clip, Hurt Clip, Pickup Clip |
   | `PlayerCombat` (same prefab) | Throw Clip |
   | `PlayerHealth` (same prefab) | Death Clip |
   | `Enemy` (on `Enemy.prefab`) | Death Clip |
   | `LevelExit` (on `LevelExit.prefab`) | Exit Clip |
   | `AudioManager` (on `AudioManager.prefab`) | Menu Music, Level Music, Ui Click Clip |

3. Playtest and balance, using the pause-menu sliders.

> Until step 2 is done, the **Audio** checklist item is not satisfied — the system is proven but
> nothing is audible.
