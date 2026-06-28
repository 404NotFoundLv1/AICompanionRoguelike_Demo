using System;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    [RequireComponent(typeof(CompanionRelationship))]
    public sealed class CompanionTacticalSupport : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CompanionRelationship relationship;
        [SerializeField] private CompanionSensor sensor;
        [SerializeField] private CompanionCombatDialogueController dialogue;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private PlayerBossSupportShield playerShield;
        [SerializeField] private RunManager runManager;

        [Header("Triggers")]
        [SerializeField, Range(0.05f, 1f)] private float lowHealthGuardThreshold = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float suppressionHealthThreshold = 0.55f;
        [SerializeField, Range(0.01f, 1f)] private float suppressionMinimumHealthThreshold = 0.22f;
        [SerializeField, Min(0f)] private float suppressionRange = 4.8f;

        [Header("Memory")]
        [SerializeField] private bool recordSupportMemory = true;
        [SerializeField] private int guardAffectionDelta = 1;
        [SerializeField] private int suppressionTrustDelta = 1;

        [Header("Debug")]
        [SerializeField] private bool logSupport;

        private HealthComponent subscribedPlayerHealth;
        private float guardCooldownTimer;
        private float suppressionCooldownTimer;

        public event Action<CompanionTacticalSupport, string> GuardActivated;
        public event Action<CompanionTacticalSupport, HealthComponent> SuppressionActivated;

        public bool IsGuardOnCooldown => guardCooldownTimer > 0f;
        public bool IsSuppressionOnCooldown => suppressionCooldownTimer > 0f;
        public float GuardCooldownRemaining => guardCooldownTimer;
        public float SuppressionCooldownRemaining => suppressionCooldownTimer;
        public CompanionSkillTendency CurrentTendency => CompanionRunBuildState.CurrentTendency;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToPlayerHealth();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToPlayerHealth();
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (relationship == null || sensor == null || playerHealth == null || playerShield == null || dialogue == null)
            {
                ResolveReferences();
            }

            SubscribeToPlayerHealth();
            TrySuppressCurrentTarget();
        }

        private void OnDisable()
        {
            UnsubscribeFromPlayerHealth();
        }

        private void OnValidate()
        {
            lowHealthGuardThreshold = Mathf.Clamp(lowHealthGuardThreshold, 0.05f, 1f);
            suppressionHealthThreshold = Mathf.Clamp(suppressionHealthThreshold, 0.05f, 1f);
            suppressionMinimumHealthThreshold = Mathf.Clamp(suppressionMinimumHealthThreshold, 0.01f, 1f);
            suppressionRange = Mathf.Max(0f, suppressionRange);
        }

        public void Configure(
            HealthComponent playerHealth,
            CompanionRelationship relationship,
            PlayerBossSupportShield playerShield,
            CompanionSensor sensor,
            CompanionCombatDialogueController dialogue)
        {
            this.playerHealth = playerHealth;
            this.relationship = relationship;
            this.playerShield = playerShield;
            this.sensor = sensor;
            this.dialogue = dialogue;
            SubscribeToPlayerHealth();
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (guardCooldownTimer > 0f)
            {
                guardCooldownTimer = Mathf.Max(0f, guardCooldownTimer - safeDeltaTime);
            }

            if (suppressionCooldownTimer > 0f)
            {
                suppressionCooldownTimer = Mathf.Max(0f, suppressionCooldownTimer - safeDeltaTime);
            }
        }

        public bool TryActivateGuard(string sourceLabel = "Tactical Guard")
        {
            ResolveReferences();
            if (playerShield == null || IsGuardOnCooldown)
            {
                return false;
            }

            CompanionTacticalSupportTuning tuning = GetCurrentTuning();
            playerShield.Activate(tuning.GuardDuration, tuning.GuardDamageMultiplier);
            guardCooldownTimer = tuning.GuardCooldown;

            if (recordSupportMemory && relationship != null)
            {
                relationship.ApplyMemoryEvent(sourceLabel, 0, guardAffectionDelta, RelationshipMemoryTag.Protected);
            }

            if (dialogue != null)
            {
                TrySpeakBuildActivationFeedback(CompanionDialogueEventType.TacticalGuard, 5);
            }

            GuardActivated?.Invoke(this, sourceLabel);

            if (logSupport)
            {
                Debug.Log(
                    $"AI tactical guard active for {tuning.GuardDuration:0.0}s. Incoming damage x{tuning.GuardDamageMultiplier:0.##}.",
                    this);
            }

            return true;
        }

        public bool TrySuppressCurrentTarget()
        {
            HealthComponent targetHealth = sensor != null ? sensor.CurrentTargetHealth : null;
            return TrySuppressTarget(targetHealth);
        }

        public bool TrySuppressTarget(HealthComponent targetHealth)
        {
            ResolveReferences();
            if (targetHealth == null || targetHealth.IsDead || IsSuppressionOnCooldown)
            {
                return false;
            }

            CompanionTacticalSupportTuning tuning = GetCurrentTuning();
            float healthRatio = targetHealth.MaxHealth > 0f ? targetHealth.CurrentHealth / targetHealth.MaxHealth : 0f;
            float effectiveSuppressionThreshold = Mathf.Max(
                suppressionHealthThreshold,
                tuning.SuppressionTriggerHealthRatio);
            if (healthRatio > effectiveSuppressionThreshold || healthRatio <= suppressionMinimumHealthThreshold)
            {
                return false;
            }

            if (suppressionRange > 0f && Vector2.Distance(transform.position, targetHealth.transform.position) > suppressionRange)
            {
                return false;
            }

            EnemyAttack2D enemyAttack = targetHealth.GetComponent<EnemyAttack2D>();
            EnemyController2D enemyController = targetHealth.GetComponent<EnemyController2D>();
            if (enemyAttack == null && enemyController == null)
            {
                return false;
            }

            if (enemyAttack != null)
            {
                enemyAttack.ApplyTacticalSuppression(tuning.SuppressionDuration, tuning.SuppressionDamageMultiplier);
            }

            if (enemyController != null)
            {
                enemyController.ApplyTacticalSuppression(tuning.SuppressionDuration, tuning.SuppressionMoveMultiplier);
            }

            suppressionCooldownTimer = tuning.SuppressionCooldown;

            if (recordSupportMemory && relationship != null)
            {
                relationship.ApplyMemoryEvent("Tactical Suppression", suppressionTrustDelta, 0, RelationshipMemoryTag.Brave);
            }

            if (dialogue != null)
            {
                TrySpeakBuildActivationFeedback(CompanionDialogueEventType.TacticalSuppression, 4);
            }

            SuppressionActivated?.Invoke(this, targetHealth);

            if (logSupport)
            {
                Debug.Log(
                    $"AI tactical suppression applied to {targetHealth.name} for {tuning.SuppressionDuration:0.0}s.",
                    targetHealth);
            }

            return true;
        }

        public string GetStatusLabel()
        {
            return GetCooldownStatusLabel();
        }

        public string GetCooldownStatusLabel()
        {
            string guard = IsGuardOnCooldown ? $"Guard {guardCooldownTimer:0.0}s" : "Guard Ready";
            string suppress = IsSuppressionOnCooldown ? $"Suppress {suppressionCooldownTimer:0.0}s" : "Suppress Ready";
            return $"AI Tactics: {guard} | {suppress}";
        }

        private void ResolveReferences()
        {
            relationship = relationship != null ? relationship : GetComponent<CompanionRelationship>();
            sensor = sensor != null ? sensor : GetComponent<CompanionSensor>();
            dialogue = dialogue != null ? dialogue : GetComponent<CompanionCombatDialogueController>();
            runManager = runManager != null ? runManager : RunManager.FindActiveRunManager();

            if (playerHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
            }

            if (playerShield == null && playerHealth != null)
            {
                playerShield = playerHealth.GetComponent<PlayerBossSupportShield>();
                if (playerShield == null)
                {
                    playerShield = playerHealth.gameObject.AddComponent<PlayerBossSupportShield>();
                }
            }
        }

        private CompanionRelationshipProfileSnapshot BuildProfile()
        {
            return relationship != null
                ? CompanionRelationshipProfile.Evaluate(
                    relationship.Trust,
                    relationship.Affection,
                    relationship.MemoryTags)
                : CompanionRelationshipProfile.Evaluate(
                    50,
                    50,
                    Array.Empty<RelationshipMemoryTagScore>());
        }

        private CompanionTacticalSupportTuning GetCurrentTuning()
        {
            int routeBonusLevel = runManager != null ? runManager.BuildRouteBonusLevel : 0;
            return CompanionTacticalSupportRules.Evaluate(BuildProfile(), CurrentTendency, routeBonusLevel);
        }

        private void TrySpeakBuildActivationFeedback(CompanionDialogueEventType eventType, int priority)
        {
            if (dialogue == null)
            {
                return;
            }

            string buildLine = CompanionSkillTendencyRules.GetTacticalActivationLine(CurrentTendency, eventType);
            if (!string.IsNullOrWhiteSpace(buildLine) && dialogue.TryShowLine(buildLine, priority))
            {
                return;
            }

            dialogue.TrySpeak(eventType, priority);
        }

        private void SubscribeToPlayerHealth()
        {
            if (playerHealth == subscribedPlayerHealth)
            {
                return;
            }

            UnsubscribeFromPlayerHealth();
            if (playerHealth == null)
            {
                return;
            }

            subscribedPlayerHealth = playerHealth;
            subscribedPlayerHealth.Damaged += HandlePlayerDamaged;
        }

        private void UnsubscribeFromPlayerHealth()
        {
            if (subscribedPlayerHealth == null)
            {
                return;
            }

            subscribedPlayerHealth.Damaged -= HandlePlayerDamaged;
            subscribedPlayerHealth = null;
        }

        private void HandlePlayerDamaged(HealthComponent health, DamageInfo damageInfo)
        {
            if (health == null || health != playerHealth || health.IsDead || damageInfo.sourceType != DamageSourceType.Enemy)
            {
                return;
            }

            float healthRatio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
            CompanionTacticalSupportTuning tuning = GetCurrentTuning();
            float effectiveGuardThreshold = Mathf.Max(lowHealthGuardThreshold, tuning.GuardTriggerHealthRatio);
            if (healthRatio <= effectiveGuardThreshold)
            {
                TryActivateGuard("Low Health Guard");
            }
        }
    }
}
