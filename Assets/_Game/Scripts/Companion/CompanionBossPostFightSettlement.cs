using System;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public readonly struct CompanionBossPostFightReport
    {
        public CompanionBossPostFightReport(
            string feedbackLine,
            int trustDelta,
            int affectionDelta,
            int supportActivations,
            int warningHits,
            int warningDodges,
            bool lowHealthVictory)
        {
            FeedbackLine = feedbackLine ?? string.Empty;
            TrustDelta = trustDelta;
            AffectionDelta = affectionDelta;
            SupportActivations = supportActivations;
            WarningHits = warningHits;
            WarningDodges = warningDodges;
            LowHealthVictory = lowHealthVictory;
        }

        public string FeedbackLine { get; }
        public int TrustDelta { get; }
        public int AffectionDelta { get; }
        public int SupportActivations { get; }
        public int WarningHits { get; }
        public int WarningDodges { get; }
        public bool LowHealthVictory { get; }
    }

    public sealed class CompanionBossPostFightSettlement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private CompanionRelationship relationship;
        [SerializeField] private CompanionBossSupport bossSupport;

        [Header("Relationship Changes")]
        [SerializeField] private int supportTrustDelta = 1;
        [SerializeField] private int supportAffectionDelta = 1;
        [SerializeField] private int cleanDodgeTrustDelta = 2;
        [SerializeField] private int cleanDodgeAffectionDelta = 1;
        [SerializeField] private int warningHitTrustDelta = -1;
        [SerializeField] private int warningHitAffectionDelta;
        [SerializeField] private int lowHealthTrustDelta;
        [SerializeField] private int lowHealthAffectionDelta = 1;
        [SerializeField, Range(0.01f, 1f)] private float lowHealthRatioThreshold = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool logSettlement = true;

        private BossTelegraphedAttack2D subscribedBossAttack;
        private bool trackingBossRoom;
        private bool settledCurrentBoss;
        private int supportActivations;
        private int warningHits;
        private int warningDodges;
        private int warningsStarted;
        private CompanionBossPostFightReport lastReport;

        public event Action<CompanionBossPostFightSettlement, CompanionBossPostFightReport> BossPostFightSettled;

        public int SupportActivations => supportActivations;
        public int WarningHits => warningHits;
        public int WarningDodges => warningDodges;
        public int WarningsStarted => warningsStarted;
        public bool HasSettledCurrentBoss => settledCurrentBoss;
        public CompanionBossPostFightReport LastReport => lastReport;

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
            SubscribeToRoomManager();
            SubscribeToBossSupport();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToRoomManager();
            SubscribeToBossSupport();
        }

        private void Update()
        {
            if (roomManager == null || playerHealth == null || relationship == null || bossSupport == null)
            {
                ResolveReferences();
                SubscribeToRoomManager();
                SubscribeToBossSupport();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRoomManager();
            UnsubscribeFromBossSupport();
            UnsubscribeFromBossAttack();
        }

        private void OnValidate()
        {
            lowHealthRatioThreshold = Mathf.Clamp(lowHealthRatioThreshold, 0.01f, 1f);
        }

        public void Configure(HealthComponent playerHealth, CompanionRelationship relationship)
        {
            this.playerHealth = playerHealth;
            this.relationship = relationship;
        }

        public void RecordSupportActivated()
        {
            if (!trackingBossRoom && Application.isPlaying)
            {
                return;
            }

            supportActivations++;
        }

        public void RecordBossWarningResolved(bool hit)
        {
            if (hit)
            {
                warningHits++;
            }
            else
            {
                warningDodges++;
            }
        }

        public CompanionBossPostFightReport SettleBossVictory()
        {
            if (settledCurrentBoss)
            {
                return lastReport;
            }

            settledCurrentBoss = true;

            int previousTrust = relationship != null ? relationship.Trust : -1;
            int previousAffection = relationship != null ? relationship.Affection : -1;
            bool lowHealthVictory = IsLowHealthVictory();

            if (relationship != null)
            {
                ApplyRelationshipEvents(lowHealthVictory);
            }

            int trustDelta = relationship != null && previousTrust >= 0
                ? relationship.Trust - previousTrust
                : 0;
            int affectionDelta = relationship != null && previousAffection >= 0
                ? relationship.Affection - previousAffection
                : 0;

            lastReport = new CompanionBossPostFightReport(
                BuildFeedbackLine(lowHealthVictory),
                trustDelta,
                affectionDelta,
                supportActivations,
                warningHits,
                warningDodges,
                lowHealthVictory);

            RunSessionState.RecordCompanionBossFeedback(
                lastReport.FeedbackLine,
                lastReport.TrustDelta,
                lastReport.AffectionDelta,
                lastReport.SupportActivations,
                lastReport.WarningHits,
                lastReport.WarningDodges);

            if (logSettlement)
            {
                Debug.Log(
                    $"Boss post-fight AI feedback settled: {lastReport.FeedbackLine} Trust {trustDelta:+#;-#;0}, Affection {affectionDelta:+#;-#;0}",
                    this);
            }

            BossPostFightSettled?.Invoke(this, lastReport);
            return lastReport;
        }

        private void ResolveReferences()
        {
            roomManager = roomManager != null ? roomManager : FindAnyObjectByType<RoomManager>();
            bossSupport = bossSupport != null ? bossSupport : FindAnyObjectByType<CompanionBossSupport>();
            relationship = relationship != null ? relationship : FindAnyObjectByType<CompanionRelationship>();

            if (playerHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
            }
        }

        private void SubscribeToRoomManager()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.RoomStarted -= HandleRoomStarted;
            roomManager.RoomCleared -= HandleRoomCleared;
            roomManager.RoomStarted += HandleRoomStarted;
            roomManager.RoomCleared += HandleRoomCleared;
        }

        private void UnsubscribeFromRoomManager()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.RoomStarted -= HandleRoomStarted;
            roomManager.RoomCleared -= HandleRoomCleared;
        }

        private void SubscribeToBossSupport()
        {
            if (bossSupport == null)
            {
                return;
            }

            bossSupport.SupportActivated -= HandleBossSupportActivated;
            bossSupport.SupportActivated += HandleBossSupportActivated;
        }

        private void UnsubscribeFromBossSupport()
        {
            if (bossSupport == null)
            {
                return;
            }

            bossSupport.SupportActivated -= HandleBossSupportActivated;
        }

        private void HandleRoomStarted(RoomManager manager, RoomType roomType, int roomNumber)
        {
            if (roomType != RoomType.BossRoom)
            {
                trackingBossRoom = false;
                return;
            }

            ResetBossTracking();
            trackingBossRoom = true;
            SubscribeToActiveBossAttack(manager);
        }

        private void HandleRoomCleared(RoomManager manager, RoomType roomType, int roomNumber)
        {
            if (roomType == RoomType.BossRoom)
            {
                SettleBossVictory();
            }
        }

        private void HandleBossSupportActivated(CompanionBossSupport support)
        {
            RecordSupportActivated();
        }

        private void SubscribeToActiveBossAttack(RoomManager manager)
        {
            UnsubscribeFromBossAttack();

            if (manager == null)
            {
                return;
            }

            for (int i = 0; i < manager.ActiveEnemies.Count; i++)
            {
                EnemyController2D enemy = manager.ActiveEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                BossTelegraphedAttack2D attack = enemy.GetComponent<BossTelegraphedAttack2D>();
                if (attack == null)
                {
                    continue;
                }

                subscribedBossAttack = attack;
                subscribedBossAttack.WarningStarted += HandleBossWarningStarted;
                subscribedBossAttack.AttackResolved += HandleBossAttackResolved;
                return;
            }
        }

        private void UnsubscribeFromBossAttack()
        {
            if (subscribedBossAttack == null)
            {
                return;
            }

            subscribedBossAttack.WarningStarted -= HandleBossWarningStarted;
            subscribedBossAttack.AttackResolved -= HandleBossAttackResolved;
            subscribedBossAttack = null;
        }

        private void HandleBossWarningStarted(BossTelegraphedAttack2D attack)
        {
            warningsStarted++;
        }

        private void HandleBossAttackResolved(BossTelegraphedAttack2D attack, bool hit)
        {
            RecordBossWarningResolved(hit);
        }

        private void ResetBossTracking()
        {
            settledCurrentBoss = false;
            supportActivations = 0;
            warningHits = 0;
            warningDodges = 0;
            warningsStarted = 0;
            lastReport = default;
            UnsubscribeFromBossAttack();
        }

        private void ApplyRelationshipEvents(bool lowHealthVictory)
        {
            if (supportActivations > 0)
            {
                relationship.ApplyMemoryEvent(
                    "Boss Aftercare: Shield",
                    supportTrustDelta,
                    supportAffectionDelta,
                    RelationshipMemoryTag.Protected);
            }

            if (warningDodges > 0 && warningHits == 0)
            {
                relationship.ApplyMemoryEvent(
                    "Boss Aftercare: Clean Dodge",
                    cleanDodgeTrustDelta,
                    cleanDodgeAffectionDelta,
                    RelationshipMemoryTag.Reliable);
            }

            if (warningHits > 0)
            {
                relationship.ApplyMemoryEvent(
                    "Boss Aftercare: Warning Hit",
                    warningHitTrustDelta,
                    warningHitAffectionDelta,
                    RelationshipMemoryTag.Stubborn);
            }

            if (lowHealthVictory)
            {
                relationship.ApplyMemoryEvent(
                    "Boss Aftercare: Low Health",
                    lowHealthTrustDelta,
                    lowHealthAffectionDelta,
                    RelationshipMemoryTag.Brave);
            }
        }

        private bool IsLowHealthVictory()
        {
            if (playerHealth == null || playerHealth.MaxHealth <= 0f)
            {
                return false;
            }

            return playerHealth.CurrentHealth / playerHealth.MaxHealth <= lowHealthRatioThreshold;
        }

        private string BuildFeedbackLine(bool lowHealthVictory)
        {
            if (supportActivations > 0 && warningHits == 0 && warningDodges > 0)
            {
                return lowHealthVictory
                    ? "AI: I shielded you, and you trusted my warning. That was close, but we made it."
                    : "AI: I shielded you, and you trusted my warning. Good fight.";
            }

            if (supportActivations > 0)
            {
                return "AI: I shielded you, but that warning hit us. Next time, move sooner.";
            }

            if (warningHits > 0)
            {
                return "AI: You survived, but that warning hit you. Listen for my call next time.";
            }

            if (warningDodges > 0)
            {
                return "AI: Clean dodges. You heard me.";
            }

            if (lowHealthVictory)
            {
                return "AI: That was too close. I am glad you made it back.";
            }

            return "AI: Boss down. We made it through.";
        }
    }
}
