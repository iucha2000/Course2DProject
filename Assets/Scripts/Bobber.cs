using UnityEngine;

/// <summary>
/// Floats an object gently up and down on the spot.
/// Pickups use calm, slow motion while projectiles and hazards spin fast and travel.
/// That difference in "motion language" is what tells the player at a glance that
/// something is safe to walk into, without needing a tutorial message.
/// </summary>
public class Bobber : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.12f;
    [SerializeField] private float frequency = 1.2f;

    private Vector3 startPosition;

    private void Start()
    {
        // Recorded in Start rather than Awake so it uses the position the enemy
        // dropped this at, not the position stored inside the prefab.
        startPosition = transform.position;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * 2f * Mathf.PI) * amplitude;
        transform.position = startPosition + Vector3.up * offset;
    }
}
