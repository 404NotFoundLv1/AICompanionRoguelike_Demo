using AICompanionRoguelike.Enemy;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionTargetPriorityRules
    {
        private const float BaseScore = 100f;
        private const float DistanceCostPerUnit = 10f;
        private const float GuardianInterceptRange = 2.25f;

        public static float EvaluateScore(
            EnemyArchetypeType archetype,
            CompanionSkillTendency tendency,
            float companionDistance,
            float playerDistance,
            bool warningActive,
            bool isCurrentTarget)
        {
            float safeCompanionDistance = Mathf.Max(0f, companionDistance);
            float safePlayerDistance = Mathf.Max(0f, playerDistance);
            float score = BaseScore - safeCompanionDistance * DistanceCostPerUnit;

            score += archetype switch
            {
                EnemyArchetypeType.Ranged => 70f,
                EnemyArchetypeType.Guard => 15f,
                _ => 20f
            };

            if (warningActive)
            {
                score += 80f;
            }

            if (tendency == CompanionSkillTendency.Guardian
                && safePlayerDistance <= GuardianInterceptRange)
            {
                score += 70f + (GuardianInterceptRange - safePlayerDistance) * 10f;
            }

            switch (tendency)
            {
                case CompanionSkillTendency.Guardian when warningActive:
                    score += 20f;
                    break;
                case CompanionSkillTendency.Suppressor when archetype == EnemyArchetypeType.Ranged:
                    score += 35f;
                    break;
                case CompanionSkillTendency.Link when archetype == EnemyArchetypeType.Guard:
                    score += 55f;
                    break;
            }

            if (isCurrentTarget)
            {
                score += 12f;
            }

            return score;
        }

        public static CompanionTargetDecisionReason EvaluateReason(
            EnemyArchetypeType archetype,
            CompanionSkillTendency tendency,
            float playerDistance,
            bool warningActive)
        {
            if (warningActive
                || (tendency == CompanionSkillTendency.Guardian
                    && Mathf.Max(0f, playerDistance) <= GuardianInterceptRange))
            {
                return CompanionTargetDecisionReason.PlayerThreat;
            }

            if (tendency == CompanionSkillTendency.Link && archetype == EnemyArchetypeType.Guard)
            {
                return CompanionTargetDecisionReason.GuardLinkOpportunity;
            }

            return archetype == EnemyArchetypeType.Ranged
                ? CompanionTargetDecisionReason.RangedThreat
                : CompanionTargetDecisionReason.ClosestThreat;
        }

        public static string BuildDecisionLine(
            EnemyArchetypeType archetype,
            CompanionSkillTendency tendency,
            CompanionTargetDecisionReason reason)
        {
            switch (reason)
            {
                case CompanionTargetDecisionReason.PlayerThreat:
                    return "AI: 威胁接近 / Threat on you. I will intercept.";
                case CompanionTargetDecisionReason.GuardLinkOpportunity:
                    return "AI: 识别守卫 / Guard marked. Prepare a link attack.";
                case CompanionTargetDecisionReason.RangedThreat
                    when tendency == CompanionSkillTendency.Suppressor:
                    return "AI: 锁定远程 / Ranged threat. I will suppress it.";
                case CompanionTargetDecisionReason.RangedThreat:
                    return "AI: 锁定远程 / Ranged threat marked.";
                case CompanionTargetDecisionReason.ClosestThreat
                    when archetype == EnemyArchetypeType.Guard:
                    return "AI: 锁定守卫 / Guard threat marked.";
                case CompanionTargetDecisionReason.ClosestThreat:
                    return "AI: 锁定近战 / Melee threat marked.";
                default:
                    return string.Empty;
            }
        }

        public static int GetDecisionPriority(CompanionTargetDecisionReason reason)
        {
            return reason switch
            {
                CompanionTargetDecisionReason.PlayerThreat => 4,
                CompanionTargetDecisionReason.RangedThreat => 3,
                CompanionTargetDecisionReason.GuardLinkOpportunity => 3,
                CompanionTargetDecisionReason.ClosestThreat => 2,
                _ => 0
            };
        }
    }
}
