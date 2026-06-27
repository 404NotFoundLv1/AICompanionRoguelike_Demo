using System;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public sealed class CompanionSensor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent ownerHealth;
        [SerializeField] private Transform protectedPlayer;

        [Header("Detection")]
        [SerializeField, Min(0f)] private float detectionRadius = 4f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.15f;
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        private readonly Collider2D[] hitBuffer = new Collider2D[16];

        private ContactFilter2D enemyFilter;
        private float scanTimer;
        private HealthComponent currentTargetHealth;
        private CompanionTargetDecisionReason currentTargetDecisionReason;
        private float currentTargetScore = float.NegativeInfinity;

        public event Action<HealthComponent> TargetChanged;

        public HealthComponent CurrentTargetHealth => currentTargetHealth;
        public Transform CurrentTarget => currentTargetHealth != null ? currentTargetHealth.transform : null;
        public bool HasTarget => IsValidTarget(currentTargetHealth);
        public CompanionTargetDecisionReason CurrentTargetDecisionReason => currentTargetDecisionReason;
        public float CurrentTargetScore => currentTargetScore;
        public EnemyArchetypeType CurrentTargetArchetype => ResolveArchetype(currentTargetHealth);

        private void Reset()
        {
            ownerHealth = GetComponent<HealthComponent>();
        }

        private void Awake()
        {
            ownerHealth = ownerHealth != null ? ownerHealth : GetComponent<HealthComponent>();
            ResolveProtectedPlayer();
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
            CompanionTargetDecisionReason nextReason = CompanionTargetDecisionReason.None;
            float nextScore = float.NegativeInfinity;

            if (ownerHealth == null || !ownerHealth.IsDead)
            {
                ResolveProtectedPlayer();
                nextTarget = FindHighestPriorityTarget(out nextReason, out nextScore);
            }

            SetCurrentTarget(nextTarget, nextReason, nextScore);
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

        private HealthComponent FindHighestPriorityTarget(
            out CompanionTargetDecisionReason selectedReason,
            out float selectedScore)
        {
            int hitCount = Physics2D.OverlapCircle(transform.position, detectionRadius, enemyFilter, hitBuffer);
            HealthComponent highestPriorityTarget = null;
            float highestScore = float.NegativeInfinity;
            float shortestDistance = float.PositiveInfinity;
            CompanionTargetDecisionReason highestPriorityReason = CompanionTargetDecisionReason.None;

            for (int i = 0; i < hitCount; i++)
            {
                HealthComponent candidate = GetHealthFromCollider(hitBuffer[i]);
                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                float companionDistance = Vector2.Distance(transform.position, candidate.transform.position);
                float playerDistance = protectedPlayer != null
                    ? Vector2.Distance(protectedPlayer.position, candidate.transform.position)
                    : detectionRadius;
                EnemyArchetypeType archetype = ResolveArchetype(candidate);
                EnemyAttack2D attack = candidate.GetComponent<EnemyAttack2D>();
                bool warningActive = attack != null && attack.IsWarningActive;
                float score = CompanionTargetPriorityRules.EvaluateScore(
                    archetype,
                    CompanionRunBuildState.CurrentTendency,
                    companionDistance,
                    playerDistance,
                    warningActive,
                    candidate == currentTargetHealth);

                bool hasHigherScore = score > highestScore + 0.001f;
                bool winsDistanceTie = Mathf.Abs(score - highestScore) <= 0.001f
                    && companionDistance < shortestDistance;
                if (!hasHigherScore && !winsDistanceTie)
                {
                    continue;
                }

                highestPriorityTarget = candidate;
                highestScore = score;
                shortestDistance = companionDistance;
                highestPriorityReason = CompanionTargetPriorityRules.EvaluateReason(
                    archetype,
                    CompanionRunBuildState.CurrentTendency,
                    playerDistance,
                    warningActive);
            }

            selectedReason = highestPriorityReason;
            selectedScore = highestScore;
            return highestPriorityTarget;
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

        private static EnemyArchetypeType ResolveArchetype(HealthComponent targetHealth)
        {
            if (targetHealth != null && targetHealth.TryGetComponent(out EnemyArchetype2D archetype))
            {
                return archetype.ArchetypeType;
            }

            return EnemyArchetypeType.Melee;
        }

        private void ResolveProtectedPlayer()
        {
            if (protectedPlayer != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            protectedPlayer = player != null ? player.transform : null;
        }

        private void SetCurrentTarget(
            HealthComponent nextTarget,
            CompanionTargetDecisionReason nextReason,
            float nextScore)
        {
            if (currentTargetHealth == nextTarget)
            {
                currentTargetDecisionReason = nextReason;
                currentTargetScore = nextScore;
                return;
            }

            currentTargetHealth = nextTarget;
            currentTargetDecisionReason = nextReason;
            currentTargetScore = nextScore;
            TargetChanged?.Invoke(currentTargetHealth);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
