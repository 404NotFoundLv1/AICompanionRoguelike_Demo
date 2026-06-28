using AICompanionRoguelike.Progression;
using UnityEngine;

namespace AICompanionRoguelike.Home
{
    public sealed class HomeMetaProgressionHUD : MonoBehaviour
    {
        [SerializeField] private bool showHud = true;
        [SerializeField] private Rect hudRect = new Rect(16f, 16f, 360f, 118f);

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }

            GUILayout.BeginArea(HomeMetaUpgradeStation.GetClampedPromptRect(hudRect, Screen.width, Screen.height), GUI.skin.box);
            string[] lines = BuildStatusLines();
            for (int i = 0; i < lines.Length; i++)
            {
                GUILayout.Label(lines[i]);
            }

            GUILayout.EndArea();
        }

        public static string[] BuildStatusLines()
        {
            return new[]
            {
                $"Core Fragments: {MetaProgressionState.CoreFragments}",
                $"Player Max HP Lv{MetaProgressionState.PlayerMaxHealthLevel} | Next: +10 Max HP",
                $"Player Damage Lv{MetaProgressionState.PlayerDamageLevel} | Next: +8% damage",
                $"AI Support Cooldown Lv{MetaProgressionState.CompanionCooldownLevel} | Next: -5% cooldown"
            };
        }
    }
}
