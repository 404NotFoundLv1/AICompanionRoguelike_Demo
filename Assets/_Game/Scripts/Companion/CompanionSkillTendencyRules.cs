using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionSkillTendencyRules
    {
        public static string GetDisplayName(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "守护型",
                CompanionSkillTendency.Suppressor => "压制型",
                CompanionSkillTendency.Link => "连携型",
                _ => "未选择"
            };
        }

        public static string GetShortDescription(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => "更早触发护援，减伤更强，冷却更短。",
                CompanionSkillTendency.Suppressor => "更积极压制低血敌人，削弱攻击和移动。",
                CompanionSkillTendency.Link => "更频繁发起 QTE 连携邀请。",
                _ => "等待本局开始时选择。"
            };
        }

        public static float GetQteCooldownMultiplier(CompanionSkillTendency tendency)
        {
            return tendency == CompanionSkillTendency.Link ? 0.7f : 1f;
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
