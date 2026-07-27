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
  `Jump` / `IsHurt` (Trigger). Bools read as questions (`Is…`), Triggers as events.

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
- Physics queries **always take a `LayerMask`**. `ProjectSettings/Physics2DSettings.asset` has
  `m_QueriesStartInColliders: 1`, so an unmasked query can hit the caller's own collider.
- No empty `Start()` / `Update()` stubs — delete them.
- Comment *why*, not *what*. This is an exam project: a short comment explaining a non-obvious
  Unity behaviour is worth more than a line-by-line narration.

## Tags, layers, sorting layers

**Tags:** `Player`, `Enemy`, `Item`, `Obstacle`, `Platform`
Use `CompareTag("X")`, never `gameObject.tag == "X"` (the latter allocates).

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
Gameplay sprites stay on `Default`.

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

## Workflow

Work proceeds **one milestone at a time** (see `PLAN.md`). After each milestone: stop, let the user
complete their manual Editor steps and playtest, then continue. Append any new manual steps to the
running list at the bottom of `PLAN.md`.

Every meaningful implementation decision gets a brief plain explanation — this is the user's exam and
they must be able to defend every line.
