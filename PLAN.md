# PLAN.md — Frog Ninja

Final-exam game built on top of the Unity 2D course project. See `CLAUDE.md` for conventions.

**Concept:** a 3-level pixel platformer. Run, jump, ride moving platforms, dodge saws, **stomp**
enemies and **throw shuriken** at the ones you can't stomp, collect cherries and coins, reach the
exit. Progress and settings persist between sessions.

**Target playtime:** 8–12 minutes.

---

## ▶ START HERE — current state

*Last updated: end of the M2.5 cleanup session.*

| | |
|---|---|
| **Done** | M0 (foundation), M1 (player refactor + combat), M2 (enemies, hazards, health, camera bounds), M2.5 (convention cleanup) |
| **Next** | **M3 — audio, PlayerPrefs, HUD, pause menu, level flow** |
| **Playable now** | `Level1` only. Move, jump, dive-stomp, throw shuriken at the mouse, take damage, die and reload, collect coins / cherries / ammo. |
| **Not built yet** | HUD (hearts, coins, ammo), pause menu, saving, level exit, audio, the actual level designs |

**To resume, just say:** *"Read PLAN.md and continue with M3."*

### Things to hand over at the start of the next session
- **Close Unity before saying go.** Scene, prefab and controller YAML get edited directly, and the
  Editor will overwrite those edits when it saves. Commit first too.
- Nothing is blocked. Audio files are **not** needed — M3 writes the audio system with every play
  call commented out (see *Deferred manual steps*), so it can be built without any `.wav` files.
- `Level1` has hand-placed test objects (a Saw, a SpikedEnemy). `Level2` and `Level3` are still plain
  copies. **Level1 is no longer safe to copy over the others** — they get designed properly in M4.

### Five checklist items are currently owed
None of these is in shipping code right now. All are on the "must survive" list:

| Item | Lost in | Repaid by |
|---|---|---|
| `SetActive` | M1 refactor | **M3** — pause panel / HUD hearts |
| `SetParent` | M2.5 follow-up (platform stopped parenting the player) | **M3** — runtime heart icons |
| `Time.deltaTime` | M2.5 follow-up | **M3** — pause menu, vs `Time.unscaledDeltaTime` |
| `OnCollisionExit2D` | M2.5 follow-up 2 (platform lost its collision callbacks) | **M3** — coyote time |
| `Transform.Translate` | M1 refactor | **M4** — scrolling background element |

> **Coyote time** is the suggested home for `OnCollisionExit2D`: when the player stops touching the
> ground, start a short window in which they can still jump. It is a well-known platformer technique,
> about five lines, genuinely improves the feel, and `OnCollisionExit2D` is the natural trigger — so
> it repays the checklist item without being a box-ticking exercise.

`Physics2D.Raycast` was in the same position and has already been repaid — M2.5 uses it three times
in the enemy's ledge, blocked and clearance checks.

### Known placeholders to revisit in M4
- `CameraBounds` polygon is sized to the current test tilemap, not to a designed level.
- The three levels are identical copies with no real layout yet.
- `CubeObstacle.prefab` still points at a sprite inside `Library/PackageCache/` *(defect #10)*.

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
- [x] `OnCollisionEnter2D` / `OnCollisionExit2D` + **contact normals**
- [x] `OnTriggerEnter2D`
- [x] `Physics2D.Raycast` / `RaycastHit2D` — *M1 removed the player's raycast ground check;
      **restored in M2.5** as the enemy's ledge check (`Enemy.HasGroundAhead`)*
- [x] `AddForce` + `ForceMode2D.Impulse`, `linearVelocity`
- [x] MonoBehaviour lifecycle, Inspector-serialized fields
- [x] `GetComponent<T>`, `CompareTag`, `FindGameObjectWithTag`
- [ ] `Transform.Translate` / `SetParent`, `Time.deltaTime` — ⚠️ **all three are currently missing
      from shipping code.** `Translate` went in the M1 player refactor; `SetParent` and
      `Time.deltaTime` went in the M2.5 follow-up when the moving platform stopped parenting the
      player. **Owed by M3** — `SetParent` for HUD heart icons created at runtime, `Time.deltaTime`
      for the pause menu (contrasted against `Time.unscaledDeltaTime`, which is the natural way to
      show what `timeScale = 0` actually does). **Owed by M4** — `Translate` on a scrolling
      background element
- [x] Coroutines — stored handle + `StopCoroutine`, `WaitForSeconds`, `yield return null`
- [x] `Vector2.Distance`, `Mathf.Abs`, `MoveTowards`
- [ ] `Dictionary<string,int>`, `SetActive` — ⚠️ `Dictionary` is fine (`Player.cs`), but
      **`SetActive` no longer exists in shipping code**. **Owed by M3** — the pause menu panel
      and the HUD heart icons both need it
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
- [ ] **PlayerPrefs** (`SetInt`/`GetInt`, `SetFloat`/`GetFloat`, `DeleteKey`) — M3
- [ ] **Audio** (`AudioSource`, `AudioClip`, `PlayOneShot`, music vs SFX, volume) — M3 structure,
      **clips wired by the user later** (see Deferred manual steps)
- [ ] **`Time.timeScale`** (pause menu) — M3

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

### M3 — Audio, PlayerPrefs, HUD, pause, flow
> **Not blocked on audio files.** `AudioManager` and the volume plumbing get built in full, but every
> individual *play* call at its call site is written as a **commented line** with a `// TODO(audio):`
> marker. The user drops in clips and uncomments them later in development.
- [ ] `AudioManager.cs` — `DontDestroyOnLoad` singleton, music + SFX sources, `PlayOneShot`
- [ ] `SaveSystem.cs` — static wrapper over `PlayerPrefs` (unlock level, volumes, reset)
- [ ] HUD rework — **separate** TMP objects for the pickup banner and the counters *(defect #3)*,
      plus hearts and ammo. *(Confirmed still live in the M2.5 audit: `itemNameText` and
      `collectedCountText` point at the **same** TMP object in all three levels, so the banner and
      the counter overwrite each other.)* Also drop `GameCanvasHandler`'s public component refs in
      favour of resolving them in code, per `CLAUDE.md`
- [ ] **Three owed checklist items must land here** *(see the checklist above)*:
      **`SetActive`** (pause panel / HUD hearts), **`SetParent`** (heart icons instantiated at
      runtime under a hearts container), **`Time.deltaTime`** (pause menu, shown against
      `Time.unscaledDeltaTime` so `timeScale = 0` is demonstrated rather than just used)
- [ ] `PauseMenu.cs` — Esc, `Time.timeScale = 0`, Resume / volume sliders / Quit to Menu
- [ ] `LevelExit.cs` — exit trigger → save unlock → load next scene
- [ ] Intro screen — Continue vs New Game driven by saved unlock, volume sliders

**Acceptance:** volume and unlock survive a full quit and relaunch; pause freezes everything;
finishing a level unlocks the next.

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

---

## Deferred manual steps

Things intentionally left for the user to finish later in development. **These are checklist items —
the game is not complete until they are done.**

### 🔊 Audio clips — deferred by user request
The audio *system* is built and working (`AudioManager`, volume sliders, saved volume prefs), but
the individual sound effects are not wired. At every place a sound should play, the code contains a
commented call marked `// TODO(audio):`, for example:

```csharp
// TODO(audio): AudioManager.Instance.PlaySfx(jumpClip);
```

**To finish:**
1. Create `Assets/Audio/` and add clips — roughly 7: jump, throw, stomp/kill, hurt, pickup, UI click,
   and 1–2 looping music tracks. (Kenney.nl "Impact Sounds" / "Music Jingles", or freesound.org.)
2. Assign each clip to the matching `[SerializeField] private AudioClip` field in the Inspector.
3. **Uncomment every `// TODO(audio):` line.** Find them all with a project-wide search for
   `TODO(audio)`.
4. Playtest and balance volumes.

> Until step 3 is done, the **Audio** checklist item is not satisfied.
