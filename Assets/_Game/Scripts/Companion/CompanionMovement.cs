using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CompanionMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovement2D targetMovement;
        [SerializeField] private HealthComponent health;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float followDistance = 1.6f;
        [SerializeField, Min(0f)] private float followTolerance = 0.25f;
        [SerializeField, Min(0f)] private float moveSpeed = 5.5f;
        [SerializeField, Min(0f)] private float acceleration = 60f;
        [SerializeField, Min(0f)] private float teleportDistance = 9f;

        [Header("Ground")]
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.08f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        private Rigidbody2D body;
        private ContactFilter2D groundFilter;
        private int facingDirection = 1;

        public bool IsGrounded { get; private set; }
        public int FacingDirection => facingDirection;
        public Transform Target => target;

        private void Reset()
        {
            health = GetComponent<HealthComponent>();

            Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
            rigidbody2D.gravityScale = 4f;
            rigidbody2D.freezeRotation = true;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = health != null ? health : GetComponent<HealthComponent>();
            ConfigureGroundFilter();
        }

        private void OnValidate()
        {
            ConfigureGroundFilter();
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();

            if ((health != null && health.IsDead) || target == null)
            {
                StopHorizontalMovement();
                return;
            }

            float distanceToTarget = Vector2.Distance(body.position, target.position);
            if (distanceToTarget > teleportDistance)
            {
                TeleportNearTarget();
                return;
            }

            FollowTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetMovement = target != null ? target.GetComponent<PlayerMovement2D>() : null;
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

        private void UpdateGroundedState()
        {
            int hitCount = body != null ? body.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance) : 0;
            IsGrounded = hitCount > 0;
        }

        private void FollowTarget()
        {
            int targetFacingDirection = GetTargetFacingDirection();
            float desiredX = target.position.x - targetFacingDirection * followDistance;
            float deltaX = desiredX - body.position.x;

            if (Mathf.Abs(deltaX) <= followTolerance)
            {
                StopHorizontalMovement();
                return;
            }

            facingDirection = deltaX > 0f ? 1 : -1;
            float targetVelocityX = facingDirection * moveSpeed;
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVelocityX, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private int GetTargetFacingDirection()
        {
            if (targetMovement != null)
            {
                return targetMovement.FacingDirection;
            }

            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= 0.01f)
            {
                return facingDirection;
            }

            return deltaX > 0f ? 1 : -1;
        }

        private void TeleportNearTarget()
        {
            int targetFacingDirection = GetTargetFacingDirection();
            Vector2 teleportPosition = new Vector2(target.position.x - targetFacingDirection * followDistance, target.position.y);
            body.position = teleportPosition;
            body.linearVelocity = Vector2.zero;
        }

        private void StopHorizontalMovement()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, followDistance);

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        }
    }
}
