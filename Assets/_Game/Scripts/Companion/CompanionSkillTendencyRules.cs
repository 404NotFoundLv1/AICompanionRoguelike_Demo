using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionSkillTendencyRules
    {
        public static string GetDisplayName(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "守护型 Guardian",
                CompanionSkillTendency.Suppressor => "压制型 Suppressor",
                CompanionSkillTendency.Link => "连携型 Link",
                _ => "未选择"
            };
        }

        public static string GetShortDescription(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "更早触发护援，Guard 减伤更强，冷却更短。",
                CompanionSkillTendency.Suppressor => "更积极压制低血敌人，Suppress 削弱攻击和移动。",
                CompanionSkillTendency.Link => "更频繁发起 QTE 连携邀请。",
                _ => "等待本局开始时选择。"
            };
        }

        public static string GetHudSummaryLine(CompanionSkillTendency tendency)
        {
            int upgradeLevel = CompanionRunBuildState.GetUpgradeLevel(tendency);
            string levelSuffix = upgradeLevel > 0 ? $" | Lv{upgradeLevel}" : string.Empty;

            return tendency switch
            {
                CompanionSkillTendency.Guardian => $"AI Build: Guardian | Guard stronger{levelSuffix}",
                CompanionSkillTendency.Suppressor => $"AI Build: Suppressor | Suppress stronger{levelSuffix}",
                CompanionSkillTendency.Link => $"AI Build: Link | QTE faster{levelSuffix}",
                _ => "AI Build: 未选择 | choose a run tendency"
            };
        }

        public static string GetBuildRewardTitle(CompanionSkillTendency tendency)
        {
            int nextLevel = CompanionRunBuildState.GetUpgradeLevel(tendency) + 1;
            return tendency switch
            {
                CompanionSkillTendency.Guardian => $"Guardian Build Lv{nextLevel}",
                CompanionSkillTendency.Suppressor => $"Suppressor Build Lv{nextLevel}",
                CompanionSkillTendency.Link => $"Link Build Lv{nextLevel}",
                _ => "AI Build Upgrade"
            };
        }

        public static string GetBuildRewardDescription(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "Guardian reward: Guard lasts longer, cuts more damage, and cools down faster.",
                CompanionSkillTendency.Suppressor => "Suppressor reward: Suppress lasts longer, weakens enemies more, and cools down faster.",
                CompanionSkillTendency.Link => "Link reward: QTE calls become faster during this run.",
                _ => "Strengthen the current AI Build for this run."
            };
        }

        public static string GetSelectionLine(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "AI: Guardian Build selected. I will guard you first.",
                CompanionSkillTendency.Suppressor => "AI: Suppressor Build selected. I will pin threats down.",
                CompanionSkillTendency.Link => "AI: Link Build selected. I will call QTE windows faster.",
                _ => string.Empty
            };
        }

        public static string GetTacticalActivationLine(
            CompanionSkillTendency tendency,
            CompanionDialogueEventType eventType)
        {
            if (tendency == CompanionSkillTendency.Guardian && eventType == CompanionDialogueEventType.TacticalGuard)
            {
                return "AI: Guardian Build active. Guard shield is on you.";
            }

            if (tendency == CompanionSkillTendency.Suppressor && eventType == CompanionDialogueEventType.TacticalSuppression)
            {
                return "AI: Suppressor Build active. I am pinning it down.";
            }

            if (tendency == CompanionSkillTendency.Link && eventType == CompanionDialogueEventType.QTEStarted)
            {
                return "AI: Link Build active. QTE window is yours.";
            }

            return string.Empty;
        }

        public static float GetQteCooldownMultiplier(CompanionSkillTendency tendency)
        {
            if (tendency != CompanionSkillTendency.Link)
            {
                return 1f;
            }

            int upgradeLevel = CompanionRunBuildState.GetUpgradeLevel(CompanionSkillTendency.Link);
            return 0.7f * Mathf.Pow(0.9f, upgradeLevel);
        }

        public static CompanionSkillTendency NormalizeSelectable(CompanionSkillTendency tendency)
        {
            return tendency == CompanionSkillTendency.Balanced ? CompanionSkillTendency.Guardian : tendency;
        }

        public static Color GetAccentColor(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => new Color(0.35f, 0.8f, 1f, 1f),
                CompanionSkillTendency.Suppressor => new Color(1f, 0.75f, 0.3f, 1f),
                CompanionSkillTendency.Link => new Color(0.45f, 1f, 0.65f, 1f),
                _ => Color.white
            };
        }
    }
}
