using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(EnemyAttack2D))]
    public sealed class EnemyController2D : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle,
            Chase,
            Attack,
            Dead
        }

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private HealthComponent health;
        [SerializeField] private EnemyAttack2D attack;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float detectionRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 1.2f;
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float stopDistance = 0.9f;

        private Rigidbody2D body;
        private EnemyState currentState = EnemyState.Idle;
        private int facingDirection = -1;
        private float tacticalSuppressionTimer;
        private float tacticalMoveSpeedMultiplier = 1f;

        public static event Action<EnemyController2D> OnEnemyDeath;
        public event Action<EnemyController2D, EnemyState> StateChanged;

        public EnemyState CurrentState => currentState;
        public int FacingDirection => facingDirection;
        public Transform Target => target;
        public float CurrentMoveSpeed => moveSpeed * TacticalMoveSpeedMultiplier;
        public bool IsTacticallySuppressed => tacticalSuppressionTimer > 0f;
        public float TacticalMoveSpeedMultiplier => IsTacticallySuppressed ? tacticalMoveSpeedMultiplier : 1f;

        private void Reset()
        {
            health = GetComponent<HealthComponent>();
            attack = GetComponent<EnemyAttack2D>();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = health != null ? health : GetComponent<HealthComponent>();
            attack = attack != null ? attack : GetComponent<EnemyAttack2D>();

            if (attack != null)
            {
                attack.SetOwner(this);
            }
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

        private void FixedUpdate()
        {
            TickTacticalSuppression(Time.fixedDeltaTime);

            if (currentState == EnemyState.Dead || target == null)
            {
                StopMoving();
                return;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            if (distanceToTarget > detectionRange)
            {
                ChangeState(EnemyState.Idle);
                StopMoving();
                return;
            }

            UpdateFacingDirection();

            if (distanceToTarget <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                StopMoving();
                attack.TryAttack(target);
                return;
            }

            ChangeState(EnemyState.Chase);
            ChaseTarget(distanceToTarget);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void ChaseTarget(float distanceToTarget)
        {
            if (distanceToTarget <= stopDistance)
            {
                StopMoving();
                return;
            }

            Vector2 direction = ((Vector2)target.position - body.position).normalized;
            body.linearVelocity = new Vector2(direction.x * CurrentMoveSpeed, body.linearVelocity.y);
        }

        private void StopMoving()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void UpdateFacingDirection()
        {
            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                facingDirection = deltaX > 0f ? 1 : -1;
            }
        }

        private void ChangeState(EnemyState nextState)
        {
            if (currentState == nextState)
            {
                return;
            }

            currentState = nextState;
            StateChanged?.Invoke(this, currentState);
        }

        private void HandleDeath(HealthComponent deadHealth, DamageInfo damageInfo)
        {
            ChangeState(EnemyState.Dead);
            StopMoving();
            OnEnemyDeath?.Invoke(this);
            Debug.Log($"{name} died. Source: {damageInfo.sourceType}", this);
        }

        public void ApplyTacticalSuppression(float duration, float moveSpeedMultiplier)
        {
            if (duration <= 0f)
            {
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(tacticalSuppressionTimer, duration);
            tacticalMoveSpeedMultiplier = Mathf.Min(
                IsTacticallySuppressed ? tacticalMoveSpeedMultiplier : 1f,
                Mathf.Clamp(moveSpeedMultiplier, 0.05f, 1f));
        }

        public void TickTacticalSuppression(float deltaTime)
        {
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalMoveSpeedMultiplier = 1f;
                return;
            }

            tacticalSuppressionTimer = Mathf.Max(0f, tacticalSuppressionTimer - Mathf.Max(0f, deltaTime));
            if (tacticalSuppressionTimer <= 0f)
            {
                tacticalMoveSpeedMultiplier = 1f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
