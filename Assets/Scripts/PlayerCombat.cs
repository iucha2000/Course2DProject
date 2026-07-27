using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Shuriken")]
    [SerializeField] private Projectile shurikenPrefab;
    [SerializeField] private float throwCooldown = 0.35f;
    [Tooltip("How far in front of the player the shuriken appears, so it does not spawn inside him.")]
    [SerializeField] private float spawnOffset = 0.7f;

    [Header("Ammo")]
    [SerializeField] private int startingAmmo = 3;
    [SerializeField] private int maxAmmo = 9;

    // Read by the HUD. Private set so only this script can change it.
    public int Ammo { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private float nextThrowTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        Ammo = startingAmmo;
    }

    private void Update()
    {
        // "Fire1" is a default axis in the Input Manager, bound to left ctrl and mouse 0.
        if (Input.GetButtonDown("Fire1"))
        {
            Throw();
        }
    }

    public void AddAmmo(int amount)
    {
        Ammo = Mathf.Min(Ammo + amount, maxAmmo);
    }

    private void Throw()
    {
        if (Ammo <= 0 || Time.time < nextThrowTime)
        {
            return;
        }

        Vector2 direction = AimDirection();
        if (direction == Vector2.zero)
        {
            return;
        }

        Ammo--;
        nextThrowTime = Time.time + throwCooldown;

        // Spawn slightly along the aim direction so it never appears inside the player.
        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnOffset);

        // Instantiate returns the component type of the prefab field, so no GetComponent needed.
        Projectile shuriken = Instantiate(shurikenPrefab, spawnPosition, Quaternion.identity);
        shuriken.Launch(direction);

        // Turn to face the throw, otherwise the player can appear to throw backwards.
        // While walking, Player.ReadInput overwrites this again from the movement input.
        spriteRenderer.flipX = direction.x < 0f;
        // TODO(audio): AudioManager.Instance.PlaySfx(throwClip);
    }

    /// <summary>Direction from the player towards the mouse cursor, in world space.</summary>
    private Vector2 AimDirection()
    {
        if (mainCamera == null)
        {
            // Camera.main can be null for a frame after a scene load.
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return Vector2.zero;
            }
        }

        // The camera is orthographic, so only the x and y of the result matter here.
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorldPosition - transform.position;

        // Guard against the cursor sitting exactly on the player, which has no direction.
        return direction.sqrMagnitude < 0.0001f ? Vector2.zero : direction.normalized;
    }
}
