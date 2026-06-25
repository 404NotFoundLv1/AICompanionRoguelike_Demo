using AICompanionRoguelike.Memory;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public readonly struct CompanionTacticalSupportTuning
    {
        public CompanionTacticalSupportTuning(
            float guardDuration,
            float guardDamageMultiplier,
            float guardCooldown,
            float guardTriggerHealthRatio,
            float suppressionDuration,
            float suppressionDamageMultiplier,
            float suppressionMoveMultiplier,
            float suppressionCooldown,
            float suppressionTriggerHealthRatio)
        {
            GuardDuration = guardDuration;
            GuardDamageMultiplier = guardDamageMultiplier;
            GuardCooldown = guardCooldown;
            GuardTriggerHealthRatio = guardTriggerHealthRatio;
            SuppressionDuration = suppressionDuration;
            SuppressionDamageMultiplier = suppressionDamageMultiplier;
            SuppressionMoveMultiplier = suppressionMoveMultiplier;
            SuppressionCooldown = suppressionCooldown;
            SuppressionTriggerHealthRatio = suppressionTriggerHealthRatio;
        }

        public float GuardDuration { get; }
        public float GuardDamageMultiplier { get; }
        public float GuardCooldown { get; }
        public float GuardTriggerHealthRatio { get; }
        public float SuppressionDuration { get; }
        public float SuppressionDamageMultiplier { get; }
        public float SuppressionMoveMultiplier { get; }
        public float SuppressionCooldown { get; }
        public float SuppressionTriggerHealthRatio { get; }
    }

    public static class CompanionTacticalSupportRules
    {
        public static CompanionTacticalSupportTuning Evaluate(CompanionRelationshipProfileSnapshot profile)
        {
            return Evaluate(profile, CompanionSkillTendency.Balanced);
        }

        public static CompanionTacticalSupportTuning Evaluate(
            CompanionRelationshipProfileSnapshot profile,
            CompanionSkillTendency tendency)
        {
            float guardDuration;
            float guardDamageMultiplier;
            float guardCooldown;
            float guardTriggerHealthRatio = 0.35f;
            float suppressionDuration;
            float suppressionDamageMultiplier;
            float suppressionMoveMultiplier;
            float suppressionCooldown;
            float suppressionTriggerHealthRatio = 0.55f;

            switch (profile.Tier)
            {
                case CompanionBondTier.Distant:
                    guardDuration = 1.1f;
                    guardDamageMultiplier = 0.85f;
                    guardCooldown = 14f;
                    suppressionDuration = 1.4f;
                    suppressionDamageMultiplier = 0.85f;
                    suppressionMoveMultiplier = 0.85f;
                    suppressionCooldown = 12f;
                    break;
                case CompanionBondTier.Synchronized:
                    guardDuration = 2.6f;
                    guardDamageMultiplier = 0.5f;
                    guardCooldown = 7f;
                    suppressionDuration = 2.8f;
                    suppressionDamageMultiplier = 0.55f;
                    suppressionMoveMultiplier = 0.55f;
                    suppressionCooldown = 6.5f;
                    break;
                default:
                    guardDuration = 1.8f;
                    guardDamageMultiplier = 0.65f;
                    guardCooldown = 10f;
                    suppressionDuration = 2f;
                    suppressionDamageMultiplier = 0.7f;
                    suppressionMoveMultiplier = 0.7f;
                    suppressionCooldown = 9f;
                    break;
            }

            if (profile.HasDominantMemory)
            {
                switch (profile.DominantMemoryTag)
                {
                    case RelationshipMemoryTag.Protected:
                        guardDuration += 0.5f;
                        guardDamageMultiplier -= 0.1f;
                        guardCooldown *= 0.85f;
                        break;
                    case RelationshipMemoryTag.Reliable:
                        suppressionDamageMultiplier -= 0.08f;
                        suppressionCooldown *= 0.9f;
                        break;
                    case RelationshipMemoryTag.Brave:
                        suppressionDuration += 0.45f;
                        suppressionMoveMultiplier -= 0.08f;
                        break;
                }
            }

            switch (tendency)
            {
                case CompanionSkillTendency.Guardian:
                    guardDuration += 0.55f;
                    guardDamageMultiplier -= 0.12f;
                    guardCooldown *= 0.75f;
                    guardTriggerHealthRatio += 0.1f;
                    break;
                case CompanionSkillTendency.Suppressor:
                    suppressionDuration += 0.45f;
                    suppressionDamageMultiplier -= 0.12f;
                    suppressionMoveMultiplier -= 0.1f;
                    suppressionCooldown *= 0.75f;
                    suppressionTriggerHealthRatio += 0.12f;
                    break;
                case CompanionSkillTendency.Link:
                    suppressionCooldown *= 0.95f;
                    break;
            }

            int upgradeLevel = CompanionRunBuildState.GetUpgradeLevel(tendency);
            if (upgradeLevel > 0)
            {
                switch (tendency)
                {
                    case CompanionSkillTendency.Guardian:
                        guardDuration += 0.35f * upgradeLevel;
                        guardDamageMultiplier -= 0.05f * upgradeLevel;
                        guardCooldown *= Mathf.Pow(0.9f, upgradeLevel);
                        guardTriggerHealthRatio += 0.03f * upgradeLevel;
                        break;
                    case CompanionSkillTendency.Suppressor:
                        suppressionDuration += 0.35f * upgradeLevel;
                        suppressionDamageMultiplier -= 0.05f * upgradeLevel;
                        suppressionMoveMultiplier -= 0.05f * upgradeLevel;
                        suppressionCooldown *= Mathf.Pow(0.9f, upgradeLevel);
                        suppressionTriggerHealthRatio += 0.04f * upgradeLevel;
                        break;
                }
            }

            return new CompanionTacticalSupportTuning(
                Mathf.Max(0.1f, guardDuration),
                Mathf.Clamp(guardDamageMultiplier, 0.25f, 1f),
                Mathf.Max(0.1f, guardCooldown),
                Mathf.Clamp(guardTriggerHealthRatio, 0.05f, 1f),
                Mathf.Max(0.1f, suppressionDuration),
                Mathf.Clamp(suppressionDamageMultiplier, 0.25f, 1f),
                Mathf.Clamp(suppressionMoveMultiplier, 0.25f, 1f),
                Mathf.Max(0.1f, suppressionCooldown),
                Mathf.Clamp(suppressionTriggerHealthRatio, 0.05f, 1f));
        }
    }
}
