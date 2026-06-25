using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.QTE;
using AICompanionRoguelike.Roguelike;
using AICompanionRoguelike.UI;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    [RequireComponent(typeof(CompanionRelationship))]
    [RequireComponent(typeof(CompanionSpeechBubbleUI))]
    public sealed class CompanionCombatDialogueController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CompanionRelationship relationship;
        [SerializeField] private CompanionSpeechBubbleUI speechBubble;
        [SerializeField] private QTEManager qteManager;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private RoomManager roomManager;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float lineDuration = 4f;
        [SerializeField, Min(0f)] private float globalCooldown = 2.5f;
        [SerializeField, Range(0.05f, 1f)] private float lowHealthThreshold = 0.35f;
        [SerializeField, Min(0.1f)] private float lowHealthRepeatCooldown = 7f;

        [Header("Debug")]
        [SerializeField] private bool logDialogue;

        private QTEManager subscribedQteManager;
        private HealthComponent subscribedPlayerHealth;
        private RoomManager subscribedRoomManager;
        private float nextAllowedDialogueTime;
        private float nextLowHealthDialogueTime;

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
            SubscribeToQteManager();
            SubscribeToPlayerHealth();
            SubscribeToRoomManager();
            CompanionRunFeedback.FeedbackRaised += HandleRunFeedbackRaised;
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToQteManager();
            SubscribeToPlayerHealth();
            SubscribeToRoomManager();
        }

        private void Update()
        {
            if (relationship == null || speechBubble == null || playerHealth == null || roomManager == null)
            {
                ResolveReferences();
            }

            SubscribeToQteManager();
            SubscribeToPlayerHealth();
            SubscribeToRoomManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromQteManager();
            UnsubscribeFromPlayerHealth();
            UnsubscribeFromRoomManager();
            CompanionRunFeedback.FeedbackRaised -= HandleRunFeedbackRaised;
        }

        private void OnValidate()
        {
            lineDuration = Mathf.Max(0.1f, lineDuration);
            globalCooldown = Mathf.Max(0f, globalCooldown);
            lowHealthThreshold = Mathf.Clamp(lowHealthThreshold, 0.05f, 1f);
            lowHealthRepeatCooldown = Mathf.Max(0.1f, lowHealthRepeatCooldown);
        }

        public bool TrySpeak(CompanionDialogueEventType eventType, int priority)
        {
            CompanionRelationshipProfileSnapshot profile = BuildProfile();
            string line = CompanionCombatDialogueLines.BuildLine(eventType, profile);
            return TryShowLine(line, priority);
        }

        public bool TryShowLine(string line, int priority)
        {
            ResolveReferences();
            if (speechBubble == null || string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            float now = Time.time;
            if (!CompanionDialoguePriorityGate.ShouldShow(
                    priority,
                    speechBubble.CurrentPriority,
                    speechBubble.IsVisible,
                    now,
                    nextAllowedDialogueTime))
            {
                return false;
            }

            speechBubble.ShowMessage(line, lineDuration, priority);
            nextAllowedDialogueTime = now + globalCooldown;

            if (logDialogue)
            {
                Debug.Log(line, this);
            }

            return true;
        }

        private CompanionRelationshipProfileSnapshot BuildProfile()
        {
            ResolveReferences();
            return relationship != null
                ? CompanionRelationshipProfile.Evaluate(
                    relationship.Trust,
                    relationship.Affection,
                    relationship.MemoryTags)
                : CompanionRelationshipProfile.Evaluate(
                    50,
                    50,
                    System.Array.Empty<RelationshipMemoryTagScore>());
        }

        private void ResolveReferences()
        {
            relationship = relationship != null ? relationship : GetComponent<CompanionRelationship>();
            speechBubble = speechBubble != null ? speechBubble : GetComponent<CompanionSpeechBubbleUI>();

            qteManager = qteManager != null ? qteManager : QTEManager.Instance;
            if (qteManager == null)
            {
                qteManager = FindAnyObjectByType<QTEManager>();
            }

            if (playerHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
            }

            roomManager = roomManager != null ? roomManager : FindAnyObjectByType<RoomManager>();
        }

        private void SubscribeToQteManager()
        {
            QTEManager manager = qteManager != null ? qteManager : QTEManager.Instance;
            if (manager == subscribedQteManager)
            {
                return;
            }

            UnsubscribeFromQteManager();
            if (manager == null)
            {
                return;
            }

            subscribedQteManager = manager;
            subscribedQteManager.QTEStarted += HandleQteStarted;
            subscribedQteManager.QTECompleted += HandleQteCompleted;
        }

        private void UnsubscribeFromQteManager()
        {
            if (subscribedQteManager == null)
            {
                return;
            }

            subscribedQteManager.QTEStarted -= HandleQteStarted;
            subscribedQteManager.QTECompleted -= HandleQteCompleted;
            subscribedQteManager = null;
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

        private void SubscribeToRoomManager()
        {
            if (roomManager == subscribedRoomManager)
            {
                return;
            }

            UnsubscribeFromRoomManager();
            if (roomManager == null)
            {
                return;
            }

            subscribedRoomManager = roomManager;
            subscribedRoomManager.RoomStarted += HandleRoomStarted;
            subscribedRoomManager.RoomCleared += HandleRoomCleared;
        }

        private void UnsubscribeFromRoomManager()
        {
            if (subscribedRoomManager == null)
            {
                return;
            }

            subscribedRoomManager.RoomStarted -= HandleRoomStarted;
            subscribedRoomManager.RoomCleared -= HandleRoomCleared;
            subscribedRoomManager = null;
        }

        private void HandleRunFeedbackRaised(CompanionRunFeedback source, string message)
        {
            if (source == null || source.gameObject != gameObject)
            {
                return;
            }

            TryShowLine(message, 1);
        }

        private void HandleQteStarted(QTEManager manager)
        {
            if (!IsCompanionQte(manager))
            {
                return;
            }

            TrySpeak(CompanionDialogueEventType.QTEStarted, 2);
        }

        private void HandleQteCompleted(QTEManager manager, QTEResultType resultType)
        {
            if (!IsCompanionQte(manager))
            {
                return;
            }

            switch (resultType)
            {
                case QTEResultType.Success:
                    TrySpeak(CompanionDialogueEventType.QTESuccess, 3);
                    break;
                case QTEResultType.WrongInput:
                    TrySpeak(CompanionDialogueEventType.QTEWrongInput, 3);
                    break;
                case QTEResultType.Ignored:
                    TrySpeak(CompanionDialogueEventType.QTEIgnored, 3);
                    break;
            }
        }

        private void HandlePlayerDamaged(HealthComponent health, DamageInfo damageInfo)
        {
            if (health == null || health != playerHealth || health.IsDead)
            {
                return;
            }

            float healthRatio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
            if (healthRatio <= lowHealthThreshold)
            {
                if (Time.time >= nextLowHealthDialogueTime && TrySpeak(CompanionDialogueEventType.PlayerLowHealth, 4))
                {
                    nextLowHealthDialogueTime = Time.time + lowHealthRepeatCooldown;
                }

                return;
            }

            TrySpeak(CompanionDialogueEventType.PlayerHit, 1);
        }

        private void HandleRoomStarted(RoomManager manager, RoomType roomType, int roomNumber)
        {
            if (roomType == RoomType.BranchEventRoom)
            {
                return;
            }

            TrySpeak(CompanionDialogueEventType.RoomStarted, 1);
        }

        private void HandleRoomCleared(RoomManager manager, RoomType roomType, int roomNumber)
        {
            if (roomType == RoomType.BranchEventRoom)
            {
                return;
            }

            TrySpeak(CompanionDialogueEventType.RoomCleared, 2);
        }

        private bool IsCompanionQte(QTEManager manager)
        {
            return manager != null && (manager.Requester == null || manager.Requester == gameObject);
        }
    }
}
