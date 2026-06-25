using System.Collections;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.QTE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Companion
{
    public sealed class CompanionQTERequester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QTEManager qteManager;
        [SerializeField] private CompanionRelationship relationship;

        [Header("Request")]
        [SerializeField, Min(0.1f)] private float qteDuration = 2f;
        [SerializeField, Min(0f)] private float requestCooldown = 3f;
        [SerializeField] private Key expectedKey = Key.E;
        [SerializeField] private string promptTemplate = "Press {0} to answer companion's finisher call on {1}";
        [SerializeField] private bool logResults = true;

        [Header("Success Effect")]
        [SerializeField, Min(0f)] private float successDamage = 999f;
        [SerializeField] private bool guaranteeFinisherKill = true;
        [SerializeField] private bool spawnSuccessEffect = true;
        [SerializeField] private Color successEffectColor = new Color(0.25f, 0.95f, 1f, 0.95f);
        [SerializeField, Min(0.05f)] private float successEffectDuration = 0.45f;
        [SerializeField] private bool flashParticipants = true;
        [SerializeField] private Color participantFlashColor = new Color(1f, 0.95f, 0.35f, 1f);

        private float cooldownTimer;
        private HealthComponent activeTarget;
        private QTEManager activeManager;

        public float BaseRequestCooldown => requestCooldown;
        public float EffectiveRequestCooldown
        {
            get
            {
                int trust = relationship != null ? relationship.Trust : 50;
                float relationshipMultiplier = CompanionRelationshipProfile.GetQteCooldownMultiplier(trust);
                float tendencyMultiplier = CompanionSkillTendencyRules.GetQteCooldownMultiplier(
                    CompanionRunBuildState.CurrentTendency);
                return Mathf.Max(0.1f, requestCooldown * relationshipMultiplier * tendencyMultiplier);
            }
        }

        private void Awake()
        {
            qteManager = qteManager != null ? qteManager : QTEManager.Instance;
            relationship = relationship != null ? relationship : GetComponent<CompanionRelationship>();
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

            cooldownTimer = EffectiveRequestCooldown;
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

            if (resultType == QTEResultType.Success)
            {
                ResolveSuccessFinisher();
            }

            UnsubscribeFromActiveManager();
            activeTarget = null;
        }

        private void ResolveSuccessFinisher()
        {
            if (activeTarget == null || activeTarget.IsDead)
            {
                return;
            }

            Vector3 requesterPosition = transform.position;
            Vector3 targetPosition = activeTarget.transform.position;

            if (spawnSuccessEffect)
            {
                QTEImpactEffect.Spawn(requesterPosition, targetPosition, successEffectColor, successEffectDuration);
            }

            if (flashParticipants)
            {
                FlashSpriteRenderers(gameObject);
                FlashSpriteRenderers(activeTarget.gameObject);
            }

            float damage = guaranteeFinisherKill ? Mathf.Max(successDamage, activeTarget.CurrentHealth) : successDamage;
            if (damage <= 0f)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            GameObject sourceObject = player != null ? player : gameObject;
            float previousHealth = activeTarget.CurrentHealth;
            activeTarget.TakeDamage(new DamageInfo(damage, DamageSourceType.Player, sourceObject));
            float appliedDamage = Mathf.Max(0f, previousHealth - activeTarget.CurrentHealth);

            if (logResults)
            {
                Debug.Log($"QTE finisher dealt {appliedDamage:0} damage to {activeTarget.name}.", activeTarget);
            }
        }

        private void FlashSpriteRenderers(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                StartCoroutine(FlashSpriteRenderers(renderers));
            }
        }

        private IEnumerator FlashSpriteRenderers(SpriteRenderer[] renderers)
        {
            Color[] originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                originalColors[i] = renderers[i].color;
                renderers[i].color = participantFlashColor;
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, successEffectDuration * 0.45f));

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = originalColors[i];
                }
            }
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
