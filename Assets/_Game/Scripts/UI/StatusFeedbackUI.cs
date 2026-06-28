using System.Text;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.UI
{
    public sealed class StatusFeedbackUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private PlayerBranchChoiceBuff branchChoiceBuff;
        [SerializeField] private PlayerCounterplayFeedback playerCounterplayFeedback;
        [SerializeField] private CompanionRelationship companionRelationship;
        [SerializeField] private CompanionTacticalSupport tacticalSupport;
        [SerializeField] private BranchEventRoomController branchEventRoomController;
        [SerializeField] private RunManager runManager;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 330f, 210f);
        [SerializeField] private float toastDuration = 3f;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool showToast = true;

        private readonly StringBuilder memoryBuilder = new StringBuilder(96);
        private BranchEventRoomController subscribedBranchEventRoomController;
        private string toastMessage;
        private float toastTimer;
        private GUIStyle toastLabelStyle;

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToBranchEventRoom();
            CompanionRelationship.AnyRelationshipChanged += HandleAnyRelationshipChanged;
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToBranchEventRoom();
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer = Mathf.Max(0f, toastTimer - Time.deltaTime);
            }

            if (playerHealth == null
                || companionRelationship == null
                || branchEventRoomController == null
                || runManager == null
                || (playerCounterplayFeedback == null && playerHealth != null))
            {
                ResolveReferences();
                SubscribeToBranchEventRoom();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromBranchEventRoom();
            CompanionRelationship.AnyRelationshipChanged -= HandleAnyRelationshipChanged;
        }

        private void ResolveReferences()
        {
            if (playerHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
                branchChoiceBuff = player != null ? player.GetComponent<PlayerBranchChoiceBuff>() : branchChoiceBuff;
                playerCounterplayFeedback = player != null ? player.GetComponent<PlayerCounterplayFeedback>() : playerCounterplayFeedback;
            }

            if (branchChoiceBuff == null && playerHealth != null)
            {
                branchChoiceBuff = playerHealth.GetComponent<PlayerBranchChoiceBuff>();
            }

            if (playerCounterplayFeedback == null && playerHealth != null)
            {
                playerCounterplayFeedback = playerHealth.GetComponent<PlayerCounterplayFeedback>();
            }

            if (companionRelationship == null)
            {
                companionRelationship = FindAnyObjectByType<CompanionRelationship>();
            }

            if (tacticalSupport == null)
            {
                tacticalSupport = FindAnyObjectByType<CompanionTacticalSupport>();
            }

            if (branchEventRoomController == null)
            {
                branchEventRoomController = FindAnyObjectByType<BranchEventRoomController>();
            }

            if (runManager == null)
            {
                runManager = FindAnyObjectByType<RunManager>();
            }
        }

        private void SubscribeToBranchEventRoom()
        {
            if (branchEventRoomController == subscribedBranchEventRoomController)
            {
                return;
            }

            UnsubscribeFromBranchEventRoom();

            if (branchEventRoomController == null)
            {
                return;
            }

            subscribedBranchEventRoomController = branchEventRoomController;
            subscribedBranchEventRoomController.ChoiceSelected += HandleBranchChoiceSelected;
        }

        private void UnsubscribeFromBranchEventRoom()
        {
            if (subscribedBranchEventRoomController == null)
            {
                return;
            }

            subscribedBranchEventRoomController.ChoiceSelected -= HandleBranchChoiceSelected;
            subscribedBranchEventRoomController = null;
        }

        private void HandleBranchChoiceSelected(BranchEventRoomController controller, BranchEventChoice choice)
        {
            if (!showToast || controller == null)
            {
                return;
            }

            string outcome = controller.LastOutcomeDescription;
            ShowToast(string.IsNullOrEmpty(outcome) ? $"Branch choice selected: {choice}" : outcome);
        }

        private void HandleAnyRelationshipChanged(CompanionRelationship relationship, RelationshipChange change)
        {
            if (!showToast || relationship != companionRelationship)
            {
                return;
            }

            ShowToast(
                $"{change.sourceLabel}: Trust {change.previousTrust}->{change.currentTrust} ({change.trustDelta:+#;-#;0}), Affection {change.previousAffection}->{change.currentAffection} ({change.affectionDelta:+#;-#;0}), Memory {change.memoryTag}");
        }

        private void ShowToast(string message)
        {
            toastMessage = message;
            toastTimer = toastDuration;
        }

        private void OnGUI()
        {
            if (showPanel)
            {
                DrawStatusPanel();
            }

            if (showToast && toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
            {
                DrawToast();
            }
        }

        private void DrawStatusPanel()
        {
            Rect effectivePanelRect = panelRect;
            effectivePanelRect.height = Mathf.Max(effectivePanelRect.height, 300f);
            GUILayout.BeginArea(effectivePanelRect, GUI.skin.box);
            GUILayout.Label("Run Status");
            GUILayout.Space(4f);
            GUILayout.Label(BuildPlayerHealthLine());
            GUILayout.Label(BuildChallengeBuffLine());
            GUILayout.Label(BuildCounterplayLine());
            GUILayout.Label(BuildRunGrowthRouteLine());
            GUILayout.Label(BuildRunGrowthLine());
            GUILayout.Space(4f);
            GUILayout.Label(BuildRelationshipLine());
            GUILayout.Label(BuildMemoryLine());
            GUILayout.Label(BuildCompanionBuildLine());
            GUILayout.Label(BuildCompanionTacticPlanLine());
            GUILayout.Label(BuildTacticalSupportLine());

            if (branchEventRoomController != null && branchEventRoomController.IsLoadingBranchScene)
            {
                GUILayout.Space(4f);
                GUILayout.Label("Branch Room: loading additive scene");
            }
            else if (branchEventRoomController != null && branchEventRoomController.IsWaitingForChoice)
            {
                GUILayout.Space(4f);
                GUILayout.Label(branchEventRoomController.BranchSceneIsLoaded
                    ? "Branch Room: additive scene loaded, combat room frozen"
                    : "Branch Room: fallback position, combat room frozen");
            }

            GUILayout.EndArea();
        }

        private string BuildPlayerHealthLine()
        {
            if (playerHealth == null)
            {
                return "Player HP: --";
            }

            return $"Player HP: {playerHealth.CurrentHealth:0}/{playerHealth.MaxHealth:0}";
        }

        private string BuildChallengeBuffLine()
        {
            if (branchChoiceBuff == null || !branchChoiceBuff.IsActive)
            {
                return "Challenge Buff: none";
            }

            return $"Challenge Buff: {branchChoiceBuff.RemainingDuration:0.0}s | ATK x{branchChoiceBuff.OutgoingDamageMultiplier:0.##} | DMG x{branchChoiceBuff.IncomingDamageMultiplier:0.##}";
        }

        private string BuildCounterplayLine()
        {
            return playerCounterplayFeedback != null
                ? playerCounterplayFeedback.GetStatusLabel()
                : "Counterplay: --";
        }

        private string BuildRunGrowthLine()
        {
            return runManager != null ? runManager.CurrentGrowthSummaryLabel : "Growth: --";
        }

        private string BuildRunGrowthRouteLine()
        {
            return runManager != null ? runManager.CurrentGrowthRouteLabel : "Growth Route: --";
        }

        private string BuildRelationshipLine()
        {
            if (companionRelationship == null)
            {
                return "AI Bond: --";
            }

            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                companionRelationship.Trust,
                companionRelationship.Affection,
                companionRelationship.MemoryTags);
            return $"AI Bond: {profile.Tier} | Trust {companionRelationship.Trust} | Affection {companionRelationship.Affection}";
        }

        private string BuildMemoryLine()
        {
            if (companionRelationship == null)
            {
                return "Memory: none";
            }

            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                companionRelationship.Trust,
                companionRelationship.Affection,
                companionRelationship.MemoryTags);
            if (!profile.HasDominantMemory)
            {
                return "Memory: none";
            }

            memoryBuilder.Clear();
            memoryBuilder.Append("Memory Lead: ");
            memoryBuilder.Append(profile.DominantMemoryTag);
            memoryBuilder.Append(" ");
            memoryBuilder.Append(profile.DominantMemoryScore);
            return memoryBuilder.ToString();
        }

        private string BuildTacticalSupportLine()
        {
            return tacticalSupport != null ? tacticalSupport.GetCooldownStatusLabel() : "AI Tactics: --";
        }

        private string BuildCompanionBuildLine()
        {
            CompanionSkillTendency tendency = tacticalSupport != null
                ? tacticalSupport.CurrentTendency
                : CompanionRunBuildState.CurrentTendency;
            return CompanionSkillTendencyRules.GetHudSummaryLine(tendency);
        }

        private string BuildCompanionTacticPlanLine()
        {
            CompanionSkillTendency tendency = tacticalSupport != null
                ? tacticalSupport.CurrentTendency
                : CompanionRunBuildState.CurrentTendency;
            return CompanionSkillTendencyRules.GetStatusPlanLine(tendency);
        }

        private void DrawToast()
        {
            Rect rect = CalculateToastRect(Screen.width, Screen.height);
            if (toastLabelStyle == null)
            {
                toastLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true
                };
            }

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(toastMessage, toastLabelStyle);
            GUILayout.EndArea();
        }

        private Rect CalculateToastRect(float screenWidth, float screenHeight)
        {
            const float desiredWidth = 720f;
            const float height = 76f;
            const float margin = 16f;
            const float gap = 12f;
            const float minimumSideWidth = 240f;

            float safeScreenWidth = Mathf.Max(0f, screenWidth - margin * 2f);
            float panelRight = panelRect.x + panelRect.width;
            float sideX = panelRight + gap;
            float sideWidth = screenWidth - sideX - margin;
            bool canPlaceBesidePanel = showPanel && sideWidth >= minimumSideWidth;

            float width = canPlaceBesidePanel
                ? Mathf.Min(desiredWidth, sideWidth)
                : Mathf.Min(desiredWidth, safeScreenWidth);
            float x = canPlaceBesidePanel
                ? sideX
                : Mathf.Max(margin, (screenWidth - width) * 0.5f);
            float preferredY = canPlaceBesidePanel || !showPanel
                ? Mathf.Max(margin, panelRect.y)
                : panelRect.y + panelRect.height + gap;
            float maximumY = Mathf.Max(margin, screenHeight - height - margin);
            float y = Mathf.Clamp(preferredY, margin, maximumY);

            return new Rect(x, y, width, height);
        }
    }
}
