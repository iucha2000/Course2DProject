using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A platform that can be stood on and ridden. Put this on a Kinematic Rigidbody2D that is on
/// the Ground layer, so the player's ground check can find it.
///
/// <para>It carries riders by <b>position</b>: every physics step it moves itself and everything
/// standing on it by exactly the same amount, before the simulation runs. Their velocity is
/// never touched.</para>
///
/// <para>That is the whole design, and it is what makes this correct where carrying by velocity
/// was not. Velocity carrying fails three separate ways, and fixing one exposes the next:</para>
/// <list type="bullet">
/// <item>a rider that is <i>pushed</i> has its velocity decided by the contact solver, so it
///       still carries that velocity when the platform stops and pops off the top;</item>
/// <item>a rider that is <i>given</i> the platform's velocity has gravity applied to it
///       afterwards, because the engine integrates gravity after FixedUpdate;</item>
/// <item>and the platform's velocity can only be measured a step late, since the order of
///       FixedUpdate between two scripts is undefined.</item>
/// </list>
///
/// <para>Moving a rider by position sidesteps all three at once: position and velocity are
/// independent, so the rider's gravity, jump and knockback keep working untouched, and there is
/// never any stored energy to release.</para>
///
/// <para>Variants are built by adding components rather than by subclassing:</para>
/// <list type="bullet">
/// <item><b>static</b> - leave <c>offset</c> at zero</item>
/// <item><b>moving</b> - set <c>offset</c></item>
/// <item><b>damaging</b> - add <see cref="Hazard"/></item>
/// <item><b>disappearing</b> - add a component that switches the collider and renderer off</item>
/// </list>
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Platform : MonoBehaviour
{
    [Tooltip("How far from its starting position the platform travels, in world units. Zero " +
             "means it stays put, so one component covers both static and moving platforms.")]
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("Seconds spent waiting at each end of the trip.")]
    [SerializeField] private float pauseAtEnds = 1f;

    /// <summary>How far this platform moved on the last physics step. Riders moved by the same.</summary>
    public Vector2 Delta { get; private set; }

    private Rigidbody2D rb;

    // Where the platform thinks it is. Tracked here rather than read back from the body so a
    // step's movement never depends on what the physics engine did with the previous one.
    private Vector2 currentPosition;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private bool headingToEnd = true;
    private float pauseRemaining;

    // Everything currently standing on this platform. A list rather than a single reference so
    // that more than one thing can ride it - the player today, an enemy or a crate later.
    private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void Start()
    {
        currentPosition = rb.position;
        startPosition = currentPosition;
        endPosition = startPosition + offset;
    }

    /// <summary>Called by anything that has just started standing on this platform.</summary>
    public void AddRider(Rigidbody2D rider)
    {
        if (rider != null && !riders.Contains(rider))
        {
            riders.Add(rider);
        }
    }

    /// <summary>Called when a rider steps off, jumps away, or is disabled.</summary>
    public void RemoveRider(Rigidbody2D rider) => riders.Remove(rider);

    private void FixedUpdate()
    {
        Delta = Travel();

        if (Delta == Vector2.zero)
        {
            return;
        }

        // Both moves happen here, before the physics step, so the platform and its riders are
        // never out of step with each other by even one frame.
        rb.MovePosition(currentPosition);
        CarryRiders(Delta);
    }

    /// <summary>Advances along the route and reports how far we moved this step.</summary>
    private Vector2 Travel()
    {
        if (offset == Vector2.zero)
        {
            return Vector2.zero;
        }

        if (pauseRemaining > 0f)
        {
            pauseRemaining -= Time.fixedDeltaTime;
            return Vector2.zero;
        }

        Vector2 target = headingToEnd ? endPosition : startPosition;
        Vector2 next = Vector2.MoveTowards(currentPosition, target, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = next - currentPosition;
        currentPosition = next;

        // Vector2 equality has a small built-in tolerance, which is what lets this register as
        // "arrived" instead of creeping towards the target forever.
        if (currentPosition == target)
        {
            headingToEnd = !headingToEnd;
            pauseRemaining = pauseAtEnds;
        }

        return delta;
    }

    private void CarryRiders(Vector2 delta)
    {
        // Backwards, so a rider that has been destroyed can be dropped as we go.
        for (int i = riders.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rider = riders[i];

            if (rider == null)
            {
                riders.RemoveAt(i);
                continue;
            }

            // Because the rider is moved by exactly our own delta, it ends the step in the same
            // place relative to us that it started. Nothing overlaps, so the solver has nothing
            // to correct and never generates an impulse - which is what used to cause the bounce.
            rider.position += delta;
        }
    }
}
