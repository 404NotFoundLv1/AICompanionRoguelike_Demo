using System.Collections.Generic;
using AICompanionRoguelike.Character;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Roguelike
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class NextRoomChoicePortal : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;

        [Header("Interaction")]
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private bool showInteractionPrompt = true;
        [SerializeField] private string promptText = "Press E to choose route";
        [SerializeField] private string choiceTitle = "Choose next route";

        [Header("Visual")]
        [SerializeField] private Vector3 portalPosition = new Vector3(6.45f, -1.15f, -0.1f);
        [SerializeField] private Color idleColor = new Color(0.15f, 0.8f, 1f, 0.85f);
        [SerializeField] private Color readyColor = new Color(0.35f, 1f, 0.6f, 0.95f);

        private readonly List<RoomType> offeredChoices = new List<RoomType>(4);
        private readonly List<RoomChoicePreview> offeredPreviews = new List<RoomChoicePreview>(4);
        private readonly List<RouteMapNode> offeredRouteMapNodes = new List<RouteMapNode>(8);
        private SpriteRenderer portalRenderer;
        private Collider2D portalCollider;
        private bool isVisible;
        private bool playerInRange;
        private bool isChoiceOpen;

        public bool IsVisible => isVisible;
        public bool IsPlayerInRange => playerInRange;
        public bool IsChoiceOpen => isChoiceOpen;
        public IReadOnlyList<RoomType> OfferedChoices => offeredChoices;
        public IReadOnlyList<RoomChoicePreview> OfferedPreviews => offeredPreviews;
        public IReadOnlyList<RouteMapNode> OfferedRouteMapNodes => offeredRouteMapNodes;

        private void Reset()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            collider2D.isTrigger = true;
        }

        private void Awake()
        {
            portalRenderer = GetComponent<SpriteRenderer>();
            portalCollider = GetComponent<Collider2D>();
            transform.position = portalPosition;
            SetVisible(false);
        }

        private void OnEnable()
        {
            ResolveRunManager();
            Subscribe();
        }

        private void Start()
        {
            ResolveRunManager();
            Subscribe();
        }

        private void Update()
        {
            if (!isVisible || !playerInRange)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (isChoiceOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    isChoiceOpen = false;
                }

                TrySelectChoiceByKeyboard(keyboard);
                return;
            }

            if (interactKey != Key.None && keyboard[interactKey].wasPressedThisFrame)
            {
                isChoiceOpen = offeredChoices.Count > 0;
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isVisible || other == null || other.GetComponentInParent<PlayerMovement2D>() == null)
            {
                return;
            }

            playerInRange = true;
            RefreshColor();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null || other.GetComponentInParent<PlayerMovement2D>() == null)
            {
                return;
            }

            playerInRange = false;
            isChoiceOpen = false;
            RefreshColor();
        }

        public void SelectChoice(int index)
        {
            if (runManager == null || index < 0 || index >= offeredChoices.Count)
            {
                return;
            }

            SetVisible(false);
            runManager.AdvanceToSelectedRoom(index);
        }

        private void ResolveRunManager()
        {
            if (runManager == null)
            {
                runManager = FindAnyObjectByType<RunManager>();
            }
        }

        private void Subscribe()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.RoomChoicesPrepared -= HandleRoomChoicesPrepared;
            runManager.RoomChoicesCleared -= HandleRoomChoicesCleared;
            runManager.RunStarted -= HandleRunStarted;
            runManager.RoomAdvanced -= HandleRoomAdvanced;

            runManager.RoomChoicesPrepared += HandleRoomChoicesPrepared;
            runManager.RoomChoicesCleared += HandleRoomChoicesCleared;
            runManager.RunStarted += HandleRunStarted;
            runManager.RoomAdvanced += HandleRoomAdvanced;
        }

        private void Unsubscribe()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.RoomChoicesPrepared -= HandleRoomChoicesPrepared;
            runManager.RoomChoicesCleared -= HandleRoomChoicesCleared;
            runManager.RunStarted -= HandleRunStarted;
            runManager.RoomAdvanced -= HandleRoomAdvanced;
        }

        private void HandleRunStarted(RunManager manager)
        {
            SetVisible(false);
        }

        private void HandleRoomAdvanced(RunManager manager, RoomType roomType, int roomNumber)
        {
            if (manager == null || !manager.IsWaitingForNextRoom)
            {
                SetVisible(false);
            }
        }

        private void HandleRoomChoicesPrepared(RunManager manager, IReadOnlyList<RoomType> choices)
        {
            offeredChoices.Clear();
            offeredPreviews.Clear();
            offeredRouteMapNodes.Clear();
            IReadOnlyList<RoomChoicePreview> previews = manager != null ? manager.CurrentRoomChoicePreviews : null;
            IReadOnlyList<RouteMapNode> routeMapNodes = manager != null ? manager.CurrentRouteMapNodes : null;

            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] == RoomType.BranchEventRoom)
                {
                    continue;
                }

                offeredChoices.Add(choices[i]);
                offeredPreviews.Add(GetPreviewForChoice(previews, choices[i], i));
            }

            CopyRouteMapNodes(routeMapNodes);
            transform.position = portalPosition;
            SetVisible(offeredChoices.Count > 0);
        }

        private void HandleRoomChoicesCleared(RunManager manager)
        {
            offeredChoices.Clear();
            offeredPreviews.Clear();
            offeredRouteMapNodes.Clear();
            SetVisible(false);
        }

        private void CopyRouteMapNodes(IReadOnlyList<RouteMapNode> routeMapNodes)
        {
            if (routeMapNodes == null)
            {
                return;
            }

            for (int i = 0; i < routeMapNodes.Count; i++)
            {
                if (routeMapNodes[i].RoomType == RoomType.BranchEventRoom)
                {
                    continue;
                }

                offeredRouteMapNodes.Add(routeMapNodes[i]);
            }
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            playerInRange = false;
            isChoiceOpen = false;

            if (portalRenderer != null)
            {
                portalRenderer.enabled = visible;
            }

            if (portalCollider != null)
            {
                portalCollider.enabled = visible;
                portalCollider.isTrigger = true;
            }

            RefreshColor();
        }

        private void RefreshColor()
        {
            if (portalRenderer == null)
            {
                return;
            }

            portalRenderer.color = playerInRange ? readyColor : idleColor;
        }

        private void TrySelectChoiceByKeyboard(Keyboard keyboard)
        {
            for (int i = 0; i < offeredChoices.Count && i < 9; i++)
            {
                if (WasDigitPressed(keyboard, i + 1))
                {
                    SelectChoice(i);
                    return;
                }
            }
        }

        private static bool WasDigitPressed(Keyboard keyboard, int digit)
        {
            return digit switch
            {
                1 => keyboard.digit1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame,
                4 => keyboard.digit4Key.wasPressedThisFrame,
                5 => keyboard.digit5Key.wasPressedThisFrame,
                6 => keyboard.digit6Key.wasPressedThisFrame,
                7 => keyboard.digit7Key.wasPressedThisFrame,
                8 => keyboard.digit8Key.wasPressedThisFrame,
                9 => keyboard.digit9Key.wasPressedThisFrame,
                _ => false
            };
        }

        private void OnGUI()
        {
            if (!isVisible || !playerInRange)
            {
                return;
            }

            if (isChoiceOpen)
            {
                DrawChoicePanel();
                return;
            }

            if (showInteractionPrompt)
            {
                DrawPrompt();
            }
        }

        private void DrawPrompt()
        {
            const float width = 260f;
            const float height = 54f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 120f, width, height);
            GUI.Box(rect, promptText);
        }

        private void DrawChoicePanel()
        {
            const float width = 540f;
            float height = Mathf.Min(Screen.height - 24f, Mathf.Min(620f, 196f + GetChoicePanelContentHeight()));
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(choiceTitle);
            DrawRouteMap();
            GUILayout.Space(6f);
            DrawRouteProgress();
            GUILayout.Space(8f);

            for (int i = 0; i < offeredChoices.Count; i++)
            {
                RoomChoicePreview preview = i < offeredPreviews.Count
                    ? offeredPreviews[i]
                    : CreateFallbackPreview(offeredChoices[i]);
                string label = $"[{i + 1}] {preview.Title}";
                if (GUILayout.Button(label))
                {
                    SelectChoice(i);
                }

                GUILayout.Label(preview.ThreatPreview);
                GUILayout.Label(preview.RewardPreview);
                GUILayout.Label(preview.RouteNote);
                if (preview.HasModifier)
                {
                    GUILayout.Label(preview.ModifierRiskPreview);
                    GUILayout.Label(preview.ModifierRewardPreview);
                    GUILayout.Label(preview.ModifierRouteNote);
                }

                GUILayout.Space(6f);
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("[Esc] Close"))
            {
                isChoiceOpen = false;
            }

            GUILayout.EndArea();
        }

        private float GetChoicePanelContentHeight()
        {
            float height = 0f;
            for (int i = 0; i < offeredChoices.Count; i++)
            {
                RoomChoicePreview preview = i < offeredPreviews.Count
                    ? offeredPreviews[i]
                    : CreateFallbackPreview(offeredChoices[i]);
                height += preview.HasModifier ? 126f : 82f;
            }

            return height;
        }

        private void DrawRouteMap()
        {
            if (offeredRouteMapNodes.Count == 0)
            {
                return;
            }

            GUILayout.Label("Route map");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < offeredRouteMapNodes.Count; i++)
            {
                if (i > 0)
                {
                    GUILayout.Label(">", GUILayout.Width(14f));
                }

                RouteMapNode node = offeredRouteMapNodes[i];
                Color previousBackground = GUI.backgroundColor;
                GUI.backgroundColor = GetRouteNodeColor(node);
                GUILayout.Box(BuildRouteNodeText(node), GUILayout.MinWidth(76f), GUILayout.Height(44f));
                GUI.backgroundColor = previousBackground;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawRouteProgress()
        {
            if (runManager == null)
            {
                return;
            }

            GUILayout.Label(runManager.CurrentRouteProgressLabel);
            GUILayout.Label(runManager.CurrentRoutePathLabel);
            GUILayout.Label(runManager.CurrentRouteMapLabel);
        }

        private static string BuildRouteNodeText(RouteMapNode node)
        {
            string prefix = node.IsNextChoice
                ? $"[{node.ChoiceIndex + 1}]"
                : $"{node.StepNumber}.";
            string label = node.HasModifier
                ? $"{node.Label}+{node.ModifierLabel}"
                : node.Label;
            string state = node.IsCurrent
                ? "Here"
                : node.IsNextChoice
                    ? "Next"
                    : node.IsBossEndpoint
                        ? "Goal"
                        : node.IsCompleted
                            ? "Done"
                            : string.Empty;

            return string.IsNullOrWhiteSpace(state)
                ? $"{prefix} {label}"
                : $"{prefix} {label}\n{state}";
        }

        private static Color GetRouteNodeColor(RouteMapNode node)
        {
            if (node.IsCurrent)
            {
                return new Color(0.65f, 0.86f, 1f, 1f);
            }

            if (node.IsBossEndpoint)
            {
                return new Color(1f, 0.42f, 0.38f, 1f);
            }

            switch (node.RoomType)
            {
                case RoomType.EliteRoom:
                    return new Color(1f, 0.82f, 0.34f, 1f);
                case RoomType.SafeRoom:
                    return new Color(0.45f, 0.9f, 0.55f, 1f);
                case RoomType.ShopRoom:
                    return new Color(0.52f, 0.9f, 0.95f, 1f);
                default:
                    return new Color(0.86f, 0.86f, 0.86f, 1f);
            }
        }

        private static RoomChoicePreview GetPreviewForChoice(
            IReadOnlyList<RoomChoicePreview> previews,
            RoomType roomType,
            int index)
        {
            if (previews != null && index >= 0 && index < previews.Count && previews[index].RoomType == roomType)
            {
                return previews[index];
            }

            if (previews != null)
            {
                for (int i = 0; i < previews.Count; i++)
                {
                    if (previews[i].RoomType == roomType)
                    {
                        return previews[i];
                    }
                }
            }

            return CreateFallbackPreview(roomType);
        }

        private static RoomChoicePreview CreateFallbackPreview(RoomType roomType)
        {
            string title = roomType switch
            {
                RoomType.BattleRoom => "Battle Room",
                RoomType.EliteRoom => "Elite Room",
                RoomType.SafeRoom => "Safe Room",
                RoomType.ShopRoom => "Supply Room",
                RoomType.BossRoom => "Boss Room",
                _ => roomType.ToString()
            };

            return new RoomChoicePreview(
                roomType,
                title,
                "Threat: unknown.",
                "Reward: unknown.",
                "Route: scout ahead.");
        }
    }
}
