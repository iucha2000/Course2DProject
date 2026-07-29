using System.Collections;
using UnityEngine;

/// <summary>
/// A flag the player can claim part-way through a level. Dying sends them back to the last one
/// they touched, with the ammo and coins they had at that moment, and on full hearts.
///
/// <para><b>Touching it heals you.</b> That is what makes "full hearts" the answer everywhere -
/// on respawn, and on Continue after a relaunch - instead of the save having to remember a heart
/// count that would sometimes strand the player on one heart at a hard checkpoint.</para>
///
/// <para>Its feedback is deliberately a different shape from the pickups'. They bob gently and
/// forever, which reads as "come and take me"; a checkpoint instead does a single sharp pulse and
/// then holds its lit colour, which reads as "done, claimed". Same idea as the ammo drop being
/// given different motion from the thrown shuriken: one glance should say which is which.</para>
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Colours")]
    [Tooltip("Shown before the player has reached this checkpoint.")]
    [SerializeField] private Color dormantColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Shown once it has been claimed.")]
    [SerializeField] private Color activeColor = Color.white;

    [Header("Claim pulse")]
    [Tooltip("How much bigger the flag gets at the peak of the pulse.")]
    [SerializeField] private float pulseScale = 1.4f;
    [SerializeField] private float pulseDuration = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip claimClip;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private bool claimed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        // Enforced rather than trusted to the prefab: a solid checkpoint would be a wall.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        // After a death the level reloads, so show the checkpoint the player is standing on as
        // already claimed - without the pulse, which would be celebrating something they did
        // before they died.
        bool isTheSavedOne =
            SaveSystem.HasCheckpointPosition &&
            SaveSystem.CheckpointLevel == gameObject.scene.buildIndex &&
            Vector2.Distance(SaveSystem.CheckpointPosition, transform.position) < 0.5f;

        claimed = isTheSavedOne;
        spriteRenderer.color = claimed ? activeColor : dormantColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (claimed || !other.CompareTag("Player"))
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        claimed = true;
        player.ClaimCheckpoint(transform.position);

        AudioManager.PlaySfx(claimClip);
        StartCoroutine(ClaimPulse());
    }

    /// <summary>
    /// One sharp scale punch, then settle. Runs on <c>yield return null</c> because it has to move
    /// a little every frame; Time.deltaTime is what keeps it the same length at any framerate.
    /// </summary>
    private IEnumerator ClaimPulse()
    {
        spriteRenderer.color = activeColor;

        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;

            // PingPong-style shape: 0 -> 1 -> 0 across the duration, eased so it does not
            // look mechanical. Mathf.Sin over half a turn gives that for free.
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            float punch = Mathf.Sin(t * Mathf.PI);

            transform.localScale = baseScale * (1f + (pulseScale - 1f) * punch);
            yield return null;
        }

        // Land exactly on the original scale rather than wherever the last frame left it.
        transform.localScale = baseScale;
    }
}
