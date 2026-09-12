using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DashScript : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [Header("Dash Burst")]
    [Tooltip("Horizontal speed applied for the duration of the dash, in units/second.")]
    [SerializeField] private float dashSpeed = 14f;

    [Tooltip("How long the dash burst lasts, in seconds.")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("Seconds from the start of one dash to when the next can begin.")]
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Ground Hold")]
    [Tooltip("While grounded and holding the dash button, normal movement speed is " +
             "multiplied by this factor — on top of, and outlasting, the initial burst.")]
    [SerializeField] private float groundHoldSpeedMultiplier = 1.5f;

    [Header("Air Dash")]
    [Tooltip("Airborne dashes allowed before landing resets the count. Set high to " +
             "effectively remove the limit.")]
    [SerializeField] private int maxAirDashes = 1;

    // =========================
    // STATE
    // =========================

    private Rigidbody2D rb;
    private MovementScript movement;
    private SpriteRenderer spriteRenderer;
    private PlayerAnimation playerAnimation;
    private PlayerStamina stamina;

    private bool dashInputHeld;
    private float dashTimer;
    private float cooldownTimer;
    private float dashDirection = 1f;
    private int airDashesUsed;

    public bool IsDashing => dashTimer > 0f;
    public bool IsOnCooldown => cooldownTimer > 0f;
    public bool IsSprinting { get; private set; }

    // 1 normally, groundHoldSpeedMultiplier while sprinting — MovementScript multiplies this in.
    public float SprintSpeedMultiplier => IsSprinting ? groundHoldSpeedMultiplier : 1f;

    // Fired when a dash starts / ends. Hook VFX / audio here without editing this script.
    public event Action OnDashStarted;
    public event Action OnDashEnded;

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MovementScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<PlayerAnimation>();
        stamina = GetComponent<PlayerStamina>();
    }

    // =========================
    // INPUT ENTRY POINTS
    // =========================

    // Called by PlayerController1 when the dash button is pressed.
    public void TryDash()
    {
        if (IsDashing || IsOnCooldown)
            return;

        bool grounded = movement != null && movement.IsGrounded();

        if (!grounded && airDashesUsed >= maxAirDashes)
            return;

        if (stamina != null && !stamina.TryConsumeDash())
            return;

        dashDirection = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        if (!grounded)
            airDashesUsed++;

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);

        if (playerAnimation != null)
            playerAnimation.PlayDashAnimation();

        OnDashStarted?.Invoke();
    }

    // Called every frame by PlayerController1 with the current held state of the
    // dash button, so the ground-hold speed multiplier can track it continuously.
    public void SetDashHeld(bool held)
    {
        dashInputHeld = held;
    }

    // =========================
    // TICK
    // =========================

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                dashTimer = 0f;
                OnDashEnded?.Invoke();
            }
        }

        if (movement == null)
            return;

        bool grounded = movement.IsGrounded();

        if (grounded)
            airDashesUsed = 0;

        // Ground hold: sustained speed boost while the button stays down, as long
        // as stamina allows sprinting. MovementScript reads SprintSpeedMultiplier.
        bool wantsSprint = grounded && dashInputHeld;
        bool sprintAllowed = stamina == null || stamina.CanSprint;
        IsSprinting = wantsSprint && sprintAllowed;

        if (stamina != null)
            stamina.SetSprinting(IsSprinting);
    }

    void FixedUpdate()
    {
        if (!IsDashing)
            return;

        // Own the Rigidbody for the burst — MovementScript stands down while IsDashing.
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
    }
}
