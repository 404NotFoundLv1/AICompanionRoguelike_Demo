using AICompanionRoguelike.Combat;
using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public static class RunRelicRules
    {
        public const float DefaultFirstAidHealAmount = 8f;
        public const float DefaultSyncMarkDamageMultiplier = 1.25f;
        public const int FieldBackpackPrepareBonusSupplies = 1;

        public static readonly RunRelicType[] AllRelics =
        {
            RunRelicType.FirstAidCharm,
            RunRelicType.SyncMark,
            RunRelicType.FieldBackpack
        };

        public static string GetTitle(RunRelicType relicType)
        {
            switch (relicType)
            {
                case RunRelicType.FirstAidCharm:
                    return "First Aid Charm";
                case RunRelicType.SyncMark:
                    return "Sync Mark";
                case RunRelicType.FieldBackpack:
                    return "Field Backpack";
                default:
                    return relicType.ToString();
            }
        }

        public static string GetDescription(RunRelicType relicType)
        {
            switch (relicType)
            {
                case RunRelicType.FirstAidCharm:
                    return $"Entering combat rooms restores {DefaultFirstAidHealAmount:0} HP.";
                case RunRelicType.SyncMark:
                    return $"Enemies hit by the AI are marked. Player damage against marked enemies x{DefaultSyncMarkDamageMultiplier:0.##}.";
                case RunRelicType.FieldBackpack:
                    return $"Safe-room Prepare grants +{FieldBackpackPrepareBonusSupplies} extra supply.";
                default:
                    return "Unknown relic effect.";
            }
        }

        public static string GetHudLabel(RunRelicType relicType)
        {
            switch (relicType)
            {
                case RunRelicType.FirstAidCharm:
                    return $"First Aid +{DefaultFirstAidHealAmount:0} HP";
                case RunRelicType.SyncMark:
                    return $"Sync Mark x{DefaultSyncMarkDamageMultiplier:0.##}";
                case RunRelicType.FieldBackpack:
                    return $"Backpack Prepare +{FieldBackpackPrepareBonusSupplies}";
                default:
                    return relicType.ToString();
            }
        }

        public static string GetChoicePrefix(RunRelicType relicType)
        {
            return "RELIC";
        }

        public static string GetPickupBanner(RunRelicType relicType)
        {
            return $"Relic acquired: {GetTitle(relicType)}";
        }

        public static string GetEffectBanner(RunRelicType relicType, string detail)
        {
            string suffix = string.IsNullOrWhiteSpace(detail)
                ? GetDescription(relicType)
                : detail;
            return $"{GetTitle(relicType)}: {suffix}";
        }

        public static RunRewardType GetRewardType(RunRelicType relicType)
        {
            switch (relicType)
            {
                case RunRelicType.FirstAidCharm:
                    return RunRewardType.RelicFirstAidCharm;
                case RunRelicType.SyncMark:
                    return RunRewardType.RelicSyncMark;
                case RunRelicType.FieldBackpack:
                    return RunRewardType.RelicFieldBackpack;
                default:
                    return RunRewardType.RelicFirstAidCharm;
            }
        }

        public static bool TryGetRelicType(RunRewardType rewardType, out RunRelicType relicType)
        {
            switch (rewardType)
            {
                case RunRewardType.RelicFirstAidCharm:
                    relicType = RunRelicType.FirstAidCharm;
                    return true;
                case RunRewardType.RelicSyncMark:
                    relicType = RunRelicType.SyncMark;
                    return true;
                case RunRewardType.RelicFieldBackpack:
                    relicType = RunRelicType.FieldBackpack;
                    return true;
                default:
                    relicType = default;
                    return false;
            }
        }

        public static DamageInfo ModifyPlayerOutgoingDamage(
            HealthComponent targetHealth,
            DamageInfo damageInfo,
            bool hasSyncMark,
            float markedDamageMultiplier)
        {
            if (!hasSyncMark
                || targetHealth == null
                || damageInfo.sourceType != DamageSourceType.Player
                || damageInfo.damage <= 0f)
            {
                return damageInfo;
            }

            RelicSyncMarkTarget marker = targetHealth.GetComponent<RelicSyncMarkTarget>();
            if (marker == null || !marker.IsMarked)
            {
                return damageInfo;
            }

            damageInfo.damage *= Mathf.Max(1f, markedDamageMultiplier);
            return damageInfo;
        }
    }
}
