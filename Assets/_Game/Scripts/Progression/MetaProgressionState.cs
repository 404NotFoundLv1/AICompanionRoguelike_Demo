using System;
using UnityEngine;

namespace AICompanionRoguelike.Progression
{
    public enum MetaUpgradeType
    {
        PlayerMaxHealth,
        PlayerDamage,
        CompanionCooldown
    }

    public readonly struct MetaProgressionSnapshot
    {
        public MetaProgressionSnapshot(
            int coreFragments,
            int playerMaxHealthLevel,
            int playerDamageLevel,
            int companionCooldownLevel)
        {
            CoreFragments = Mathf.Max(0, coreFragments);
            PlayerMaxHealthLevel = Mathf.Max(0, playerMaxHealthLevel);
            PlayerDamageLevel = Mathf.Max(0, playerDamageLevel);
            CompanionCooldownLevel = Mathf.Max(0, companionCooldownLevel);
        }

        public int CoreFragments { get; }
        public int PlayerMaxHealthLevel { get; }
        public int PlayerDamageLevel { get; }
        public int CompanionCooldownLevel { get; }
    }

    public static class MetaProgressionState
    {
        public static event Action StateChanged;

        public static bool HasState { get; private set; }
        public static int CoreFragments { get; private set; }
        public static int PlayerMaxHealthLevel { get; private set; }
        public static int PlayerDamageLevel { get; private set; }
        public static int CompanionCooldownLevel { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HasState = false;
            CoreFragments = 0;
            PlayerMaxHealthLevel = 0;
            PlayerDamageLevel = 0;
            CompanionCooldownLevel = 0;
            StateChanged = null;
        }

        public static void Clear()
        {
            HasState = false;
            CoreFragments = 0;
            PlayerMaxHealthLevel = 0;
            PlayerDamageLevel = 0;
            CompanionCooldownLevel = 0;
        }

        public static void RestoreSnapshot(
            int coreFragments,
            int playerMaxHealthLevel,
            int playerDamageLevel,
            int companionCooldownLevel)
        {
            ApplySnapshot(
                coreFragments,
                playerMaxHealthLevel,
                playerDamageLevel,
                companionCooldownLevel,
                notify: false);
        }

        public static void SaveSnapshot(
            int coreFragments,
            int playerMaxHealthLevel,
            int playerDamageLevel,
            int companionCooldownLevel)
        {
            ApplySnapshot(
                coreFragments,
                playerMaxHealthLevel,
                playerDamageLevel,
                companionCooldownLevel,
                notify: true);
        }

        public static MetaProgressionSnapshot CreateSnapshot()
        {
            return new MetaProgressionSnapshot(
                CoreFragments,
                PlayerMaxHealthLevel,
                PlayerDamageLevel,
                CompanionCooldownLevel);
        }

        public static void AddCoreFragments(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CoreFragments = Mathf.Max(0, CoreFragments + amount);
            HasState = true;
            StateChanged?.Invoke();
        }

        public static int GetUpgradeLevel(MetaUpgradeType type)
        {
            switch (type)
            {
                case MetaUpgradeType.PlayerMaxHealth:
                    return PlayerMaxHealthLevel;
                case MetaUpgradeType.PlayerDamage:
                    return PlayerDamageLevel;
                case MetaUpgradeType.CompanionCooldown:
                    return CompanionCooldownLevel;
                default:
                    return 0;
            }
        }

        public static int GetUpgradeCost(MetaUpgradeType type)
        {
            return 6 + GetUpgradeLevel(type) * 4;
        }

        public static bool TryPurchaseUpgrade(MetaUpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            if (CoreFragments < cost)
            {
                return false;
            }

            CoreFragments -= cost;
            switch (type)
            {
                case MetaUpgradeType.PlayerMaxHealth:
                    PlayerMaxHealthLevel++;
                    break;
                case MetaUpgradeType.PlayerDamage:
                    PlayerDamageLevel++;
                    break;
                case MetaUpgradeType.CompanionCooldown:
                    CompanionCooldownLevel++;
                    break;
            }

            HasState = true;
            StateChanged?.Invoke();
            return true;
        }

        public static string GetUpgradeDisplayName(MetaUpgradeType type)
        {
            switch (type)
            {
                case MetaUpgradeType.PlayerMaxHealth:
                    return "Player Max HP";
                case MetaUpgradeType.PlayerDamage:
                    return "Player Damage";
                case MetaUpgradeType.CompanionCooldown:
                    return "AI Support Cooldown";
                default:
                    return type.ToString();
            }
        }

        private static void ApplySnapshot(
            int coreFragments,
            int playerMaxHealthLevel,
            int playerDamageLevel,
            int companionCooldownLevel,
            bool notify)
        {
            CoreFragments = Mathf.Max(0, coreFragments);
            PlayerMaxHealthLevel = Mathf.Max(0, playerMaxHealthLevel);
            PlayerDamageLevel = Mathf.Max(0, playerDamageLevel);
            CompanionCooldownLevel = Mathf.Max(0, companionCooldownLevel);
            HasState = true;

            if (notify)
            {
                StateChanged?.Invoke();
            }
        }
    }
}
