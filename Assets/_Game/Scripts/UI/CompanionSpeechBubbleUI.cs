using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class CompanionSpeechBubbleUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;

        [Header("Layout")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private Vector2 bubbleSize = new Vector2(340f, 72f);
        [SerializeField, Min(0.1f)] private float defaultVisibleDuration = 4f;
        [SerializeField, Min(0f)] private float screenMargin = 10f;

        [Header("Colors")]
        [SerializeField] private Color bubbleColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);
        [SerializeField] private Color textColor = Color.white;

        private string currentMessage;
        private float visibleTimer;
        private GUIStyle labelStyle;

        public bool IsVisible => visibleTimer > 0f && !string.IsNullOrEmpty(currentMessage);
        public string CurrentMessage => currentMessage;
        public int CurrentPriority { get; private set; }

        private void Reset()
        {
            target = transform;
        }

        private void Awake()
        {
            ResolveTarget();
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (target == null)
            {
                ResolveTarget();
            }
        }

        private void OnValidate()
        {
            bubbleSize = new Vector2(Mathf.Max(160f, bubbleSize.x), Mathf.Max(36f, bubbleSize.y));
            defaultVisibleDuration = Mathf.Max(0.1f, defaultVisibleDuration);
            screenMargin = Mathf.Max(0f, screenMargin);
        }

        public void ShowMessage(string message, float duration = -1f, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            currentMessage = message;
            CurrentPriority = Mathf.Max(0, priority);
            visibleTimer = duration > 0f ? duration : defaultVisibleDuration;
        }

        public void Tick(float deltaTime)
        {
            if (visibleTimer <= 0f)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
            if (visibleTimer <= 0f)
            {
                CurrentPriority = 0;
            }
        }

        public static Rect CalculateScreenRect(
            Vector2 screenPosition,
            Vector2 bubbleSize,
            float screenWidth,
            float screenHeight,
            float margin)
        {
            float safeMargin = Mathf.Max(0f, margin);
            float safeWidth = Mathf.Max(1f, screenWidth - safeMargin * 2f);
            float safeHeight = Mathf.Max(1f, screenHeight - safeMargin * 2f);
            float width = Mathf.Min(Mathf.Max(1f, bubbleSize.x), safeWidth);
            float height = Mathf.Min(Mathf.Max(1f, bubbleSize.y), safeHeight);
            float minX = safeMargin;
            float minY = safeMargin;
            float maxX = Mathf.Max(minX, screenWidth - safeMargin - width);
            float maxY = Mathf.Max(minY, screenHeight - safeMargin - height);
            float x = Mathf.Clamp(screenPosition.x - width * 0.5f, minX, maxX);
            float y = Mathf.Clamp(screenPosition.y - height * 0.5f, minY, maxY);
            return new Rect(x, y, width, height);
        }

        private void ResolveTarget()
        {
            target = target != null ? target : transform;
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            Rect rect = GetBubbleRect();
            Rect labelRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f);
            EnsureStyle();

            Color previousColor = GUI.color;
            GUI.color = bubbleColor;
            GUI.Box(rect, GUIContent.none);
            GUI.color = textColor;
            GUI.Label(labelRect, currentMessage, labelStyle);
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
                    Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
                    return CalculateScreenRect(guiPosition, bubbleSize, Screen.width, Screen.height, screenMargin);
                }
            }

            return CalculateScreenRect(
                new Vector2(Screen.width * 0.5f, 108f),
                bubbleSize,
                Screen.width,
                Screen.height,
                screenMargin);
        }

        private void EnsureStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
