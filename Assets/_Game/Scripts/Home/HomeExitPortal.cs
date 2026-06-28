using System.Collections.Generic;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Progression;
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
        [SerializeField] private Key cancelKey = Key.Escape;
        [SerializeField] private Key alternateCancelKey = Key.Q;
        [SerializeField] private Key guardianTacticKey = Key.Digit1;
        [SerializeField] private Key suppressorTacticKey = Key.Digit2;
        [SerializeField] private Key linkTacticKey = Key.Digit3;
        [SerializeField] private bool requireConfirmInput = true;
        [SerializeField] private bool showInteractionPrompt = true;
        [SerializeField] private string promptText = "Press E to prepare expedition";
        [SerializeField] private Rect preparationPanelRect = new Rect(0f, 72f, 620f, 360f);
        [SerializeField] private bool resetTacticOnPreparationOpen = true;
        [SerializeField] private CompanionSkillTendency defaultPreparationTendency = CompanionSkillTendency.Guardian;
        [SerializeField] private Color readyColor = new Color(0.25f, 1f, 0.75f, 1f);
        [SerializeField] private bool logTransition = true;

        private bool isTransitioning;
        private bool playerInRange;
        private bool isPreparationOpen;
        private SpriteRenderer portalRenderer;
        private Color idleColor;

        public bool IsPlayerInRange => playerInRange;
        public bool IsPreparationOpen => isPreparationOpen;
        public bool IsTransitioning => isTransitioning;

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
            if (keyboard == null)
            {
                return;
            }

            if (isPreparationOpen)
            {
                if (WasKeyPressed(keyboard, cancelKey) || WasKeyPressed(keyboard, alternateCancelKey))
                {
                    CancelPreparation();
                    return;
                }

                if (TrySelectPreparationTacticFromInput(keyboard))
                {
                    return;
                }

                if (WasKeyPressed(keyboard, interactKey))
                {
                    ConfirmPreparation();
                }

                return;
            }

            if (WasKeyPressed(keyboard, interactKey))
            {
                OpenPreparationPanel();
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
            CancelPreparation();
            RefreshPortalFeedback();
        }

        public void OpenPreparationPanel()
        {
            if (isTransitioning)
            {
                return;
            }

            if (!isPreparationOpen)
            {
                StartFreshPreparationTactic();
            }

            isPreparationOpen = true;
        }

        public void CancelPreparation()
        {
            isPreparationOpen = false;
        }

        public void ConfirmPreparation()
        {
            if (!isPreparationOpen || isTransitioning)
            {
                return;
            }

            if (!CompanionRunBuildState.HasSelectedTendency)
            {
                SelectPreparationTendency(defaultPreparationTendency);
            }

            EnterBattle();
        }

        public void SelectPreparationTendency(CompanionSkillTendency tendency)
        {
            CompanionSkillTendency selectedTendency = CompanionSkillTendencyRules.NormalizeSelectable(tendency);
            CompanionRunBuildState.SetTendency(selectedTendency);
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
            isPreparationOpen = false;
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

            if (isPreparationOpen)
            {
                DrawPreparationPanel();
                return;
            }

            const float width = 280f;
            const float height = 54f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 120f, width, height);
            GUI.Box(rect, promptText);
        }

        private void DrawPreparationPanel()
        {
            Rect rect = HomeMetaUpgradeStation.GetClampedPromptRect(
                GetCenteredPreparationRect(preparationPanelRect),
                Screen.width,
                Screen.height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            string[] lines = BuildPreparationLines();
            for (int i = 0; i < lines.Length; i++)
            {
                GUILayout.Label(lines[i]);
            }

            GUILayout.Space(8f);
            GUILayout.Label($"[{FormatKey(interactKey)}] Confirm    [{FormatKey(cancelKey)}/{FormatKey(alternateCancelKey)}] Cancel");
            GUILayout.EndArea();
        }

        public static string[] BuildPreparationLines()
        {
            List<string> lines = new List<string>
            {
                "Expedition Preparation",
                BuildPermanentUpgradeLine(),
                BuildCompanionReadinessLine()
            };
            lines.AddRange(BuildTacticalChoiceLines());
            lines.Add("Confirm to enter the next combat run.");
            return lines.ToArray();
        }

        public static string[] BuildTacticalChoiceLines()
        {
            CompanionSkillTendency currentTendency = CompanionSkillTendencyRules.NormalizeSelectable(
                CompanionRunBuildState.CurrentTendency);

            return new[]
            {
                "AI Tactic Choices",
                $"Current AI Tactic: {GetTacticName(currentTendency)} - {GetTacticSummary(currentTendency)}",
                "[1] Guardian - guard earlier and reduce incoming damage",
                "[2] Suppressor - weaken dangerous enemies more often",
                "[3] Link - call QTE link windows faster"
            };
        }

        public static string BuildPermanentUpgradeLine()
        {
            return $"Permanent Upgrades: HP Lv{MetaProgressionState.PlayerMaxHealthLevel} / Damage Lv{MetaProgressionState.PlayerDamageLevel} / AI Cooldown Lv{MetaProgressionState.CompanionCooldownLevel}";
        }

        public static string BuildCompanionReadinessLine()
        {
            if (!CompanionRelationshipState.HasState)
            {
                return $"AI Readiness: Bond unrecorded | {CompanionSkillTendencyRules.GetHudSummaryLine(CompanionRunBuildState.CurrentTendency)}";
            }

            CompanionRelationshipProfileSnapshot profile = CompanionRelationshipProfile.Evaluate(
                CompanionRelationshipState.Trust,
                CompanionRelationshipState.Affection,
                CompanionRelationshipState.MemoryTags);
            string memoryPart = profile.HasDominantMemory
                ? $" | Memory {profile.DominantMemoryTag} x{profile.DominantMemoryScore}"
                : " | Memory none";

            return $"AI Readiness: {profile.Tier} | Trust {CompanionRelationshipState.Trust} | Affection {CompanionRelationshipState.Affection}{memoryPart} | {CompanionSkillTendencyRules.GetHudSummaryLine(CompanionRunBuildState.CurrentTendency)}";
        }

        private void StartFreshPreparationTactic()
        {
            if (resetTacticOnPreparationOpen)
            {
                CompanionRunBuildState.Reset();
            }

            SelectPreparationTendency(defaultPreparationTendency);
        }

        private bool TrySelectPreparationTacticFromInput(Keyboard keyboard)
        {
            if (WasKeyPressed(keyboard, guardianTacticKey))
            {
                SelectPreparationTendency(CompanionSkillTendency.Guardian);
                return true;
            }

            if (WasKeyPressed(keyboard, suppressorTacticKey))
            {
                SelectPreparationTendency(CompanionSkillTendency.Suppressor);
                return true;
            }

            if (WasKeyPressed(keyboard, linkTacticKey))
            {
                SelectPreparationTendency(CompanionSkillTendency.Link);
                return true;
            }

            return false;
        }

        private static string GetTacticName(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "Guardian",
                CompanionSkillTendency.Suppressor => "Suppressor",
                CompanionSkillTendency.Link => "Link",
                _ => "Guardian"
            };
        }

        private static string GetTacticSummary(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "safer protection",
                CompanionSkillTendency.Suppressor => "stronger enemy control",
                CompanionSkillTendency.Link => "faster QTE support",
                _ => "safer protection"
            };
        }

        private static Rect GetCenteredPreparationRect(Rect sourceRect)
        {
            float width = Mathf.Max(220f, sourceRect.width);
            float x = sourceRect.x <= 0f ? (Screen.width - width) * 0.5f : sourceRect.x;
            return new Rect(x, sourceRect.y, width, Mathf.Max(240f, sourceRect.height));
        }

        private static bool WasKeyPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        private static string FormatKey(Key key)
        {
            if (key == Key.Digit1)
            {
                return "1";
            }

            if (key == Key.Digit2)
            {
                return "2";
            }

            if (key == Key.Digit3)
            {
                return "3";
            }

            return key.ToString();
        }
    }
}
