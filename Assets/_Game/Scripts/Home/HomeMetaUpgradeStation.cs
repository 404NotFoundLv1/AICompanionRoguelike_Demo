using AICompanionRoguelike.Character;
using AICompanionRoguelike.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Home
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HomeMetaUpgradeStation : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Key playerMaxHealthKey = Key.Digit1;
        [SerializeField] private Key playerDamageKey = Key.Digit2;
        [SerializeField] private Key companionCooldownKey = Key.Digit3;

        [Header("UI")]
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private Rect promptRect = new Rect(16f, 72f, 460f, 170f);
        [SerializeField, Min(0.5f)] private float feedbackDuration = 2.5f;

        private bool playerInRange;
        private string lastFeedback = string.Empty;
        private float feedbackHideAtUnscaledTime;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void Update()
        {
            if (!playerInRange || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[playerMaxHealthKey].wasPressedThisFrame)
            {
                TryPurchase(MetaUpgradeType.PlayerMaxHealth);
            }
            else if (Keyboard.current[playerDamageKey].wasPressedThisFrame)
            {
                TryPurchase(MetaUpgradeType.PlayerDamage);
            }
            else if (Keyboard.current[companionCooldownKey].wasPressedThisFrame)
            {
                TryPurchase(MetaUpgradeType.CompanionCooldown);
            }
        }

        public bool TryPurchase(MetaUpgradeType upgradeType)
        {
            bool purchased = MetaProgressionState.TryPurchaseUpgrade(upgradeType);
            lastFeedback = BuildPurchaseFeedback(upgradeType, purchased);
            feedbackHideAtUnscaledTime = Time.unscaledTime + feedbackDuration;
            Debug.Log(lastFeedback, this);
            return purchased;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                playerInRange = false;
            }
        }

        private void OnGUI()
        {
            if (!showPrompt || !playerInRange)
            {
                return;
            }

            GUILayout.BeginArea(GetClampedPromptRect(promptRect, Screen.width, Screen.height), GUI.skin.box);
            GUILayout.Label("Home Upgrade Station");
            GUILayout.Label($"Core Fragments: {MetaProgressionState.CoreFragments}");
            DrawUpgradeLine(playerMaxHealthKey, MetaUpgradeType.PlayerMaxHealth, "+10 Max HP next run");
            DrawUpgradeLine(playerDamageKey, MetaUpgradeType.PlayerDamage, "+8% damage next run");
            DrawUpgradeLine(companionCooldownKey, MetaUpgradeType.CompanionCooldown, "-5% AI cooldown next run");
            if (!string.IsNullOrEmpty(lastFeedback) && Time.unscaledTime < feedbackHideAtUnscaledTime)
            {
                GUILayout.Space(6f);
                GUILayout.Label(lastFeedback);
            }

            GUILayout.EndArea();
        }

        public static Rect GetClampedPromptRect(Rect desiredRect, float screenWidth, float screenHeight)
        {
            const float margin = 8f;
            float availableWidth = Mathf.Max(1f, screenWidth - margin * 2f);
            float availableHeight = Mathf.Max(1f, screenHeight - margin * 2f);
            float width = Mathf.Min(Mathf.Max(1f, desiredRect.width), availableWidth);
            float height = Mathf.Min(Mathf.Max(1f, desiredRect.height), availableHeight);
            float maxX = Mathf.Max(margin, screenWidth - width - margin);
            float maxY = Mathf.Max(margin, screenHeight - height - margin);
            float x = Mathf.Clamp(desiredRect.x, margin, maxX);
            float y = Mathf.Clamp(desiredRect.y, margin, maxY);
            return new Rect(x, y, width, height);
        }

        private static void DrawUpgradeLine(Key key, MetaUpgradeType type, string effectText)
        {
            int level = MetaProgressionState.GetUpgradeLevel(type);
            int cost = MetaProgressionState.GetUpgradeCost(type);
            GUILayout.Label(
                $"[{FormatKey(key)}] {MetaProgressionState.GetUpgradeDisplayName(type)} Lv{level} | Cost {cost} | {effectText}");
        }

        public static string BuildPurchaseFeedback(MetaUpgradeType type, bool purchased)
        {
            string displayName = MetaProgressionState.GetUpgradeDisplayName(type);
            if (purchased)
            {
                return $"Purchased {displayName} Lv{MetaProgressionState.GetUpgradeLevel(type)}. Core Fragments: {MetaProgressionState.CoreFragments}.";
            }

            return $"Need {MetaProgressionState.GetUpgradeCost(type)} Core Fragments for {displayName}. Current: {MetaProgressionState.CoreFragments}.";
        }

        private static string FormatKey(Key key)
        {
            switch (key)
            {
                case Key.Digit1:
                    return "1";
                case Key.Digit2:
                    return "2";
                case Key.Digit3:
                    return "3";
                default:
                    return key.ToString();
            }
        }

        private static bool IsPlayer(Collider2D other)
        {
            return other != null && other.GetComponentInParent<PlayerMovement2D>() != null;
        }
    }
}
