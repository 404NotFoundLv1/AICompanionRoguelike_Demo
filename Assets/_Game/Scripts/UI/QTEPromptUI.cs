using AICompanionRoguelike.QTE;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class QTEPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QTEManager qteManager;

        [Header("Layout")]
        [SerializeField] private Rect promptRect = new Rect(0f, 78f, 460f, 132f);
        [SerializeField] private Rect resultRect = new Rect(0f, 226f, 420f, 58f);
        [SerializeField] private bool centerHorizontally = true;
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private bool showResult = true;
        [SerializeField, Min(0.1f)] private float resultDuration = 1.4f;

        [Header("Colors")]
        [SerializeField] private Color safeTimeColor = new Color(0.25f, 0.95f, 1f, 1f);
        [SerializeField] private Color lowTimeColor = new Color(1f, 0.32f, 0.18f, 1f);
        [SerializeField] private Color successColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color failureColor = new Color(1f, 0.32f, 0.22f, 1f);
        [SerializeField] private Color ignoredColor = new Color(1f, 0.78f, 0.28f, 1f);

        private QTEManager subscribedManager;
        private string resultMessage;
        private Color resultColor;
        private float resultTimer;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle keyStyle;

        private void OnEnable()
        {
            ResolveManager();
            SubscribeToManager();
        }

        private void Start()
        {
            ResolveManager();
            SubscribeToManager();
        }

        private void Update()
        {
            if (resultTimer > 0f)
            {
                resultTimer = Mathf.Max(0f, resultTimer - Time.deltaTime);
            }

            if (qteManager == null)
            {
                ResolveManager();
                SubscribeToManager();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromManager();
        }

        private void ResolveManager()
        {
            if (qteManager == null)
            {
                qteManager = QTEManager.Instance;
            }
        }

        private void SubscribeToManager()
        {
            if (qteManager == null || qteManager == subscribedManager)
            {
                return;
            }

            UnsubscribeFromManager();
            subscribedManager = qteManager;
            subscribedManager.QTEStarted += HandleQTEStarted;
            subscribedManager.QTECompleted += HandleQTECompleted;
        }

        private void UnsubscribeFromManager()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.QTEStarted -= HandleQTEStarted;
            subscribedManager.QTECompleted -= HandleQTECompleted;
            subscribedManager = null;
        }

        private void HandleQTEStarted(QTEManager manager)
        {
            resultTimer = 0f;
            resultMessage = null;
        }

        private void HandleQTECompleted(QTEManager manager, QTEResultType resultType)
        {
            if (!showResult)
            {
                return;
            }

            switch (resultType)
            {
                case QTEResultType.Success:
                    resultMessage = "连携成功！玩家完成终结。";
                    resultColor = successColor;
                    break;
                case QTEResultType.WrongInput:
                    resultMessage = "连携失败：输入错误。";
                    resultColor = failureColor;
                    break;
                case QTEResultType.Ignored:
                    resultMessage = "连携错过：邀请已消失。";
                    resultColor = ignoredColor;
                    break;
                default:
                    resultMessage = resultType.ToString();
                    resultColor = Color.white;
                    break;
            }

            resultTimer = resultDuration;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (showPrompt && qteManager != null && qteManager.IsActive)
            {
                DrawPrompt();
            }

            if (showResult && resultTimer > 0f && !string.IsNullOrEmpty(resultMessage))
            {
                DrawResult();
            }
        }

        private void DrawPrompt()
        {
            Rect rect = ResolveRect(promptRect);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("AI 队友请求连携", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(qteManager.ActivePrompt, bodyStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"按键：{qteManager.ExpectedKey}    目标：{GetTargetName(qteManager)}", keyStyle);
            GUILayout.Space(8f);
            DrawTimeBar(qteManager);
            GUILayout.EndArea();
        }

        private void DrawResult()
        {
            Rect rect = ResolveRect(resultRect);
            Color previousColor = GUI.color;
            GUI.color = resultColor;
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(resultMessage, keyStyle);
            GUILayout.EndArea();
            GUI.color = previousColor;
        }

        private void DrawTimeBar(QTEManager manager)
        {
            Rect barRect = GUILayoutUtility.GetRect(1f, 14f, GUILayout.ExpandWidth(true));
            GUI.Box(barRect, GUIContent.none);

            float ratio = manager.ActiveDuration > 0f
                ? Mathf.Clamp01(manager.TimeRemaining / manager.ActiveDuration)
                : 0f;
            Rect fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, Mathf.Max(0f, (barRect.width - 4f) * ratio), barRect.height - 4f);

            Color previousColor = GUI.color;
            GUI.color = Color.Lerp(lowTimeColor, safeTimeColor, ratio);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private Rect ResolveRect(Rect rect)
        {
            float width = Mathf.Min(rect.width, Mathf.Max(120f, Screen.width - 16f));
            float x = centerHorizontally ? (Screen.width - width) * 0.5f : rect.x;
            return new Rect(Mathf.Max(8f, x), rect.y, width, rect.height);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true
            };

            keyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        private static string GetTargetName(QTEManager manager)
        {
            return manager.Target != null ? manager.Target.name : "--";
        }
    }
}
