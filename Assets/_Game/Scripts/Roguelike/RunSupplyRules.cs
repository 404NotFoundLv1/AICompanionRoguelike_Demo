using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public static class RunSupplyRules
    {
        private const int BattleRoomGain = 1;
        private const int EliteRoomGain = 2;
        private const int RewardCost = 2;

        public static int GetSupplyGain(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.BattleRoom:
                    return BattleRoomGain;
                case RoomType.EliteRoom:
                    return EliteRoomGain;
                default:
                    return 0;
            }
        }

        public static int ShopRewardCost()
        {
            return RewardCost;
        }

        public static bool CanAffordShopReward(int currentSupplies)
        {
            return Mathf.Max(0, currentSupplies) >= RewardCost;
        }

        public static int SpendShopReward(int currentSupplies)
        {
            return Mathf.Max(0, currentSupplies - RewardCost);
        }

        public static string BuildSupplyGainLabel(RoomType roomType)
        {
            int gain = GetSupplyGain(roomType);
            return gain > 0 ? $"Supplies +{gain}" : string.Empty;
        }

        public static string BuildShopAffordabilityLabel(int currentSupplies)
        {
            int supplies = Mathf.Max(0, currentSupplies);
            if (CanAffordShopReward(supplies))
            {
                return $"Supplies {supplies} | Cost {RewardCost} | Affordable";
            }

            return $"Supplies {supplies} | Cost {RewardCost} | Need {RewardCost - supplies} more";
        }

        public static string BuildShopSpentLabel(int currentSupplies)
        {
            return $"Spent {RewardCost} supplies. Supplies {Mathf.Max(0, currentSupplies)} remaining.";
        }

        public static string BuildShopBlockedLabel(int currentSupplies)
        {
            int supplies = Mathf.Max(0, currentSupplies);
            return $"Not enough supplies. Need {RewardCost - supplies} more to buy this reward.";
        }
    }
}
