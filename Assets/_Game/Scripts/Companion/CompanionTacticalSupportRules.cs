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
            float suppressionDuration,
            float suppressionDamageMultiplier,
            float suppressionMoveMultiplier,
            float suppressionCooldown)
        {
            GuardDuration = guardDuration;
            GuardDamageMultiplier = guardDamageMultiplier;
            GuardCooldown = guardCooldown;
            SuppressionDuration = suppressionDuration;
            SuppressionDamageMultiplier = suppressionDamageMultiplier;
            SuppressionMoveMultiplier = suppressionMoveMultiplier;
            SuppressionCooldown = suppressionCooldown;
        }

        public float GuardDuration { get; }
        public float GuardDamageMultiplier { get; }
        public float GuardCooldown { get; }
        public float SuppressionDuration { get; }
        public float SuppressionDamageMultiplier { get; }
        public float SuppressionMoveMultiplier { get; }
        public float SuppressionCooldown { get; }
    }

    public static class CompanionTacticalSupportRules
    {
        public static CompanionTacticalSupportTuning Evaluate(CompanionRelationshipProfileSnapshot profile)
        {
            float guardDuration;
            float guardDamageMultiplier;
            float guardCooldown;
            float suppressionDuration;
            float suppressionDamageMultiplier;
            float suppressionMoveMultiplier;
            float suppressionCooldown;

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

            return new CompanionTacticalSupportTuning(
                Mathf.Max(0.1f, guardDuration),
                Mathf.Clamp(guardDamageMultiplier, 0.25f, 1f),
                Mathf.Max(0.1f, guardCooldown),
                Mathf.Max(0.1f, suppressionDuration),
                Mathf.Clamp(suppressionDamageMultiplier, 0.25f, 1f),
                Mathf.Clamp(suppressionMoveMultiplier, 0.25f, 1f),
                Mathf.Max(0.1f, suppressionCooldown));
        }
    }
}
