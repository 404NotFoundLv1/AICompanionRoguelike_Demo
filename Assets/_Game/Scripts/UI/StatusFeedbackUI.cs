using System.Text;
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
        [SerializeField] private CompanionRelationship companionRelationship;
        [SerializeField] private BranchEventRoomController branchEventRoomController;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 330f, 210f);
        [SerializeField] private float toastDuration = 3f;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool showToast = true;

        private readonly StringBuilder memoryBuilder = new StringBuilder(96);
        private BranchEventRoomController subscribedBranchEventRoomController;
        private string toastMessage;
        private float toastTimer;

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

            if (playerHealth == null || companionRelationship == null || branchEventRoomController == null)
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
            }

            if (branchChoiceBuff == null && playerHealth != null)
            {
                branchChoiceBuff = playerHealth.GetComponent<PlayerBranchChoiceBuff>();
            }

            if (companionRelationship == null)
            {
                companionRelationship = FindAnyObjectByType<CompanionRelationship>();
            }

            if (branchEventRoomController == null)
            {
                branchEventRoomController = FindAnyObjectByType<BranchEventRoomController>();
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
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Run Status");
            GUILayout.Space(4f);
            GUILayout.Label(BuildPlayerHealthLine());
            GUILayout.Label(BuildChallengeBuffLine());
            GUILayout.Space(4f);
            GUILayout.Label(BuildRelationshipLine());
            GUILayout.Label(BuildMemoryLine());

            if (branchEventRoomController != null && branchEventRoomController.IsWaitingForChoice)
            {
                GUILayout.Space(4f);
                GUILayout.Label("Branch Room: previous combat room frozen");
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

        private string BuildRelationshipLine()
        {
            if (companionRelationship == null)
            {
                return "AI Bond: --";
            }

            return $"AI Bond: Trust {companionRelationship.Trust} | Affection {companionRelationship.Affection}";
        }

        private string BuildMemoryLine()
        {
            if (companionRelationship == null || companionRelationship.MemoryTags.Count == 0)
            {
                return "Memory: none";
            }

            memoryBuilder.Clear();
            memoryBuilder.Append("Memory: ");

            for (int i = 0; i < companionRelationship.MemoryTags.Count; i++)
            {
                RelationshipMemoryTagScore entry = companionRelationship.MemoryTags[i];
                if (i > 0)
                {
                    memoryBuilder.Append(", ");
                }

                memoryBuilder.Append(entry.tag);
                memoryBuilder.Append(" ");
                memoryBuilder.Append(entry.score);
            }

            return memoryBuilder.ToString();
        }

        private void DrawToast()
        {
            const float width = 720f;
            const float height = 76f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                24f,
                width,
                height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(toastMessage);
            GUILayout.EndArea();
        }
    }
}
