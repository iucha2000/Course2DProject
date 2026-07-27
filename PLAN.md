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

### M1 — Player refactor & combat core
- [ ] `Player.cs`: horizontal movement → `rb.linearVelocity` in `FixedUpdate` *(defect #1)*
- [ ] `Player.cs`: ground check → `Physics2D.OverlapCircle` with a `Ground` LayerMask *(defect #2)*
- [ ] `Player.cs`: `rigidbody2D` → `rb`, components resolved in `Awake` *(defects #4, #9)*
- [ ] **Stomp** — `OnCollisionEnter2D`, `normal.y > 0.5f` → kill enemy + bounce
- [ ] `PlayerCombat.cs` — `Fire1` throws, ammo count, cooldown
- [ ] `Projectile.cs` — kinematic Rigidbody2D, `gravityScale 0`, trigger, `Destroy` on hit + lifetime
- [ ] `Shuriken.prefab` + `Shuriken_Spin.controller` / `.anim` from the 8 `Saw.png` frames
- [ ] `Item.cs` gains an `ItemType` enum (Coin / Cherry / Shuriken)
- [ ] `Player.controller`: fix the overlapping `Run` thresholds *(defect #6)* and remove the
      unreachable `Player_Fall → Run` transition *(defect #7)*

**Acceptance:** player moves without tunneling; ground check never self-hits; `Fire1` spawns a
spinning shuriken that despawns on hit or after its lifetime; stomping an enemy kills it and bounces
the player; walking into an enemy still hurts.

### M2 — Enemies, hazards, camera bounds
- [ ] `Enemy.cs`: `Die()` → `Instantiate` ammo drop + `Destroy`; `canBeStomped` flag
- [ ] Enemy gets its own `Enemy.controller` (it currently borrows `Player.controller`)
- [ ] Enemy promoted to a real prefab; **`SpikyEnemy` as a prefab variant** (`canBeStomped: false`)
- [ ] `Hazard.cs` + `Saw.prefab` (spinning, damages on contact, not killable) + a moving variant
- [ ] `PlayerHealth.cs` — 3 hearts, cherry heals, death reloads the level
- [ ] `CinemachineConfiner2D` per level so the camera stops at the level edges

**Acceptance:** both enemy types behave correctly, saws hurt, hearts deplete and refill, the camera
never shows past the tilemap.

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
