using System;
using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public sealed class CompanionSensor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent ownerHealth;

        [Header("Detection")]
        [SerializeField, Min(0f)] private float detectionRadius = 4f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.15f;
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        private readonly Collider2D[] hitBuffer = new Collider2D[16];

        private ContactFilter2D enemyFilter;
        private float scanTimer;
        private HealthComponent currentTargetHealth;

        public event Action<HealthComponent> TargetChanged;

        public HealthComponent CurrentTargetHealth => currentTargetHealth;
        public Transform CurrentTarget => currentTargetHealth != null ? currentTargetHealth.transform : null;
        public bool HasTarget => IsValidTarget(currentTargetHealth);

        private void Reset()
        {
            ownerHealth = GetComponent<HealthComponent>();
        }

        private void Awake()
        {
            ownerHealth = ownerHealth != null ? ownerHealth : GetComponent<HealthComponent>();
            ConfigureEnemyFilter();
        }

        private void OnValidate()
        {
            scanInterval = Mathf.Max(0.02f, scanInterval);
            ConfigureEnemyFilter();
        }

        private void OnEnable()
        {
            scanTimer = 0f;
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f)
            {
                return;
            }

            scanTimer = scanInterval;
            ScanNow();
        }

        public HealthComponent ScanNow()
        {
            HealthComponent nextTarget = null;

            if (ownerHealth == null || !ownerHealth.IsDead)
            {
                nextTarget = FindNearestTarget();
            }

            SetCurrentTarget(nextTarget);
            return currentTargetHealth;
        }

        private void ConfigureEnemyFilter()
        {
            enemyFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = enemyLayerMask,
                useTriggers = false
            };
        }

        private HealthComponent FindNearestTarget()
        {
            int hitCount = Physics2D.OverlapCircle(transform.position, detectionRadius, enemyFilter, hitBuffer);
            HealthComponent nearestTarget = null;
            float nearestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                HealthComponent candidate = GetHealthFromCollider(hitBuffer[i]);
                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                float sqrDistance = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestTarget = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearestTarget;
        }

        private static HealthComponent GetHealthFromCollider(Collider2D hit)
        {
            if (hit == null)
            {
                return null;
            }

            if (hit.TryGetComponent(out HealthComponent health))
            {
                return health;
            }

            return hit.GetComponentInParent<HealthComponent>();
        }

        private bool IsValidTarget(HealthComponent targetHealth)
        {
            return targetHealth != null
                && targetHealth != ownerHealth
                && !targetHealth.IsDead
                && IsInEnemyLayer(targetHealth.gameObject.layer)
                && Vector2.Distance(transform.position, targetHealth.transform.position) <= detectionRadius;
        }

        private bool IsInEnemyLayer(int layer)
        {
            return (enemyLayerMask.value & (1 << layer)) != 0;
        }

        private void SetCurrentTarget(HealthComponent nextTarget)
        {
            if (currentTargetHealth == nextTarget)
            {
                return;
            }

            currentTargetHealth = nextTarget;
            TargetChanged?.Invoke(currentTargetHealth);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
