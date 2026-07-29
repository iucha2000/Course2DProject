# CLAUDE.md — Course2DProject ("Frog Ninja")

Conventions for this repository. Read this before making changes.

## What this project is

A Unity 2D course project being turned into a small final-exam game: **Frog Ninja**, a 3-level pixel
platformer (~8–12 min playtime). The game must demonstrate **every** concept covered in the course —
see `PLAN.md` for the checklist that serves as acceptance criteria.

This is a **student project**. Code should read like something a strong student who just finished the
course would plausibly write. **No over-engineering**: no DI containers, no event-bus architecture,
no ScriptableObject-driven config systems, no assembly definitions, no object pooling. Prefer the
obvious solution.

## Environment

| | |
|---|---|
| Unity | **6000.3.14f1** (note: the folder path says `6000.3.5f1` — the project was upgraded) |
| Render pipeline | **URP 17.3.0**, 2D Renderer (`Assets/Settings/Renderer2D.asset`) |
| Camera | **Cinemachine 3.1.7** — CM3 API (`CinemachineCamera`, *not* `CinemachineVirtualCamera`) |
| Input | **Legacy Input Manager** (`Input.GetAxis`, `Input.GetKeyDown`, `Input.GetButtonDown`) |
| Text | TextMeshPro (ships inside `com.unity.ugui` 2.0.0 in Unity 6) |
| Art | Pixel art, **16 pixels per unit** |

### Input — important

`ProjectSettings/ProjectSettings.asset` has `activeInputHandler: 2` (**Both**). Gameplay uses the
**legacy Input Manager**. The new Input System is present only because the UI `EventSystem` in both
scenes uses `InputSystemUIInputModule`.

- ✅ Write gameplay input as `Input.GetAxis("Horizontal")`, `Input.GetKeyDown(KeyCode.Space)`,
  `Input.GetButtonDown("Fire1")`.
- ⚠️ **Do not delete `Assets/InputSystem_Actions.inputactions`.** It is registered as the project-wide
  actions asset in `EditorBuildSettings.m_configObjects`, and both EventSystems depend on it.
- `Assets/_Archive/Player Movement.inputactions` is an abandoned experiment — ignore it.

## Folder structure

```
Assets/
  _Archive/       Lecture scratch-work kept for reference; not used by the game
  Animations/     .controller + .anim, one subfolder per subject (Player/, Item/)
  Art/            Sprite sheets. Art/Player/ holds the character sheets
  Materials/      PhysicsMaterial2D assets. NoFriction (friction 0) is on the player's collider
                  so holding a direction into a wall does not stick him to it mid-air
  Prefabs/        All gameplay prefabs
  Scenes/         IntroScene, Level1, Level2, Level3
  Scripts/        Gameplay scripts; Scripts/Objects/ for small data-holder components
  Settings/       URP assets (do not edit by hand)
  Tiles/          145 generated Tile assets sliced from Art/Terrain.png + the Tile Palette
  Simple 2D Platformer BE2/   Asset Store pack. Only Sprites/Coins.png is used —
                              do not delete the folder, the Coin prefab references it by GUID
  TextMesh Pro/   TMP essentials. Resources/ and Fonts/ are required; do not remove
```

## Naming

- **Scripts / classes**: `PascalCase`, and the **file name must exactly match the class name**
  (Unity will not bind a MonoBehaviour otherwise — this was a real bug in this repo).
- **Prefabs**: `PascalCase` singular — `Player`, `Coin`, `Shuriken`, `SpikyEnemy`.
- **Scenes**: `IntroScene`, `Level1`, `Level2`, `Level3`.
- **Animator controllers**: `<Subject>.controller`; **clips**: `<Subject>_<State>.anim`
  (e.g. `Player_Run.anim`, `Shuriken_Spin.anim`).
- **Animator parameters**: `PascalCase` — `Run` (Float), `IsGrounded` / `IsFalling` (Bool),
  `Jump` / `IsHurt` / `Dash` (Trigger or Bool). Bools read as questions (`Is…`), Triggers as events.
- ⚠️ **Every animator transition in this project uses Transition Duration 0.** All the clips are
  sprite swaps, and crossfading a sprite swap blends nothing — there is no in-between sprite, Unity
  just picks a moment mid-blend to switch. Unity's 0.25 s default therefore buys no smoothing and
  costs a window in which the state machine **cannot be interrupted** (`Interruption Source: None`).
  That caused a real bug: the 0.16 s dash was shorter than a 0.25 s `Idle ↔ Run` blend, so pressing
  Shift while that blend ran silently skipped the dash animation entirely.

## Coding style

- `[SerializeField] private` for **designer knobs only** (speeds, damage, durations, prefab refs,
  audio clips). Group with `[Header("…")]` when a class has more than ~4.
- **Do not** expose component references for Inspector dragging. Resolve them in code:
  ```csharp
  [RequireComponent(typeof(Rigidbody2D))]
  public class Player : MonoBehaviour
  {
      private Rigidbody2D rb;
      private void Awake() => rb = GetComponent<Rigidbody2D>();
  }
  ```
  This keeps manual wiring near zero and prevents silent `NullReferenceException`s. For
  scene-singletons (HUD, audio) use `FindFirstObjectByType<T>()` in `Awake`.
- **`Awake`** = wire up own components. **`Start`** = read other objects / saved state.
  **`Update`** = input and animation. **`FixedUpdate`** = physics (`Rigidbody2D` velocity/forces).
- Never mix movement models on one axis. The player moves by **`rb.linearVelocity` in
  `FixedUpdate`** (Unity 6 renamed `velocity` → `linearVelocity`). Do not use `transform.Translate`
  on physics bodies — it tunnels through colliders and breaks moving platforms.
- **Moving platforms carry riders by _position_, never by parenting, pushing, or velocity.** The
  platform moves itself and everything registered as standing on it by the same delta, in the same
  `FixedUpdate`, before the simulation runs (`Platform.CarryRiders`). Position and velocity are
  independent channels, so the rider's gravity, jumping and knockback stay untouched and no energy
  is ever stored in the solver. All three alternatives were tried and all three failed:
  | Approach | Why it fails |
  |---|---|
  | `SetParent` | rider is driven twice per step — parent transform *and* its own gravity — so it sinks in and is pushed back out |
  | `rb.MovePosition` **on the platform** | it hands the kinematic body a velocity so the solver can sweep it, and the contact passes that velocity to the rider — who keeps it when the platform stops, and pops off the top. Assign `rb.position` instead: a platform that carries riders itself must have no velocity of its own |
  | let the platform push it | the contact solver decides the rider's velocity, which it still has when the platform stops → pops off the top |
  | hand the rider the platform's velocity | gravity is applied *after* `FixedUpdate`, and the platform's velocity can only be measured a step late (FixedUpdate order between scripts is undefined) |
- **To crouch, resize the collider — do not scale the transform.** Scaling a physics object scales
  its collider too, but around the transform origin, so the feet lift off the floor and need
  compensating. Setting `capsule.size` and anchoring the shape by its **bottom** (`offset.y =
  standingBottom + size.y / 2`) keeps the feet planted for any size, with no compensation and no
  interference with the sprite.
- **Size a collider from the artwork, in world units, not as a fraction of another collider.**
  `Player.slideColliderSize` is `(2, 0.875)` because `Slide.png` is 32 × 14 px at 16 PPU. A scale
  factor would have to be re-derived by hand every time the art changed; the measurement does not.
- ⚠️ **A `CapsuleCollider2D` cannot be shorter than it is wide along its own direction.** Unity
  silently clamps the shape to a circle of the other dimension. Shortening the player's 1.5-wide
  *vertical* capsule to 0.9 left it 1.5 tall with an already-lowered offset, burying it 0.3 units
  in the floor — the player floated during the slide and dropped when it ended. **Switch
  `capsule.direction` to `Horizontal` when making it flat**, which is the correct shape anyway.
- **A "can I stand up again?" test must not include the floor.** Test only the band between the
  crouched head and the standing head. An overlap test using the full standing capsule finds the
  ground the player is stood on and reports "no room" forever.
- **To pass through one layer temporarily, use `Rigidbody2D.excludeLayers`**, not
  `Physics2D.IgnoreLayerCollision`. It is per-body rather than global, so it cannot leak into other
  objects, and it disappears with the object if it is destroyed mid-effect. The player's dash uses
  it to pass through `Enemy` — and *only* `Enemy`, since passing through scenery would let the
  player leave the level.
  ⚠️ **State the gameplay rule in code as well, and give it the right window.** A contact already
  in progress when the exclusion is switched on can still be delivered for a step, so
  `Player.TryTakeContactDamage` also ignores enemies outright. Key that guard on the *pass-through*
  (`isPhasingEnemies`), not on the dash — a dash that ends while still inside an enemy would
  otherwise restore contacts and hand over a heart on the next step. End the pass-through when the
  player is actually clear (an overlap query still sees them, since `excludeLayers` filters
  contacts, not queries), with a short grace cap so stopping inside an enemy cannot make the player
  permanently safe.
- **Purely visual effects belong in `Update`, not `FixedUpdate`.** The physics clock is a fixed
  0.02 s, so anything spawned there is capped at 50/second regardless of framerate — which is what
  made the dash trail look like a row of clones. `Update` also reads the *interpolated* transform,
  which is where the sprite is actually drawn.
- Physics queries belong in **`FixedUpdate`**, not `Update`. Unity runs all of a frame's
  `FixedUpdate`s *before* that frame's `Update`, so a ground check done in `Update` is already one
  frame stale by the time the next physics step reads it.
- Physics queries **always take a `LayerMask`**. `ProjectSettings/Physics2DSettings.asset` has
  `m_QueriesStartInColliders: 1`, so an unmasked query can hit the caller's own collider.
- ⚠️ **Never put `Default` (layer 0) in a query mask.** `Physics2DSettings` also has
  `m_QueriesHitTriggers: 1`, and marker volumes live on `Default` — `CameraBounds` is a
  *level-sized* trigger. Combined with `m_QueriesStartInColliders`, any query with `Default` in its
  mask reports a hit everywhere on the map. This has caused two real bugs already: it ate every
  shuriken in M2, and it froze every enemy in M2.5. `m_ExcludeLayers` on the marker does **not**
  help — that setting governs collisions, not queries. Solid scenery belongs on `Ground`; marker
  volumes belong on `Ignore Raycast` (layer 2), which is why `CameraBounds` now sits there.
- **UI lives in prefabs that carry their own `Canvas`.** `Hud.prefab` and `PauseMenu.prefab` each
  have a Canvas + CanvasScaler at the root, so they drop into a scene as ordinary root objects.
  Two reasons: a canvas is rebuilt as a unit, so the pause menu changing cannot force the HUD to
  rebuild; and a root-level prefab instance is far less fragile to hand-write than re-parenting
  into an existing canvas (which needs "stripped" transform objects). Sorting order: the level's
  own canvas 0, HUD 1, pause menu 2.
- **UI scripts resolve their children by name** (`transform.Find`, or a `GetComponentsInChildren`
  scan matching `name`). The script and its prefab ship together, so a name is as reliable as a
  dragged reference and cannot be silently unset in one scene out of three.
- **Menus are laid out by layout groups, not by `anchoredPosition`.** A button column is
  `Image` + `VerticalLayoutGroup` + `ContentSizeFitter`, so hiding one entry with `SetActive(false)`
  re-flows the rest and shrinks the panel instead of leaving a hole. Keep `Child Control Width` on
  (width comes from the column) and `Child Control Height` off (each button's height stays one
  editable number, with no `LayoutElement` needed).
- **Single-line labels have text wrapping off.** A button label in a rect that is momentarily zero
  wide will otherwise wrap to one letter per line and render vertically.
- ⚠️ **Never activate a UI object and deactivate it again in the same frame.** The queued layout
  rebuild is discarded and TextMeshPro can keep stale zero-width geometry. A panel that must start
  hidden should **ship inactive** and initialise lazily on first use — an inactive GameObject never
  receives `Awake`, so wiring it there would never run. This caused a real bug: the pause menu's
  labels rendered vertically.
- **The HUD polls; nothing pushes to it.** Everything it shows is a public read-only property on
  the object that owns it, so no gameplay script needs to know a HUD exists.
- **The checkpoint is the save.** There is one record in `SaveSystem` (level, position, ammo,
  coins) and it answers all three questions the game asks — where a death respawns, what carries
  into the next level, and where `Continue` resumes. Read back in exactly one place,
  `Player.RestoreFromSave` in `Start`, so those three cannot drift apart. Hearts are deliberately
  *not* stored: touching a checkpoint heals to full, so the answer is always "max".
- No empty `Start()` / `Update()` stubs — delete them.
- Comment *why*, not *what*. This is an exam project: a short comment explaining a non-obvious
  Unity behaviour is worth more than a line-by-line narration.

## Tags, layers, sorting layers

**Tags:** `Player`, `Enemy`, `Item`, `Obstacle`, `Platform`
Use `CompareTag("X")`, never `gameObject.tag == "X"` (the latter allocates).

The two damage sources are deliberately different shapes, and the difference is trigger vs solid:

| | `Saw` | `CubeObstacle` |
|---|---|---|
| Collider | trigger, layer `Hazard` | solid, layer `Ground` |
| Hurts via | `Hazard.cs` — `OnTriggerEnter2D` + `OnTriggerStay2D` | the `Obstacle` tag, read by `Player.OnCollisionEnter2D` + `OnCollisionStay2D` |
| You can | walk through it | stand on it, and enemies hop it |
| Moves? | yes, via `PatrolMover` | no — it is the static one |

Both need the **Stay** callback as well as Enter: after being knocked back, a player still touching
the thing when invulnerability ends never raises a second Enter, so they would sit inside it taking
no damage.

**Layers** (`ProjectSettings/TagManager.asset`):

| # | Name | Used by |
|---|---|---|
| 6 | `Ground` | Tilemap, moving platforms — the ground-check mask |
| 7 | `Player` | Player |
| 8 | `Enemy` | Enemies |
| 9 | `Projectile` | Thrown shuriken |
| 10 | `Hazard` | Saws and other damage-on-touch objects |
| 11 | `Pickup` | Coins, cherries, dropped ammo |

**Layer collision matrix** — these pairs are disabled on purpose:
`Player ✗ Projectile` (can't shoot yourself) · `Projectile ✗ Projectile` · `Projectile ✗ Pickup` ·
`Enemy ✗ Pickup` · `Hazard ✗ Pickup` · `Pickup ✗ Pickup` (drops don't shove each other).

**Sorting layers**, back to front: `Background` → `Default` → `Foreground`.
Gameplay sprites stay on `Default` and separate by **sorting order** within it:

| Order | What |
|---|---|
| 0 | tilemap, platforms, `CubeObstacle`, `Checkpoint`, `LevelExit` — the world and its props |
| 5 | pickups (`Coin`, `Cherry`, `AmmoPickup`) |
| 10 | `Enemy`, `Saw` |
| 20 | `Player` — always in front of anything it walks past |
| 25 | `Shuriken` |

Leaving everything at 0 lets Unity's fallback ordering decide, which is unstable when sprites
overlap — that is what made the checkpoint flag cut through the player.

## Working with scene and prefab YAML

`.unity` and `.prefab` files are YAML and can be edited directly, which avoids a lot of manual
Inspector work. Rules:

1. **Unity must be closed** (or at least the scene not dirty) during YAML edits, or the Editor will
   overwrite them on save.
2. **Commit before editing.** A bad YAML edit is then one `git checkout` away.
3. Scripts are referenced by the **GUID in their `.cs.meta`**, and each component also carries
   `m_EditorClassIdentifier: Assembly-CSharp::<ClassName>` — update **both** when renaming a class.
4. Renaming an asset must move its `.meta` too (use `git mv` for both) or the GUID changes and every
   reference breaks.
5. **Prefab variants**: the file is a single `PrefabInstance` with `m_SourcePrefab` pointing at the
   base. The anchor must be a **generated id** (`--- !u!1001 &3853355706663758102`) — using the
   `&100100000` that normal prefabs use for their main asset does not work.
6. Renaming a `[SerializeField]` field leaves stale `propertyPath` overrides in every scene that
   overrode it. They are harmless but should be removed; each modification entry is exactly four
   lines (`- target:` / `propertyPath:` / `value:` / `objectReference:`).

## Workflow

Work proceeds **one milestone at a time** (see `PLAN.md`). After each milestone: stop, let the user
complete their manual Editor steps and playtest, then continue. Append any new manual steps to the
running list at the bottom of `PLAN.md`.

Every meaningful implementation decision gets a brief plain explanation — this is the user's exam and
they must be able to defend every line.
