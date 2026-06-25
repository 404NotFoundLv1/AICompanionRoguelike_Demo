using AICompanionRoguelike.Companion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.UI
{
    public sealed class CompanionBuildChoiceUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(360f, 72f, 440f, 260f);
        [SerializeField] private bool showPanel = true;

        [Header("Run Build")]
        [SerializeField] private CompanionCombatDialogueController selectionDialogue;
        [SerializeField] private bool resetBuildOnEnable = true;
        [SerializeField] private bool logSelection = true;
        [SerializeField, Min(0)] private int selectionFeedbackPriority = 6;

        private bool isOpen;

        public bool IsOpen => isOpen;

        private void OnEnable()
        {
            ResolveReferences();

            if (resetBuildOnEnable)
            {
                CompanionRunBuildState.Reset();
            }

            isOpen = showPanel && !CompanionRunBuildState.HasSelectedTendency;
            CompanionRunBuildState.TendencyChanged += HandleTendencyChanged;
        }

        private void OnDisable()
        {
            CompanionRunBuildState.TendencyChanged -= HandleTendencyChanged;
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SelectTendency(CompanionSkillTendency.Guardian);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SelectTendency(CompanionSkillTendency.Suppressor);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SelectTendency(CompanionSkillTendency.Link);
            }
        }

        public void SelectTendency(CompanionSkillTendency tendency)
        {
            ResolveReferences();

            CompanionSkillTendency selectedTendency = CompanionSkillTendencyRules.NormalizeSelectable(tendency);
            CompanionRunBuildState.SetTendency(selectedTendency);
            isOpen = false;
            SpeakSelectionFeedback(selectedTendency);

            if (logSelection)
            {
                Debug.Log(
                    $"Companion run build selected: {CompanionSkillTendencyRules.GetDisplayName(selectedTendency)}",
                    this);
            }
        }

        private void ResolveReferences()
        {
            selectionDialogue = selectionDialogue != null
                ? selectionDialogue
                : GetComponent<CompanionCombatDialogueController>();

            if (selectionDialogue == null)
            {
                selectionDialogue = FindAnyObjectByType<CompanionCombatDialogueController>();
            }
        }

        private void SpeakSelectionFeedback(CompanionSkillTendency tendency)
        {
            if (selectionDialogue == null)
            {
                return;
            }

            selectionDialogue.TryShowLine(
                CompanionSkillTendencyRules.GetSelectionLine(tendency),
                selectionFeedbackPriority);
        }

        private void OnGUI()
        {
            if (!showPanel || !isOpen)
            {
                return;
            }

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("选择本局 AI 队友技能倾向");
            GUILayout.Space(8f);
            DrawChoice(1, CompanionSkillTendency.Guardian);
            DrawChoice(2, CompanionSkillTendency.Suppressor);
            DrawChoice(3, CompanionSkillTendency.Link);
            GUILayout.EndArea();
        }

        private void DrawChoice(int index, CompanionSkillTendency tendency)
        {
            string title = CompanionSkillTendencyRules.GetDisplayName(tendency);
            if (GUILayout.Button($"[{index}] {title}"))
            {
                SelectTendency(tendency);
            }

            GUILayout.Label(CompanionSkillTendencyRules.GetShortDescription(tendency));
            GUILayout.Space(6f);
        }

        private void HandleTendencyChanged(CompanionSkillTendency tendency)
        {
            if (tendency != CompanionSkillTendency.Balanced)
            {
                isOpen = false;
            }
        }
    }
}
