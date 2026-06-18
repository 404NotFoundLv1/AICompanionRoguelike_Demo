using AICompanionRoguelike.Character;
using AICompanionRoguelike.Roguelike;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Home
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HomeExitPortal : MonoBehaviour
    {
        [SerializeField] private string battleScenePath = "Assets/Scenes/SampleScene.unity";
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private bool requireConfirmInput = true;
        [SerializeField] private bool showInteractionPrompt = true;
        [SerializeField] private string promptText = "按 E 开始探索";
        [SerializeField] private Color readyColor = new Color(0.25f, 1f, 0.75f, 1f);
        [SerializeField] private bool logTransition = true;

        private bool isTransitioning;
        private bool playerInRange;
        private SpriteRenderer portalRenderer;
        private Color idleColor;

        public bool IsPlayerInRange => playerInRange;

        private void Reset()
        {
            Collider2D portalCollider = GetComponent<Collider2D>();
            portalCollider.isTrigger = true;
        }

        private void Awake()
        {
            portalRenderer = GetComponent<SpriteRenderer>();
            idleColor = portalRenderer != null ? portalRenderer.color : Color.white;
        }

        private void Update()
        {
            if (!requireConfirmInput || !playerInRange || isTransitioning)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && interactKey != Key.None && keyboard[interactKey].wasPressedThisFrame)
            {
                EnterBattle();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isTransitioning || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerMovement2D>() == null)
            {
                return;
            }

            playerInRange = true;
            RefreshPortalFeedback();

            if (!requireConfirmInput)
            {
                EnterBattle();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null || other.GetComponentInParent<PlayerMovement2D>() == null)
            {
                return;
            }

            playerInRange = false;
            RefreshPortalFeedback();
        }

        public void EnterBattle()
        {
            if (isTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(battleScenePath))
            {
                Debug.LogWarning("HomeExitPortal cannot load battle scene because battleScenePath is empty.", this);
                return;
            }

            isTransitioning = true;
            RunSessionState.StartRunFromHome(battleScenePath);

            if (logTransition)
            {
                Debug.Log($"HomeExitPortal loading battle scene: {battleScenePath}", this);
            }

            SceneManager.LoadScene(battleScenePath, LoadSceneMode.Single);
        }

        private void RefreshPortalFeedback()
        {
            if (portalRenderer == null)
            {
                return;
            }

            portalRenderer.color = playerInRange ? readyColor : idleColor;
        }

        private void OnGUI()
        {
            if (!showInteractionPrompt || !requireConfirmInput || !playerInRange || isTransitioning)
            {
                return;
            }

            const float width = 260f;
            const float height = 54f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 120f, width, height);
            GUI.Box(rect, promptText);
        }
    }
}
