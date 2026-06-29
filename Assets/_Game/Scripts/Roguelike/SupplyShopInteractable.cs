using AICompanionRoguelike.Character;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Roguelike
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SupplyShopInteractable : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;

        [Header("Interaction")]
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private string promptText = "Press E to open supply shop";
        [SerializeField] private Rect promptRect = new Rect(0f, 148f, 320f, 56f);

        [Header("Visual")]
        [SerializeField] private Color availableColor = new Color(0.15f, 0.95f, 0.78f, 0.95f);
        [SerializeField] private Color highlightedColor = new Color(0.85f, 1f, 0.35f, 1f);
        [SerializeField] private Color usedColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);

        private SpriteRenderer spriteRenderer;
        private Collider2D triggerCollider;
        private RoomInteractionVisualCue2D visualCue;
        private bool playerInRange;

        public bool IsPlayerInRange => playerInRange;
        public bool IsAvailable => runManager != null && runManager.CanOpenShopRewardDraft;
        public RoomInteractionVisualCue2D VisualCue => visualCue;

        private void Reset()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            ResolveRunManager();
            ResolveVisualCue();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            ResolveRunManager();
            RefreshVisualState();
        }

        private void Update()
        {
            ResolveRunManager();
            RefreshVisualState();

            if (!playerInRange || interactKey == Key.None)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[interactKey].wasPressedThisFrame)
            {
                Interact();
            }
        }

        public void Configure(RunManager manager)
        {
            runManager = manager;
            ResolveVisualCue();
            RefreshVisualState();
        }

        public bool Interact()
        {
            ResolveRunManager();
            return runManager != null && runManager.OpenShopRewardDraft();
        }

        private void ResolveRunManager()
        {
            if (runManager == null)
            {
                runManager = FindAnyObjectByType<RunManager>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerMovement2D>() != null)
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerMovement2D>() != null)
            {
                playerInRange = false;
            }
        }

        private void RefreshVisualState()
        {
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            triggerCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
            ResolveVisualCue();

            bool inShopRoom = runManager != null && runManager.CurrentRoomType == RoomType.ShopRoom;
            bool visible = inShopRoom;
            bool available = IsAvailable;
            bool highlighted = available
                && runManager != null
                && runManager.CurrentNextStepLabel.IndexOf("shop", System.StringComparison.OrdinalIgnoreCase) >= 0;
            visualCue.ApplyState(visible, available, highlighted);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
                spriteRenderer.color = visualCue.CurrentColor;
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = visible;
                triggerCollider.isTrigger = true;
            }

            if (!visible)
            {
                playerInRange = false;
            }
        }

        private void ResolveVisualCue()
        {
            if (visualCue == null)
            {
                visualCue = GetComponent<RoomInteractionVisualCue2D>();
                if (visualCue == null)
                {
                    visualCue = gameObject.AddComponent<RoomInteractionVisualCue2D>();
                }
            }

            visualCue.Configure("Shop", availableColor, highlightedColor, usedColor);
        }

        private void OnGUI()
        {
            if (!showPrompt || !playerInRange || !IsAvailable)
            {
                return;
            }

            float width = Mathf.Min(promptRect.width, Mathf.Max(180f, Screen.width - 16f));
            float x = promptRect.x <= 0f ? (Screen.width - width) * 0.5f : promptRect.x;
            Rect rect = new Rect(Mathf.Max(8f, x), promptRect.y, width, promptRect.height);
            GUI.Box(rect, promptText);
        }
    }
}
