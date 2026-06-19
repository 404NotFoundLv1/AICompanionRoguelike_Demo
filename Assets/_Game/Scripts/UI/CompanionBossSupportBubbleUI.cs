using AICompanionRoguelike.Companion;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class CompanionBossSupportBubbleUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CompanionBossSupport support;
        [SerializeField] private Transform target;

        [Header("Layout")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField] private Vector2 bubbleSize = new Vector2(340f, 54f);
        [SerializeField, Min(0.1f)] private float visibleDuration = 2.6f;

        [Header("Colors")]
        [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.2f, 0.96f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.95f, 1f, 0.96f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.35f, 0.25f, 0.96f);

        private CompanionBossSupport subscribedSupport;
        private CompanionBossSupportFeedbackState currentState;
        private string currentMessage;
        private float visibleTimer;

        public bool IsVisible => visibleTimer > 0f && !string.IsNullOrEmpty(currentMessage);
        public string CurrentMessage => currentMessage;

        private void Reset()
        {
            support = GetComponent<CompanionBossSupport>();
            target = transform;
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToSupport();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToSupport();
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (support == null || target == null)
            {
                ResolveReferences();
                SubscribeToSupport();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromSupport();
        }

        private void OnValidate()
        {
            bubbleSize = new Vector2(Mathf.Max(160f, bubbleSize.x), Mathf.Max(36f, bubbleSize.y));
            visibleDuration = Mathf.Max(0.1f, visibleDuration);
        }

        public void Tick(float deltaTime)
        {
            if (visibleTimer <= 0f)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
        }

        private void ResolveReferences()
        {
            support = support != null ? support : GetComponent<CompanionBossSupport>();
            if (support == null)
            {
                support = FindAnyObjectByType<CompanionBossSupport>();
            }

            target = target != null ? target : transform;
        }

        private void SubscribeToSupport()
        {
            if (support == null || subscribedSupport == support)
            {
                return;
            }

            UnsubscribeFromSupport();
            subscribedSupport = support;
            subscribedSupport.SupportPrompted += HandleSupportPrompted;
            subscribedSupport.SupportFeedbackIssued += HandleSupportFeedbackIssued;
        }

        private void UnsubscribeFromSupport()
        {
            if (subscribedSupport == null)
            {
                return;
            }

            subscribedSupport.SupportPrompted -= HandleSupportPrompted;
            subscribedSupport.SupportFeedbackIssued -= HandleSupportFeedbackIssued;
            subscribedSupport = null;
        }

        private void HandleSupportPrompted(CompanionBossSupport bossSupport)
        {
            ShowFeedback(CompanionBossSupportFeedbackState.WarningOnly, "AI: Watch the warning area.");
        }

        private void HandleSupportFeedbackIssued(
            CompanionBossSupport bossSupport,
            CompanionBossSupportFeedbackState state,
            string message)
        {
            ShowFeedback(state, message);
        }

        private void ShowFeedback(CompanionBossSupportFeedbackState state, string message)
        {
            currentState = state;
            currentMessage = message;
            visibleTimer = visibleDuration;
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            Rect rect = GetBubbleRect();
            Color previousColor = GUI.color;
            GUI.color = GetStateColor(currentState);
            GUI.Box(rect, currentMessage);
            GUI.color = previousColor;
        }

        private Rect GetBubbleRect()
        {
            Camera camera = Camera.main;
            if (camera != null && target != null)
            {
                Vector3 screenPosition = camera.WorldToScreenPoint(target.position + worldOffset);
                if (screenPosition.z > 0f)
                {
                    return new Rect(
                        screenPosition.x - bubbleSize.x * 0.5f,
                        Screen.height - screenPosition.y - bubbleSize.y * 0.5f,
                        bubbleSize.x,
                        bubbleSize.y);
                }
            }

            return new Rect(
                (Screen.width - bubbleSize.x) * 0.5f,
                112f,
                bubbleSize.x,
                bubbleSize.y);
        }

        private Color GetStateColor(CompanionBossSupportFeedbackState state)
        {
            switch (state)
            {
                case CompanionBossSupportFeedbackState.Activated:
                    return activeColor;
                case CompanionBossSupportFeedbackState.Cooldown:
                case CompanionBossSupportFeedbackState.TrustTooLow:
                case CompanionBossSupportFeedbackState.MissingRelationship:
                case CompanionBossSupportFeedbackState.MissingShield:
                    return blockedColor;
                default:
                    return warningColor;
            }
        }
    }
}
