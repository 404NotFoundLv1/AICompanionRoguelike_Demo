using System;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public sealed class CompanionBossSupport : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private CompanionRelationship relationship;
        [SerializeField] private PlayerBossSupportShield playerShield;
        [SerializeField] private BossTelegraphedAttack2D bossAttack;

        [Header("Support")]
        [SerializeField, Range(0, 100)] private int requiredTrust = 30;
        [SerializeField, Min(0.1f)] private float shieldDuration = 2f;
        [SerializeField, Range(0f, 1f)] private float incomingDamageMultiplier = 0.5f;
        [SerializeField, Min(0.1f)] private float cooldown = 5f;
        [SerializeField, Range(0.1f, 1f)] private float highTrustCooldownMultiplier = 0.7f;

        [Header("Memory")]
        [SerializeField] private bool recordSupportMemory = true;
        [SerializeField] private int supportTrustDelta;
        [SerializeField] private int supportAffectionDelta = 1;

        [Header("Debug")]
        [SerializeField] private bool logSupportMessages = true;

        private BossTelegraphedAttack2D subscribedBossAttack;
        private float cooldownTimer;

        public event Action<CompanionBossSupport> SupportPrompted;
        public event Action<CompanionBossSupport> SupportActivated;

        public bool IsOnCooldown => cooldownTimer > 0f;
        public float CooldownRemaining => cooldownTimer;
        public bool CanActivateSupport => playerShield != null
            && !IsOnCooldown
            && relationship != null
            && relationship.Trust >= requiredTrust;

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
            SubscribeToBossAttack();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToBossAttack();
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (player == null || relationship == null || playerShield == null || bossAttack == null)
            {
                ResolveReferences();
                SubscribeToBossAttack();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromBossAttack();
        }

        private void OnValidate()
        {
            requiredTrust = Mathf.Clamp(requiredTrust, 0, 100);
            shieldDuration = Mathf.Max(0.1f, shieldDuration);
            incomingDamageMultiplier = Mathf.Clamp01(incomingDamageMultiplier);
            cooldown = Mathf.Max(0.1f, cooldown);
            highTrustCooldownMultiplier = Mathf.Clamp(highTrustCooldownMultiplier, 0.1f, 1f);
        }

        public void Configure(
            Transform playerTarget,
            CompanionRelationship companionRelationship,
            PlayerBossSupportShield shield,
            int requiredTrust,
            float shieldDuration,
            float incomingDamageMultiplier,
            float cooldown)
        {
            player = playerTarget;
            relationship = companionRelationship;
            playerShield = shield;
            this.requiredTrust = Mathf.Clamp(requiredTrust, 0, 100);
            this.shieldDuration = Mathf.Max(0.1f, shieldDuration);
            this.incomingDamageMultiplier = Mathf.Clamp01(incomingDamageMultiplier);
            this.cooldown = Mathf.Max(0.1f, cooldown);
        }

        public void SetBossAttack(BossTelegraphedAttack2D newBossAttack)
        {
            if (bossAttack == newBossAttack)
            {
                return;
            }

            UnsubscribeFromBossAttack();
            bossAttack = newBossAttack;
            SubscribeToBossAttack();
        }

        public void Tick(float deltaTime)
        {
            if (cooldownTimer <= 0f)
            {
                return;
            }

            cooldownTimer = Mathf.Max(0f, cooldownTimer - Mathf.Max(0f, deltaTime));
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.Find("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (relationship == null)
            {
                relationship = FindAnyObjectByType<CompanionRelationship>();
            }

            if (playerShield == null && player != null)
            {
                playerShield = player.GetComponent<PlayerBossSupportShield>();
                if (playerShield == null)
                {
                    playerShield = player.gameObject.AddComponent<PlayerBossSupportShield>();
                }
            }

            if (bossAttack == null)
            {
                bossAttack = FindAnyObjectByType<BossTelegraphedAttack2D>();
            }
        }

        private void SubscribeToBossAttack()
        {
            if (bossAttack == null || subscribedBossAttack == bossAttack)
            {
                return;
            }

            UnsubscribeFromBossAttack();
            subscribedBossAttack = bossAttack;
            subscribedBossAttack.WarningStarted += HandleBossWarningStarted;
        }

        private void UnsubscribeFromBossAttack()
        {
            if (subscribedBossAttack == null)
            {
                return;
            }

            subscribedBossAttack.WarningStarted -= HandleBossWarningStarted;
            subscribedBossAttack = null;
        }

        private void HandleBossWarningStarted(BossTelegraphedAttack2D attack)
        {
            SupportPrompted?.Invoke(this);

            if (logSupportMessages)
            {
                Debug.Log("AI companion warned player about a Boss attack.", this);
            }

            if (!CanActivateSupport)
            {
                return;
            }

            playerShield.Activate(shieldDuration, incomingDamageMultiplier);
            cooldownTimer = GetCooldownForCurrentTrust();

            if (recordSupportMemory && relationship != null)
            {
                relationship.ApplyMemoryEvent("Boss Support", supportTrustDelta, supportAffectionDelta, RelationshipMemoryTag.Protected);
            }

            if (logSupportMessages)
            {
                Debug.Log($"AI companion activated Boss support shield. Trust={relationship.Trust}.", this);
            }

            SupportActivated?.Invoke(this);
        }

        private float GetCooldownForCurrentTrust()
        {
            float trustRatio = relationship != null ? relationship.Trust / 100f : 0f;
            float multiplier = Mathf.Lerp(1f, highTrustCooldownMultiplier, Mathf.Clamp01(trustRatio));
            return cooldown * multiplier;
        }
    }
}
