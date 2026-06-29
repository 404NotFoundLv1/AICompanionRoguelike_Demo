using System.Collections.Generic;
using AICompanionRoguelike.Roguelike;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.UI
{
    public sealed class SafeRestChoiceUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(360f, 96f, 460f, 260f);
        [SerializeField] private bool showPanel = true;

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (runManager == null)
            {
                ResolveReferences();
            }

            if (runManager == null || !runManager.IsWaitingForRest)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                runManager.CloseSafeRestDraft();
                return;
            }

            IReadOnlyList<SafeRestChoice> choices = runManager.CurrentRestChoices;
            for (int i = 0; i < choices.Count && i < 9; i++)
            {
                if (WasDigitPressed(keyboard, i + 1))
                {
                    runManager.SelectSafeRestChoice(i);
                    return;
                }
            }
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = FindAnyObjectByType<RunManager>();
            }
        }

        private void OnGUI()
        {
            if (!showPanel || runManager == null || !runManager.IsWaitingForRest)
            {
                return;
            }

            IReadOnlyList<SafeRestChoice> choices = runManager.CurrentRestChoices;
            if (choices.Count == 0)
            {
                return;
            }

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Safe Rest Point");
            if (!string.IsNullOrWhiteSpace(runManager.LastSafeRestFeedbackMessage))
            {
                GUILayout.Label(runManager.LastSafeRestFeedbackMessage);
            }

            GUILayout.Space(8f);

            for (int i = 0; i < choices.Count; i++)
            {
                SafeRestChoice choice = choices[i];
                if (GUILayout.Button($"[{i + 1}] {choice.Title}"))
                {
                    runManager.SelectSafeRestChoice(i);
                }

                if (!string.IsNullOrWhiteSpace(choice.PreviewLine))
                {
                    GUILayout.Label(choice.PreviewLine);
                }

                GUILayout.Label(choice.Description);
                GUILayout.Space(6f);
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("[Esc] Close"))
            {
                runManager.CloseSafeRestDraft();
            }

            GUILayout.EndArea();
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
    }
}
