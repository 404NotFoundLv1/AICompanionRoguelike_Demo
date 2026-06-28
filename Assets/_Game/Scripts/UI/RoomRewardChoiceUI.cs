using System.Collections.Generic;
using AICompanionRoguelike.Roguelike;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.UI
{
    public sealed class RoomRewardChoiceUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new Rect(360f, 96f, 420f, 250f);
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

            if (runManager == null || !runManager.IsWaitingForReward)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (runManager.IsCurrentRewardShopPurchase && keyboard.escapeKey.wasPressedThisFrame)
            {
                runManager.CloseShopRewardDraft();
                return;
            }

            IReadOnlyList<RunRewardChoice> rewards = runManager.CurrentRewardChoices;
            for (int i = 0; i < rewards.Count && i < 9; i++)
            {
                if (WasDigitPressed(keyboard, i + 1))
                {
                    runManager.SelectReward(i);
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
            if (!showPanel || runManager == null || !runManager.IsWaitingForReward)
            {
                return;
            }

            IReadOnlyList<RunRewardChoice> rewards = runManager.CurrentRewardChoices;
            if (rewards.Count == 0)
            {
                return;
            }

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("选择奖励");
            if (runManager.IsCurrentRewardShopPurchase)
            {
                GUILayout.Label(runManager.CurrentShopAffordabilityLabel);
                if (!string.IsNullOrWhiteSpace(runManager.LastShopFeedbackMessage))
                {
                    GUILayout.Label(runManager.LastShopFeedbackMessage);
                }
            }

            GUILayout.Space(8f);

            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardChoice reward = rewards[i];
                string costSuffix = runManager.IsCurrentRewardShopPurchase
                    ? $" (Cost {runManager.CurrentShopRewardCost})"
                    : string.Empty;
                if (GUILayout.Button($"[{i + 1}] [{reward.CategoryLabel}] {reward.Title}{costSuffix}"))
                {
                    runManager.SelectReward(i);
                }

                if (!string.IsNullOrWhiteSpace(reward.PreviewLine))
                {
                    GUILayout.Label(reward.PreviewLine);
                }

                GUILayout.Label(reward.Description);
                GUILayout.Space(6f);
            }

            if (runManager.IsCurrentRewardShopPurchase)
            {
                GUILayout.Space(4f);
                if (GUILayout.Button("[Esc] Close / Skip purchase"))
                {
                    runManager.CloseShopRewardDraft();
                }
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
