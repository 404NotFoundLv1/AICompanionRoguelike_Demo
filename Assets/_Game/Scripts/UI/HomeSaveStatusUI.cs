using AICompanionRoguelike.Save;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class HomeSaveStatusUI : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float visibleDuration = 2.5f;
        [SerializeField] private Rect statusRect = new Rect(0f, 18f, 240f, 38f);

        private string currentMessage = string.Empty;
        private float hideAtUnscaledTime;

        private void OnEnable()
        {
            SaveGameCoordinator.StatusChanged += HandleStatusChanged;

            if (!string.IsNullOrEmpty(SaveGameCoordinator.LastStatusMessage))
            {
                ShowStatus(SaveGameCoordinator.LastStatusMessage);
            }
        }

        private void OnDisable()
        {
            SaveGameCoordinator.StatusChanged -= HandleStatusChanged;
        }

        private void OnValidate()
        {
            visibleDuration = Mathf.Max(0.5f, visibleDuration);
            statusRect.width = Mathf.Max(180f, statusRect.width);
            statusRect.height = Mathf.Max(30f, statusRect.height);
        }

        private void HandleStatusChanged(string message)
        {
            ShowStatus(message);
        }

        private void ShowStatus(string message)
        {
            currentMessage = message ?? string.Empty;
            hideAtUnscaledTime = Time.unscaledTime + visibleDuration;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(currentMessage) || Time.unscaledTime >= hideAtUnscaledTime)
            {
                return;
            }

            float x = statusRect.x > 0f
                ? statusRect.x
                : Mathf.Max(8f, Screen.width - statusRect.width - 18f);
            Rect rect = new Rect(x, Mathf.Max(8f, statusRect.y), statusRect.width, statusRect.height);
            GUI.Box(rect, currentMessage);
        }
    }
}
