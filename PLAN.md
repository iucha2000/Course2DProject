# PLAN.md — Frog Ninja

Final-exam game built on top of the Unity 2D course project. See `CLAUDE.md` for conventions.

**Concept:** a 3-level pixel platformer. Run, jump, ride moving platforms, dodge saws, **stomp**
enemies and **throw shuriken** at the ones you can't stomp, collect cherries and coins, reach the
exit. Progress and settings persist between sessions.

**Target playtime:** 8–12 minutes.

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
- [x] `Physics2D.Raycast` / `RaycastHit2D`
- [x] `AddForce` + `ForceMode2D.Impulse`, `linearVelocity`
- [x] MonoBehaviour lifecycle, Inspector-serialized fields
- [x] `GetComponent<T>`, `CompareTag`, `FindGameObjectWithTag`
- [x] `Transform.Translate` / `SetParent`, `Time.deltaTime`
- [x] Coroutines — stored handle + `StopCoroutine`, `WaitForSeconds`, `yield return null`
- [x] `Vector2.Distance`, `Mathf.Abs`, `MoveTowards`
- [x] `Dictionary<string,int>`, `SetActive`
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
- [ ] **`LayerMask` in physics queries** — M1
- [ ] **`Instantiate` / `Destroy`** — M1
- [ ] **Player offense** (stomp + shuriken) — M1
- [ ] **Prefab Variants** (SpikyEnemy) — M2
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

> Note: `SpikyEnemy.prefab` (hand-written) was replaced by an Editor-made **`Spiked Enemy.prefab`**.
> Prefab variant YAML needs a generated anchor id, not `&100100000` — see `CLAUDE.md`.

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

### M3 — Audio, PlayerPrefs, HUD, pause, flow
> **Not blocked on audio files.** `AudioManager` and the volume plumbing get built in full, but every
> individual *play* call at its call site is written as a **commented line** with a `// TODO(audio):`
> marker. The user drops in clips and uncomments them later in development.
- [ ] `AudioManager.cs` — `DontDestroyOnLoad` singleton, music + SFX sources, `PlayOneShot`
- [ ] `SaveSystem.cs` — static wrapper over `PlayerPrefs` (unlock level, volumes, reset)
- [ ] HUD rework — **separate** TMP objects for the pickup banner and the counters *(defect #3)*,
      plus hearts and ammo
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
- [ ] `Cube Obstacle.prefab` sprite repointed off `Library/PackageCache/` *(defect #10)*
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
2. **⚠️ Highest-risk item — open `Assets/Prefabs/SpikyEnemy.prefab`.** It is a **prefab variant**,
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
