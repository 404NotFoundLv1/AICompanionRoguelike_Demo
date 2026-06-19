using System.Text;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Roguelike;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Home
{
    public enum HomeCompanionDialogueChoice
    {
        ThankSupport,
        DiscussTactics,
        KeepDistance
    }

    public readonly struct HomeCompanionDialogueChoiceOutcome
    {
        public HomeCompanionDialogueChoiceOutcome(
            HomeCompanionDialogueChoice choice,
            string label,
            string playerLine,
            string reactionLine,
            int trustDelta,
            int affectionDelta,
            RelationshipMemoryTag memoryTag)
        {
            Choice = choice;
            Label = label;
            PlayerLine = playerLine;
            ReactionLine = reactionLine;
            TrustDelta = trustDelta;
            AffectionDelta = affectionDelta;
            MemoryTag = memoryTag;
        }

        public HomeCompanionDialogueChoice Choice { get; }
        public string Label { get; }
        public string PlayerLine { get; }
        public string ReactionLine { get; }
        public int TrustDelta { get; }
        public int AffectionDelta { get; }
        public RelationshipMemoryTag MemoryTag { get; }
    }

    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HomeCompanionDialogue : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CompanionRelationship relationship;

        [Header("Interaction")]
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private string promptText = "\u6309 E \u4E0E AI \u961F\u53CB\u4EA4\u8C08";

        [Header("Layout")]
        [SerializeField] private Rect promptRect = new Rect(0f, 0f, 260f, 54f);
        [SerializeField] private Rect dialogueRect = new Rect(0f, 72f, 560f, 250f);

        [Header("Visual Feedback")]
        [SerializeField] private Color readyColor = new Color(0.9f, 0.75f, 1f, 1f);

        [Header("Choice Outcomes")]
        [SerializeField] private int thankSupportTrustDelta = 1;
        [SerializeField] private int thankSupportAffectionDelta = 4;
        [SerializeField] private int discussTacticsTrustDelta = 4;
        [SerializeField] private int discussTacticsAffectionDelta = 1;
        [SerializeField] private int keepDistanceTrustDelta;
        [SerializeField] private int keepDistanceAffectionDelta = -2;

        private readonly StringBuilder dialogueBuilder = new StringBuilder(384);
        private readonly StringBuilder memoryBuilder = new StringBuilder(96);
        private bool isPlayerInRange;
        private bool isDialogueOpen;
        private bool hasDialogueChoice;
        private int syncedSummaryRunId = -1;
        private HomeCompanionDialogueChoiceOutcome lastChoiceOutcome;
        private SpriteRenderer spriteRenderer;
        private Color idleColor;

        public bool IsPlayerInRange => isPlayerInRange;
        public bool IsDialogueOpen => isDialogueOpen;
        public bool HasDialogueChoice => hasDialogueChoice;
        public string LastChoiceReactionLine => hasDialogueChoice ? lastChoiceOutcome.ReactionLine : string.Empty;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            relationship = GetComponent<CompanionRelationship>();
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            idleColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            relationship = relationship != null ? relationship : GetComponent<CompanionRelationship>();
        }

        private void Update()
        {
            if (!isPlayerInRange)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && interactKey != Key.None && keyboard[interactKey].wasPressedThisFrame)
            {
                TryToggleDialogue();
            }
        }

        private void OnValidate()
        {
            promptRect.width = Mathf.Max(160f, promptRect.width);
            promptRect.height = Mathf.Max(36f, promptRect.height);
            dialogueRect.width = Mathf.Max(260f, dialogueRect.width);
            dialogueRect.height = Mathf.Max(160f, dialogueRect.height);
        }

        public void Configure(CompanionRelationship companionRelationship)
        {
            relationship = companionRelationship;
        }

        public bool TryToggleDialogue()
        {
            if (!isPlayerInRange)
            {
                return false;
            }

            SetDialogueOpen(!isDialogueOpen);
            return true;
        }

        public void SetDialogueOpen(bool open)
        {
            isDialogueOpen = isPlayerInRange && open;
        }

        public HomeCompanionDialogueChoiceOutcome ApplyDialogueChoice(HomeCompanionDialogueChoice choice)
        {
            if (hasDialogueChoice)
            {
                return lastChoiceOutcome;
            }

            RunSessionSummary summary = RunSessionState.LastSummary;
            EnsureRelationshipSeededFromSummary(summary);

            HomeCompanionDialogueChoiceOutcome outcome = CreateChoiceOutcome(choice);
            if (relationship != null)
            {
                relationship.ApplyMemoryEvent(
                    $"Home Dialogue: {outcome.Label}",
                    outcome.TrustDelta,
                    outcome.AffectionDelta,
                    outcome.MemoryTag);
            }

            hasDialogueChoice = true;
            lastChoiceOutcome = outcome;
            return outcome;
        }

        public string BuildDialogueText()
        {
            RunSessionSummary summary = RunSessionState.LastSummary;
            EnsureRelationshipSeededFromSummary(summary);
            dialogueBuilder.Clear();

            dialogueBuilder.AppendLine("AI Companion");
            dialogueBuilder.AppendLine(BuildGreetingLine(summary));

            if (hasDialogueChoice)
            {
                dialogueBuilder.Append("You: ");
                dialogueBuilder.AppendLine(lastChoiceOutcome.PlayerLine);
                dialogueBuilder.AppendLine(lastChoiceOutcome.ReactionLine);
                dialogueBuilder.Append("Last Home Choice: ");
                dialogueBuilder.Append(lastChoiceOutcome.Label);
                dialogueBuilder.Append("  Bond ");
                dialogueBuilder.Append(lastChoiceOutcome.TrustDelta.ToString("+#;-#;0"));
                dialogueBuilder.Append("/");
                dialogueBuilder.AppendLine(lastChoiceOutcome.AffectionDelta.ToString("+#;-#;0"));
            }

            dialogueBuilder.AppendLine(BuildBondLine(summary));
            dialogueBuilder.AppendLine(BuildMemoryLine(summary));

            if (summary.HasSummary)
            {
                dialogueBuilder.Append("Boss AI Stats: shield ");
                dialogueBuilder.Append(summary.BossSupportActivations);
                dialogueBuilder.Append(", warning hit ");
                dialogueBuilder.Append(summary.BossWarningHits);
                dialogueBuilder.Append(", dodge ");
                dialogueBuilder.Append(summary.BossWarningDodges);
                dialogueBuilder.AppendLine();
            }
            else
            {
                dialogueBuilder.AppendLine("Boss AI Stats: no run data yet.");
            }

            dialogueBuilder.AppendLine("Press E to close.");
            return dialogueBuilder.ToString();
        }

        private void EnsureRelationshipSeededFromSummary(RunSessionSummary summary)
        {
            if (relationship == null || !summary.HasRelationship || syncedSummaryRunId == summary.RunId)
            {
                return;
            }

            relationship.SetRelationshipValues(summary.FinalTrust, summary.FinalAffection);
            syncedSummaryRunId = summary.RunId;
        }

        private string BuildGreetingLine(RunSessionSummary summary)
        {
            if (summary.HasCompanionFeedback)
            {
                return summary.CompanionFeedbackLine;
            }

            return "AI: The home is quiet. When you are ready, we can try another run.";
        }

        private string BuildBondLine(RunSessionSummary summary)
        {
            if (relationship != null)
            {
                return $"AI Bond: Trust {relationship.Trust} | Affection {relationship.Affection}";
            }

            if (summary.HasRelationship)
            {
                return $"AI Bond: Trust {summary.FinalTrust} | Affection {summary.FinalAffection}";
            }

            return "AI Bond: Trust -- | Affection --";
        }

        private string BuildMemoryLine(RunSessionSummary summary)
        {
            if (relationship != null && relationship.MemoryTags.Count > 0)
            {
                memoryBuilder.Clear();
                memoryBuilder.Append("Memory: ");

                for (int i = 0; i < relationship.MemoryTags.Count; i++)
                {
                    RelationshipMemoryTagScore entry = relationship.MemoryTags[i];
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

            if (summary.HasCompanionFeedback)
            {
                return $"Recent Memory: {summary.CompanionFeedbackLine}";
            }

            return "Recent Memory: none yet.";
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            isPlayerInRange = true;
            RefreshVisualFeedback();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            isPlayerInRange = false;
            isDialogueOpen = false;
            RefreshVisualFeedback();
        }

        private static bool IsPlayerCollider(Collider2D other)
        {
            return other != null && other.GetComponentInParent<PlayerMovement2D>() != null;
        }

        private HomeCompanionDialogueChoiceOutcome CreateChoiceOutcome(HomeCompanionDialogueChoice choice)
        {
            switch (choice)
            {
                case HomeCompanionDialogueChoice.ThankSupport:
                    return new HomeCompanionDialogueChoiceOutcome(
                        choice,
                        "Thank Support",
                        "Thank you for standing with me.",
                        "AI: I will remember that you noticed my support.",
                        thankSupportTrustDelta,
                        thankSupportAffectionDelta,
                        RelationshipMemoryTag.Protected);
                case HomeCompanionDialogueChoice.DiscussTactics:
                    return new HomeCompanionDialogueChoiceOutcome(
                        choice,
                        "Discuss Tactics",
                        "Let's review the fight and adjust our plan.",
                        "AI: Good. Better plans mean I can cover you more precisely.",
                        discussTacticsTrustDelta,
                        discussTacticsAffectionDelta,
                        RelationshipMemoryTag.Reliable);
                case HomeCompanionDialogueChoice.KeepDistance:
                    return new HomeCompanionDialogueChoiceOutcome(
                        choice,
                        "Keep Distance",
                        "I need some quiet before the next run.",
                        "AI: Understood. I will give you space for now.",
                        keepDistanceTrustDelta,
                        keepDistanceAffectionDelta,
                        RelationshipMemoryTag.Cold);
                default:
                    return CreateChoiceOutcome(HomeCompanionDialogueChoice.ThankSupport);
            }
        }

        private void RefreshVisualFeedback()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = isPlayerInRange ? readyColor : idleColor;
        }

        private void OnGUI()
        {
            if (showPrompt && isPlayerInRange && !isDialogueOpen)
            {
                Rect rect = GetPromptRect();
                GUI.Box(rect, promptText);
            }

            if (!isDialogueOpen)
            {
                return;
            }

            Rect dialogue = GetCenteredRect(dialogueRect);
            GUILayout.BeginArea(dialogue, GUI.skin.box);
            GUILayout.Label(BuildDialogueText());
            DrawChoiceButtons();
            GUILayout.EndArea();
        }

        private void DrawChoiceButtons()
        {
            if (hasDialogueChoice)
            {
                GUILayout.Space(4f);
                GUILayout.Label("Home memory recorded.");
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Choose a reply:");

            if (GUILayout.Button("Thank Support"))
            {
                ApplyDialogueChoice(HomeCompanionDialogueChoice.ThankSupport);
            }

            if (GUILayout.Button("Discuss Tactics"))
            {
                ApplyDialogueChoice(HomeCompanionDialogueChoice.DiscussTactics);
            }

            if (GUILayout.Button("Keep Distance"))
            {
                ApplyDialogueChoice(HomeCompanionDialogueChoice.KeepDistance);
            }
        }

        private Rect GetPromptRect()
        {
            Rect rect = promptRect;
            float x = rect.x <= 0f ? (Screen.width - rect.width) * 0.5f : rect.x;
            float y = rect.y <= 0f ? Screen.height - 186f : rect.y;
            return new Rect(Mathf.Max(8f, x), Mathf.Max(8f, y), rect.width, rect.height);
        }

        private static Rect GetCenteredRect(Rect sourceRect)
        {
            float width = Mathf.Min(sourceRect.width, Mathf.Max(220f, Screen.width - 16f));
            float x = sourceRect.x <= 0f ? (Screen.width - width) * 0.5f : sourceRect.x;
            return new Rect(Mathf.Max(8f, x), sourceRect.y, width, sourceRect.height);
        }
    }
}
