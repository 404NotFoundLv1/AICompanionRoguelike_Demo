using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private HealthComponent health;

        [Header("Move")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float groundAcceleration = 80f;
        [SerializeField, Min(0f)] private float groundDeceleration = 90f;
        [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.75f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpVelocity = 13f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.08f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 18f;
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.45f;
        [SerializeField, Min(0f)] private float invincibilityDuration = 0.22f;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        private Rigidbody2D body;
        private ContactFilter2D groundFilter;
        private float defaultGravityScale;
        private float dashTimer;
        private float dashCooldownTimer;
        private float invincibilityTimer;
        private int facingDirection = 1;

        public event Action Jumped;
        public event Action DashStarted;
        public event Action DashEnded;
        public event Action<bool> GroundedChanged;

        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsInvincible { get; private set; }
        public int FacingDirection => facingDirection;
        public float MoveSpeed => moveSpeed;
        public float DashCooldown => dashCooldown;

        private void Reset()
        {
            inputReader = GetComponent<PlayerInputReader>();
            health = GetComponent<HealthComponent>();

            Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
            rigidbody2D.gravityScale = 4f;
            rigidbody2D.freezeRotation = true;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
            health = health != null ? health : GetComponent<HealthComponent>();
            defaultGravityScale = body.gravityScale;
            ConfigureGroundFilter();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void OnValidate()
        {
            ConfigureGroundFilter();
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();

            if (health != null && health.IsDead)
            {
                LockDeadMovement();
                return;
            }

            UpdateTimers();

            if (inputReader == null)
            {
                return;
            }

            UpdateFacingDirection(inputReader.MoveInput);
            TryStartDash();

            if (IsDashing)
            {
                ApplyDashVelocity();
                return;
            }

            TryJump();
            ApplyHorizontalMovement();
        }

        private void ConfigureGroundFilter()
        {
            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayerMask,
                useTriggers = false
            };
        }

        private void UpdateTimers()
        {
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.fixedDeltaTime;
            }

            if (IsDashing)
            {
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    StopDash();
                }
            }

            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.fixedDeltaTime;
                IsInvincible = invincibilityTimer > 0f;
            }
            else
            {
                IsInvincible = false;
            }
        }

        private void UpdateGroundedState()
        {
            bool wasGrounded = IsGrounded;
            int hitCount = body.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
            IsGrounded = hitCount > 0;

            if (wasGrounded != IsGrounded)
            {
                GroundedChanged?.Invoke(IsGrounded);
            }
        }

        private void UpdateFacingDirection(float moveInput)
        {
            if (moveInput > 0.01f)
            {
                facingDirection = 1;
            }
            else if (moveInput < -0.01f)
            {
                facingDirection = -1;
            }
        }

        private void TryJump()
        {
            if (!inputReader.ConsumeJumpPressed() || !IsGrounded)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpVelocity;
            body.linearVelocity = velocity;

            Jumped?.Invoke();
        }

        private void TryStartDash()
        {
            if (!inputReader.ConsumeDashPressed() || IsDashing || dashCooldownTimer > 0f)
            {
                return;
            }

            IsDashing = true;
            IsInvincible = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            invincibilityTimer = invincibilityDuration;
            body.gravityScale = 0f;

            DashStarted?.Invoke();
        }

        private void StopDash()
        {
            IsDashing = false;
            body.gravityScale = defaultGravityScale;

            Vector2 velocity = body.linearVelocity;
            velocity.x = inputReader != null ? inputReader.MoveInput * moveSpeed : 0f;
            body.linearVelocity = velocity;

            DashEnded?.Invoke();
        }

        private void ApplyDashVelocity()
        {
            body.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = inputReader.MoveInput * moveSpeed;
            Vector2 velocity = body.linearVelocity;
            float acceleration = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;

            if (!IsGrounded)
            {
                acceleration *= airControlMultiplier;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        public void MultiplyMoveSpeed(float multiplier)
        {
            moveSpeed = Mathf.Max(0f, moveSpeed * Mathf.Max(0f, multiplier));
        }

        public void MultiplyDashCooldown(float multiplier)
        {
            dashCooldown = Mathf.Max(0f, dashCooldown * Mathf.Max(0f, multiplier));
            dashCooldownTimer = Mathf.Min(dashCooldownTimer, dashCooldown);
        }

        private void HandleDeath(HealthComponent deadHealth, DamageInfo damageInfo)
        {
            LockDeadMovement();
        }

        private void LockDeadMovement()
        {
            if (body == null)
            {
                return;
            }

            bool wasDashing = IsDashing;
            IsDashing = false;
            IsInvincible = false;
            dashTimer = 0f;
            dashCooldownTimer = 0f;
            invincibilityTimer = 0f;
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

            if (wasDashing)
            {
                DashEnded?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        }
    }
}
