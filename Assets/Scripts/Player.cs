using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [Tooltip("How long after walking off a ledge the player can still jump. Without this, a " +
             "jump pressed on the very last pixel of a platform is silently dropped and feels " +
             "like the game missed the input.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Taking damage")]
    [Tooltip("How long the player loses control of the character after being hit. Keep this short.")]
    [SerializeField] private float hitStunDuration = 0.25f;
    [Tooltip("How long the player cannot be hurt again after being hit. Keep this much longer.")]
    [SerializeField] private float invulnerabilityDuration = 1.2f;
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("How fast the sprite flashes while invulnerable.")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Dash")]
    [Tooltip("Shift. In the air it is a straight dash; on the ground it is a slide that also " +
             "shortens the player, so a low gap can be got through.")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.16f;
    [Tooltip("Ground dashes run on this cooldown. The air dash is limited differently - one per " +
             "jump - so a player cannot chain dashes to cross a gap that was never meant to be.")]
    [SerializeField] private float groundDashCooldown = 0.6f;
    [SerializeField] private AfterImage afterImagePrefab;
    [Tooltip("Seconds between the ghost images left along the dash.")]
    [SerializeField] private float afterImageInterval = 0.035f;

    [Tooltip("Layers the player passes straight through while dashing. Enemies only - dashing " +
             "through scenery would let the player leave the level.")]
    [SerializeField] private LayerMask dashPassThroughLayers;
    [Tooltip("Longest the player stays intangible to enemies after a dash ends while still " +
             "inside one. Bounded, so parking inside an enemy cannot make you permanently safe.")]
    [SerializeField] private float enemyPhaseGrace = 0.4f;

    [Header("Sliding")]
    [Tooltip("Collider size while sliding, in world units. Taken straight from the Slide " +
             "artwork: 32 x 14 pixels at 16 PPU is 2 x 0.875, so the body matches what is drawn.")]
    [SerializeField] private Vector2 slideColliderSize = new Vector2(2f, 0.875f);
    [Tooltip("Walking speed while still stuck crouched under a low ceiling.")]
    [SerializeField] private float crouchSpeedScale = 0.5f;

    [Header("Dive and stomp")]
    [Tooltip("Downward speed when diving. Diving is the only way to kill an enemy by landing on it.")]
    [SerializeField] private float diveSpeed = 20f;
    [SerializeField] private float stompBounceForce = 11f;
    [Tooltip("How straight-down the contact has to be to count as a stomp. 1 = perfectly on top.")]
    [SerializeField] private float stompNormalThreshold = 0.5f;

    // Left empty on purpose. The clips are dropped in late in development - see the deferred
    // manual steps in PLAN.md - and AudioManager.PlaySfx treats a null clip as "play nothing",
    // so the game runs perfectly well until then.
    [Header("Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip stompClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dashClip;

    // Components on this same GameObject. Found in Awake so nothing has to be dragged
    // in the Inspector, which also means they can never be left null by accident.
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D capsule;
    private PlayerCombat combat;
    private PlayerHealth health;

    [Header("Collectibles")]
    [Tooltip("Which pickup counts as score. Must match the Item Name on the Coin prefab.")]
    [SerializeField] private string coinItemName = "Coin";

    // The record of everything picked up, keyed by the item's name. Counting by
    // name rather than by ItemType keeps this honest if two pickups ever share a type - a silver
    // and a gold coin would both be score, but they are still different things.
    private readonly Dictionary<string, int> collectedItemCounts = new();

    /// <summary>How many of a given pickup have been collected.</summary>
    public int CollectedCount(string itemName) =>
        collectedItemCounts.TryGetValue(itemName, out int count) ? count : 0;

    /// <summary>The score. Read by <see cref="Hud"/> and carried in the save.</summary>
    public int CoinCount => CollectedCount(coinItemName);

    // Input is read in Update but applied in FixedUpdate, so it has to be
    // remembered in between.
    private float horizontalInput;
    private bool jumpRequested;
    private bool diveRequested;
    private bool diveKeyWasHeld;

    private bool isGrounded;
    private bool isDiving;

    // Time.time at which the coyote window closes. Set when we lose contact with the ground.
    private float coyoteTimeEnd;

    /// <summary>Grounded, or recently enough grounded that the jump is still allowed.</summary>
    private bool CanJump => isGrounded || Time.time < coyoteTimeEnd;

    // The moving platform we are currently standing on, or null. Filled in by CheckGround,
    // since the ground check already has to find out what is under our feet.
    private Platform carrier;

    // Two separate ideas that are easy to confuse:
    // isHurt        - the player has lost control for a moment (hit stun)
    // isInvulnerable - the player cannot be hurt again yet (invulnerability frames)
    // Invulnerability lasts much longer than the stun, so control comes back quickly
    // but you do not get shredded by standing next to a saw.
    private bool isHurt;
    private bool isInvulnerable;

    // The handle to the running hurt/blink coroutine, kept so a checkpoint can cut it short.
    private Coroutine hurtRoutine;

    // Dash state.
    private bool dashRequested;
    private bool isDashing;
    private float dashEndTime;
    private float dashDirection;
    private float afterImageTimer;
    private bool airDashAvailable = true;
    private float nextGroundDashTime;

    private bool isCrouched;

    // The capsule as authored, so crouching can shorten it and put it back exactly.
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private CapsuleDirection2D standingColliderDirection;

    private float baseGravityScale;
    private LayerMask baseExcludeLayers;

    // True while enemies are being passed through. Outlives the dash itself - see UpdateEnemyPhase.
    private bool isPhasingEnemies;
    private float enemyPhaseEndTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();

        baseGravityScale = rb.gravityScale;
        baseExcludeLayers = rb.excludeLayers;

        standingColliderSize = capsule.size;
        standingColliderOffset = capsule.offset;
        standingColliderDirection = capsule.direction;
    }

    private void Start()
    {
        RestoreFromSave();
    }

    /// <summary>
    /// Puts the player back at the last checkpoint with what they were carrying. Runs on every
    /// level load, so it covers all three cases at once: dying, finishing the previous level,
    /// and pressing Continue after a relaunch.
    /// </summary>
    private void RestoreFromSave()
    {
        // Only move if the saved checkpoint belongs to *this* level. A checkpoint in Level1 says
        // nothing about where to stand in Level2, which is what HasCheckpointPosition is for.
        if (SaveSystem.HasCheckpointPosition &&
            SaveSystem.CheckpointLevel == gameObject.scene.buildIndex)
        {
            // rb.position rather than transform.position: this body is Dynamic, and moving the
            // transform teleports it past the solver.
            rb.position = SaveSystem.CheckpointPosition;
        }

        collectedItemCounts[coinItemName] = SaveSystem.CarriedCoins;

        // -1 means "no save yet", in which case PlayerCombat keeps its own starting ammo.
        if (SaveSystem.CarriedAmmo >= 0)
        {
            combat.SetAmmo(SaveSystem.CarriedAmmo);
        }
    }

    /// <summary>Called by a <see cref="Checkpoint"/> the player has just touched.</summary>
    public void ClaimCheckpoint(Vector2 position)
    {
        // Healing here rather than in the save is what keeps "full hearts" true everywhere -
        // after a death and after a relaunch - without the save having to remember a heart count.
        health.Refill();
        EndHurtEarly();

        SaveSystem.SaveCheckpoint(gameObject.scene.buildIndex, position, combat.Ammo, CoinCount);
    }

    /// <summary>
    /// Cuts the hurt state short. A checkpoint puts the player back to full, so leaving them
    /// stunned and flashing afterwards would be wrong - and a blink that is interrupted halfway
    /// can leave the sprite switched off, which looks like the player has vanished.
    ///
    /// <para>This is why the coroutine handle is stored: <c>StopCoroutine</c> needs the exact
    /// running instance, and stopping it by name would also kill any other routine of that name.
    /// Whatever the coroutine would have tidied up on its way out has to be undone here instead,
    /// because stopping it means its remaining lines never run.</para>
    /// </summary>
    private void EndHurtEarly()
    {
        if (hurtRoutine == null)
        {
            return;
        }

        StopCoroutine(hurtRoutine);
        hurtRoutine = null;

        isHurt = false;
        isInvulnerable = false;
        spriteRenderer.enabled = true;
    }

    /// <summary>Records what the player is carrying into the next level.</summary>
    public void SaveCarriedState(int nextLevelBuildIndex) =>
        SaveSystem.SaveLevelStart(nextLevelBuildIndex, combat.Ammo, CoinCount);

    private void Update()
    {
        ReadInput();

        animator.SetBool("IsGrounded", isGrounded);

        // Falling means airborne and heading down. The grounded test matters now that riding
        // a descending platform gives a real negative velocity while standing perfectly still
        // on it - without it, the fall animation would play the whole way down.
        animator.SetBool("IsFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
        animator.SetFloat("Run", Mathf.Abs(horizontalInput));

        // Two different poses for two different moves. Sliding covers the whole low state, not
        // just the dash that started it - the same flat pose is right for shuffling along under
        // a ceiling afterwards. They are mutually exclusive by construction: a ground dash
        // crouches, an air dash does not.
        animator.SetBool("Slide", isCrouched);
        animator.SetBool("Dash", isDashing && !isCrouched);

        if (isDashing)
        {
            TickAfterImages();
        }
    }

    private void FixedUpdate()
    {
        // The ground check is a physics query, so it runs on the physics clock. Doing it in
        // Update left it a frame stale, because Unity runs every FixedUpdate for a frame
        // before that frame's Update - so the platform logic below was reading last frame's
        // answer about what we were standing on.
        isGrounded = CheckGround();

        if (isGrounded)
        {
            // Landing always ends a dive, whether it hit anything or not, and gives the air
            // dash back - which is what stops it being used to fly.
            isDiving = false;
            airDashAvailable = true;
        }

        if (dashRequested)
        {
            StartDash();
        }

        if (isDashing)
        {
            // A dash drives the body entirely: no input, no gravity, no jumping out of it.
            TickDash();
            return;
        }

        UpdateEnemyPhase();

        // Stand back up as soon as there is room. While there is not, the player stays low and
        // keeps walking at crouch speed - that is what lets a tunnel longer than one slide be
        // crossed rather than leaving them stuck inside it.
        TryStand();

        // While hurt the player keeps the knockback velocity instead of being driven
        // by input, otherwise Move() would overwrite the knockback on the very next step.
        if (!isHurt)
        {
            Move();
        }

        if (jumpRequested)
        {
            Jump();
        }

        if (diveRequested)
        {
            Dive();
        }
    }

    private void ReadInput()
    {
        // Time.timeScale = 0 freezes physics but not Update, so without this the player would
        // still steer and turn behind the pause panel - and the click that presses Resume would
        // land in the game as well.
        if (PauseMenu.IsPaused)
        {
            horizontalInput = 0f;
            return;
        }

        // Pressing S or Down while in the air slams the player straight down.
        // "Vertical" is a default Input Manager axis, so S and Down arrow both work.
        // GetAxisRaw skips the smoothing, so the dive starts the instant the key goes down.
        //
        // Only a fresh press counts, never a held key. Without that, bouncing off a stomped
        // enemy while still holding Down would immediately start another dive and wipe out
        // the bounce on the very next physics step.
        //
        // This is tracked before the hit-stun check below, otherwise letting go of the key
        // while stunned would go unnoticed and the next press would be ignored.
        bool diveKeyHeld = Input.GetAxisRaw("Vertical") < -0.5f;
        bool diveKeyPressed = diveKeyHeld && !diveKeyWasHeld;
        diveKeyWasHeld = diveKeyHeld;

        if (isHurt)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxis("Horizontal");

        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = true;
        }

        // GetKeyDown is only true for one frame, so it has to be polled in Update
        // and remembered until the next physics step.
        // Crouched means there is a ceiling overhead, so there is nowhere to jump to.
        if (Input.GetKeyDown(KeyCode.Space) && CanJump && !isCrouched)
        {
            jumpRequested = true;
        }

        if (!isGrounded && !isDiving && diveKeyPressed)
        {
            diveRequested = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            // On the ground it is a cooldown; in the air it is one per jump. Two different rules
            // because they solve different problems - the ground one stops it being spammed, the
            // air one stops a chain of dashes crossing a gap the level never intended.
            bool available = isGrounded ? Time.time >= nextGroundDashTime : airDashAvailable;

            if (available && !isDashing)
            {
                dashRequested = true;
            }
        }
    }

    private void Dive()
    {
        diveRequested = false;
        isDiving = true;

        // Replace the current fall speed rather than adding to it, so a dive always
        // feels the same whether it starts at the top of a jump or halfway down.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -diveSpeed);
    }

    /// <summary>
    /// Begins a dash in the direction the player is facing. On the ground it doubles as a slide:
    /// the whole object is scaled down, which shortens the capsule with it, so the dash can be
    /// used to get under something too low to walk through.
    /// </summary>
    private void StartDash()
    {
        dashRequested = false;
        isDashing = true;
        isDiving = false;
        afterImageTimer = 0f;

        dashDirection = spriteRenderer.flipX ? -1f : 1f;
        dashEndTime = Time.time + dashDuration;

        // Gravity off for the duration. That is what makes the dash cover the same distance
        // every time instead of a shorter, drooping one when entered at speed while falling.
        rb.gravityScale = 0f;

        // Pass through enemies, but only enemies. excludeLayers is per-body, so this cannot leak
        // into anything else the way Physics2D.IgnoreLayerCollision would - and it switches off
        // contacts entirely, so no damage is taken on the way through either.
        isPhasingEnemies = true;
        rb.excludeLayers = baseExcludeLayers | dashPassThroughLayers;

        if (isGrounded)
        {
            nextGroundDashTime = Time.time + groundDashCooldown;
            SetCrouched(true);
        }
        else
        {
            airDashAvailable = false;
        }

        AudioManager.PlaySfx(dashClip);
    }

    private void TickDash()
    {
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        if (Time.time >= dashEndTime)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        if (!isDashing)
        {
            return;
        }

        isDashing = false;
        rb.gravityScale = baseGravityScale;

        // The pass-through is deliberately NOT ended here. Restoring collisions while the player
        // is still standing inside the enemy they just dashed into hands them a contact - and a
        // heart - on the very next step, which is the whole thing this is meant to prevent.
        // UpdateEnemyPhase ends it once they are actually clear, or when the grace runs out.
        enemyPhaseEndTime = Time.time + enemyPhaseGrace;

        // The crouch is deliberately not undone here. TryStand does it once there is headroom,
        // which is what keeps the player low for as long as the ceiling lasts.
    }

    /// <summary>
    /// Ghosts are spawned from Update, not FixedUpdate, for two reasons. The physics step is a
    /// fixed 0.02s, so spawning there caps the trail at one ghost per step - far too sparse to
    /// read as a trail at dash speed. And the body is interpolated, so transform.position in
    /// Update is where the sprite is actually drawn, which is where a copy of it belongs.
    /// </summary>
    private void TickAfterImages()
    {
        afterImageTimer -= Time.deltaTime;

        if (afterImageTimer > 0f)
        {
            return;
        }

        afterImageTimer = afterImageInterval;
        SpawnAfterImage();
    }

    /// <summary>
    /// Ends the pass-through once the player is genuinely clear of every enemy, or once the grace
    /// period expires - whichever comes first. The grace is what stops a player who stops inside
    /// an enemy from being permanently untouchable.
    /// </summary>
    private void UpdateEnemyPhase()
    {
        if (!isPhasingEnemies)
        {
            return;
        }

        if (Time.time < enemyPhaseEndTime && IsOverlappingEnemy())
        {
            return;
        }

        isPhasingEnemies = false;
        rb.excludeLayers = baseExcludeLayers;
    }

    /// <summary>
    /// Is any enemy still inside us? excludeLayers only filters *contacts*, not queries, so this
    /// still sees them while we are passing through.
    /// </summary>
    private bool IsOverlappingEnemy() =>
        Physics2D.OverlapCapsule(rb.position + capsule.offset, capsule.size, capsule.direction,
                                 0f, dashPassThroughLayers) != null;

    private void SpawnAfterImage()
    {
        if (afterImagePrefab == null)
        {
            return;
        }

        AfterImage image = Instantiate(afterImagePrefab, transform.position, Quaternion.identity);
        image.Show(spriteRenderer.sprite, spriteRenderer.flipX, transform.localScale);
    }

    private void SetCrouched(bool crouched)
    {
        if (isCrouched == crouched)
        {
            return;
        }

        isCrouched = crouched;

        // Shorten the capsule from the top down. The offset is moved by half of whatever height
        // was removed, which keeps the bottom - the feet - exactly where it was, so crouching
        // never lifts the player off the floor and standing never drives them into it.
        Vector2 size = crouched ? slideColliderSize : standingColliderSize;

        // The capsule is also laid on its side while crouched, and that is not cosmetic. A
        // *vertical* capsule cannot be shorter than it is wide - Unity clamps the shape to a
        // circle of the width - so asking a 1.5-wide capsule to be 0.875 tall would silently
        // leave it 1.5 tall with the offset already lowered, driving it into the floor. The
        // solver would push the player up out of it and standing again would drop them back:
        // the "player floats during the slide and then lands" bug. Horizontal is also simply the
        // right shape for a slide - wide and flat, exactly like the sprite.
        capsule.direction = crouched ? CapsuleDirection2D.Horizontal : standingColliderDirection;
        capsule.size = size;

        // Anchor the new shape by its bottom, so the feet stay on the floor whichever size is
        // in use. Everything else about the slide follows from the artwork; this is the one
        // thing that has to be true regardless of it.
        capsule.offset = new Vector2(standingColliderOffset.x, StandingBottomLocal + size.y * 0.5f);
    }

    /// <summary>Where the bottom of the standing capsule sits, in local units.</summary>
    private float StandingBottomLocal => standingColliderOffset.y - standingColliderSize.y * 0.5f;

    private void TryStand()
    {
        if (isCrouched && HasHeadroom())
        {
            SetCrouched(false);
        }
    }

    /// <summary>Is there room above the crouched player to stand back up?</summary>
    private bool HasHeadroom()
    {
        // Only the band between the crouched head and the standing head is tested. Checking the
        // whole standing capsule would find the floor the player is stood on and decide there is
        // never room. capsule.size is local and unscaled, which is what makes this arithmetic work.
        float worldBottom = rb.position.y + capsule.offset.y - capsule.size.y * 0.5f;
        float crouchedTop = worldBottom + slideColliderSize.y;
        float standingTop = worldBottom + standingColliderSize.y;

        Vector2 centre = new Vector2(rb.position.x + standingColliderOffset.x,
                                     (crouchedTop + standingTop) * 0.5f);
        Vector2 size = new Vector2(standingColliderSize.x * 0.9f, standingTop - crouchedTop);

        // Masked to Ground for the usual reason - see CLAUDE.md. Without it this would find the
        // player's own collider, because queries are set to start inside colliders project-wide.
        return Physics2D.OverlapBox(centre, size, 0f, groundLayer) == null;
    }

    private void Move()
    {
        // Setting the velocity directly, instead of moving the transform, keeps the player
        // inside the physics simulation so it cannot tunnel through the tilemap collider.
        //
        // Note there is nothing about moving platforms here. A platform we are standing on
        // moves us by position in its own FixedUpdate, which leaves our velocity - and with it
        // our gravity, jumping and knockback - completely untouched. See Platform for why.
        //
        // Still crouched means still under a ceiling, so the player shuffles rather than runs.
        float speed = isCrouched ? moveSpeed * crouchSpeedScale : moveSpeed;

        // A slide that runs out under a ceiling must not leave the player parked in the slide
        // pose with nothing happening - stationary in a stretched-out sliding sprite reads as a
        // bug, not as a crouch. With no input they keep drifting the way the slide was already
        // going, so a tunnel always carries them out of itself and can never trap them. Input
        // still steers and still reverses, so nothing is taken away from the player.
        float direction = horizontalInput;
        if (isCrouched && Mathf.Approximately(direction, 0f))
        {
            direction = dashDirection;
        }

        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        jumpRequested = false;

        // Spend the coyote window. Without this the same window could be used twice while
        // the player is still technically off the ground.
        coyoteTimeEnd = 0f;

        // Zero out any leftover vertical speed first so every jump reaches the same height.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger("Jump");
        AudioManager.PlaySfx(jumpClip);
    }

    private bool CheckGround()
    {
        // A small circle at the bottom of the capsule. The LayerMask is what makes this
        // reliable: Physics2D has "Queries Start In Colliders" enabled project-wide, so
        // without a mask this could hit the player's own collider and always say grounded.
        Vector2 feet = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y);
        Collider2D ground = Physics2D.OverlapCircle(feet, groundCheckRadius, groundLayer);

        // The ground check is already the one thing that knows what is under our feet, so it
        // is also what decides which platform we are riding.
        SetCarrier(ground != null ? ground.GetComponent<Platform>() : null);

        return ground != null;
    }

    /// <summary>
    /// Registers or deregisters us with the platform we are standing on. Riding is a
    /// registration rather than something we read each frame: the platform moves us itself,
    /// in the very step it moves in, so we can never act on a stale idea of where it is.
    /// </summary>
    private void SetCarrier(Platform platform)
    {
        if (platform == carrier)
        {
            return;
        }

        if (carrier != null)
        {
            carrier.RemoveRider(rb);
        }

        carrier = platform;

        if (carrier != null)
        {
            carrier.AddRider(rb);
        }
    }

    private void OnDisable()
    {
        // Do not leave a dangling entry in a platform's rider list when the level reloads.
        SetCarrier(null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Item"))
        {
            return;
        }

        Item item = other.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        switch (item.itemType)
        {
            case ItemType.Shuriken:
                combat.AddAmmo(1);
                break;
            case ItemType.Cherry:
                health.Heal(1);
                break;
            case ItemType.Coin:
                // Coins are score only.
                break;
        }

        collectedItemCounts.TryGetValue(item.itemName, out int currentCount);
        collectedItemCounts[item.itemName] = currentCount + 1;

        // Nothing tells the HUD about this. It polls CoinCount, Hearts and Ammo itself, so the
        // player does not need to know a HUD exists.
        AudioManager.PlaySfx(pickupClip);
        Destroy(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // A stomp is an impact, so it is only ever tested on the frame of the impact.
        if (TryStomp(other))
        {
            return;
        }

        TryTakeContactDamage(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        // Coyote time starts the instant contact is actually lost, which is something the
        // ground check cannot tell us: OverlapCircle only reports whether there is ground
        // under us right now, not that we have just left some.
        //
        // The velocity test is what stops this granting a second jump. Walking off a ledge
        // means we are falling or level, so the window opens; leaving the ground because we
        // just jumped means we are moving upwards, so it does not.
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            coyoteTimeEnd = Time.time + coyoteTime;
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        // Enter on its own is not enough for something solid that hurts, such as CubeObstacle.
        // If the player is still leaning on it - or standing on top of it - when the
        // invulnerability runs out, no second Enter is ever raised, so they would sit inside a
        // damaging block taking no further damage. This is the same reason Hazard needs
        // OnTriggerStay2D as well as OnTriggerEnter2D; the solid case simply needed it too.
        TryTakeContactDamage(other);
    }

    /// <summary>
    /// Kills an enemy by diving onto it. Needs all three of: coming down on top of it, actively
    /// diving, and an enemy that is currently stompable. Merely falling onto one hurts you, so
    /// killing something by landing on it has to be a deliberate act.
    /// </summary>
    private bool TryStomp(Collision2D other)
    {
        // A collision can be reported with no contact points, and GetContact(0) throws if it is.
        if (!isDiving || other.contactCount == 0 || !other.collider.CompareTag("Enemy"))
        {
            return false;
        }

        // The contact normal points from the surface we hit back towards us, so a normal
        // pointing upwards means we landed on top of whatever we just touched.
        if (other.GetContact(0).normal.y <= stompNormalThreshold)
        {
            return false;
        }

        Enemy enemy = other.collider.GetComponent<Enemy>();
        if (enemy == null || !enemy.canBeStomped)
        {
            return false;
        }

        enemy.Die();

        // Bounce off the enemy, the same way jumping works.
        isDiving = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
        AudioManager.PlaySfx(stompClip);
        return true;
    }

    private void TryTakeContactDamage(Collision2D other)
    {
        if (other.contactCount == 0)
        {
            return;
        }

        if (!other.collider.CompareTag("Enemy") && !other.collider.CompareTag("Obstacle"))
        {
            return;
        }

        // Passing through an enemy must not hurt, and that has to hold for as long as the pass
        // lasts - which is longer than the dash itself when the dash ends inside one. Keyed on
        // isPhasingEnemies rather than isDashing for exactly that reason. The layer exclusion
        // usually stops the contact arising at all, but a contact already in progress when the
        // exclusion was switched on can still be delivered for a step, so the rule is stated
        // here too rather than left implied by a physics setting.
        if (isPhasingEnemies && other.collider.CompareTag("Enemy"))
        {
            return;
        }

        // TakeHit ignores this while the player is still invulnerable, so calling it every
        // frame from OnCollisionStay2D is safe.
        TakeHit(other.GetContact(0).normal);
    }

    /// <summary>
    /// The single way the player gets hurt, whether from an enemy, an obstacle or a hazard.
    /// Ignored while already hurt, which is what gives the brief invulnerability after a hit.
    /// </summary>
    public void TakeHit(Vector2 knockbackDirection)
    {
        // Invulnerability, not hit stun, is what blocks a second hit. That is the whole
        // point of separating them: control returns long before you can be hurt again.
        if (isInvulnerable)
        {
            return;
        }

        hurtRoutine = StartCoroutine(HurtRoutine(knockbackDirection));
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        isInvulnerable = true;
        isDiving = false;

        // The dash grants no invulnerability, so being hit during one has to actually stop it -
        // otherwise TickDash would keep overwriting the knockback velocity every step.
        EndDash();

        animator.SetFloat("Run", 0f);
        animator.SetTrigger("IsHurt");
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        health.TakeDamage(1);
        AudioManager.PlaySfx(hurtClip);

        // Flash the sprite for the whole invulnerable window. Without this the player has
        // no way of knowing they are safe, which is what makes taking damage feel random.
        float elapsed = 0f;
        bool controlRestored = false;

        while (elapsed < invulnerabilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;

            float wait = Mathf.Min(blinkInterval, invulnerabilityDuration - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;

            if (!controlRestored && elapsed >= hitStunDuration)
            {
                isHurt = false;
                controlRestored = true;
            }
        }

        // Always leave the sprite visible, whichever half of the blink we ended on.
        spriteRenderer.enabled = true;
        isHurt = false;
        isInvulnerable = false;
        hurtRoutine = null;
    }
}
