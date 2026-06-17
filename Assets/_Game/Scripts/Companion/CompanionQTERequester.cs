using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.QTE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Companion
{
    public sealed class CompanionQTERequester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QTEManager qteManager;

        [Header("Request")]
        [SerializeField, Min(0.1f)] private float qteDuration = 2f;
        [SerializeField, Min(0f)] private float requestCooldown = 3f;
        [SerializeField] private Key expectedKey = Key.E;
        [SerializeField] private string promptTemplate = "Press {0} to answer companion's finisher call on {1}";
        [SerializeField] private bool logResults = true;

        private float cooldownTimer;
        private HealthComponent activeTarget;
        private QTEManager activeManager;

        private void Awake()
        {
            qteManager = qteManager != null ? qteManager : QTEManager.Instance;
        }

        private void OnEnable()
        {
            CompanionKillProtection.FinisherRequested += HandleFinisherRequested;
        }

        private void OnDisable()
        {
            CompanionKillProtection.FinisherRequested -= HandleFinisherRequested;
            UnsubscribeFromActiveManager();
            activeTarget = null;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
        }

        private void HandleFinisherRequested(CompanionKillProtection protection, HealthComponent targetHealth, DamageInfo damageInfo)
        {
            if (damageInfo.sourceObject != null && damageInfo.sourceObject != gameObject)
            {
                return;
            }

            if (cooldownTimer > 0f || targetHealth == null || targetHealth.IsDead)
            {
                return;
            }

            QTEManager manager = qteManager != null ? qteManager : QTEManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("Companion tried to request a QTE, but no QTEManager exists in the scene.", this);
                return;
            }

            string prompt = string.Format(promptTemplate, expectedKey, targetHealth.name);
            bool started = manager.TryStartQTE(prompt, qteDuration, expectedKey, gameObject, targetHealth.gameObject);
            if (!started)
            {
                return;
            }

            cooldownTimer = requestCooldown;
            activeTarget = targetHealth;
            activeManager = manager;
            activeManager.QTECompleted += HandleQTECompleted;
        }

        private void HandleQTECompleted(QTEManager manager, QTEResultType resultType)
        {
            if (manager != activeManager)
            {
                return;
            }

            if (logResults)
            {
                string targetName = activeTarget != null ? activeTarget.name : "missing target";
                Debug.Log($"Companion QTE result: {resultType} for {targetName}", this);
            }

            UnsubscribeFromActiveManager();
            activeTarget = null;
        }

        private void UnsubscribeFromActiveManager()
        {
            if (activeManager != null)
            {
                activeManager.QTECompleted -= HandleQTECompleted;
                activeManager = null;
            }
        }
    }
}
