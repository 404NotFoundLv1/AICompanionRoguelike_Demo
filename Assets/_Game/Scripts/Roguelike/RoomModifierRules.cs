using AICompanionRoguelike.Memory;
using UnityEngine;

namespace AICompanionRoguelike.Roguelike
{
    public static class RoomModifierRules
    {
        public static RoomModifierType GetModifierForChoice(RoomType roomType, int choiceIndex, bool useRoomModifiers)
        {
            if (!useRoomModifiers || roomType == RoomType.BossRoom || roomType == RoomType.BranchEventRoom)
            {
                return RoomModifierType.None;
            }

            switch (roomType)
            {
                case RoomType.BattleRoom:
                    return choiceIndex % 2 == 0 ? RoomModifierType.Reinforced : RoomModifierType.Ambush;
                case RoomType.EliteRoom:
                    return RoomModifierType.Ambush;
                case RoomType.SafeRoom:
                    return RoomModifierType.Recovery;
                case RoomType.ShopRoom:
                    return RoomModifierType.BondSignal;
                default:
                    return RoomModifierType.None;
            }
        }

        public static RoomModifierPreview CreatePreview(RoomType roomType, RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return new RoomModifierPreview(
                        modifierType,
                        "Reinforced",
                        "Risk: enemies have more HP and hit harder.",
                        "Reward: reward draft gains +1 option.",
                        "Modifier: take a harder fight for a better reward draft.");
                case RoomModifierType.Ambush:
                    return new RoomModifierPreview(
                        modifierType,
                        "Ambush",
                        "Risk: one extra enemy joins the room.",
                        "Reward: reward draft gains +1 option.",
                        "Modifier: survive a crowded fight for more choice.");
                case RoomModifierType.Recovery:
                    return new RoomModifierPreview(
                        modifierType,
                        "Recovery",
                        "Risk: no combat risk.",
                        "Reward: healing is increased by 50%.",
                        "Modifier: use the safe room to stabilize before the next route.");
                case RoomModifierType.BondSignal:
                    return new RoomModifierPreview(
                        modifierType,
                        "Bond Signal",
                        "Risk: no combat risk.",
                        "Reward: AI trust and affection increase, and reward draft gains +1 option.",
                        "Modifier: pause to strengthen the AI bond before moving on.");
                default:
                    return new RoomModifierPreview(
                        RoomModifierType.None,
                        "No Modifier",
                        BuildDefaultRisk(roomType),
                        BuildDefaultReward(roomType),
                        "Modifier: standard room behavior.");
            }
        }

        public static int GetBonusRewardChoices(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                case RoomModifierType.Ambush:
                case RoomModifierType.BondSignal:
                    return 1;
                default:
                    return 0;
            }
        }

        public static int GetExtraEnemyCount(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.Ambush ? 1 : 0;
        }

        public static float GetEnemyHealthMultiplier(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.Reinforced ? 1.35f : 1f;
        }

        public static float GetEnemyDamageMultiplier(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.Reinforced ? 1.15f : 1f;
        }

        public static float GetEnemyScaleMultiplier(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.Reinforced ? 1.08f : 1f;
        }

        public static Color GetEnemyTint(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return new Color(1f, 0.62f, 0.38f, 1f);
                case RoomModifierType.Ambush:
                    return new Color(0.95f, 0.48f, 0.88f, 1f);
                default:
                    return Color.white;
            }
        }

        public static Color GetFeedbackColor(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return new Color(1f, 0.58f, 0.24f, 1f);
                case RoomModifierType.Ambush:
                    return new Color(0.84f, 0.42f, 1f, 1f);
                case RoomModifierType.Recovery:
                    return new Color(0.36f, 0.92f, 0.54f, 1f);
                case RoomModifierType.BondSignal:
                    return new Color(0.45f, 0.82f, 1f, 1f);
                default:
                    return Color.white;
            }
        }

        public static string GetFeedbackTitle(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return "Reinforced";
                case RoomModifierType.Ambush:
                    return "Ambush";
                case RoomModifierType.Recovery:
                    return "Recovery";
                case RoomModifierType.BondSignal:
                    return "Bond Signal";
                default:
                    return string.Empty;
            }
        }

        public static string GetReadableVisualHint(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return "orange marker above reinforced enemies";
                case RoomModifierType.Ambush:
                    return "violet marker above ambush enemies";
                case RoomModifierType.Recovery:
                    return "green healing field in the safe room";
                case RoomModifierType.BondSignal:
                    return "blue bond pulse around the AI companion";
                default:
                    return string.Empty;
            }
        }

        public static string BuildEntryFeedbackLine(RoomModifierType modifierType, float restoredHealth)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return "Enemies now show an orange Reinforced marker and trade higher danger for +1 reward option.";
                case RoomModifierType.Ambush:
                    return "Ambush enemies show violet markers, with one extra enemy joining from the side lane for +1 reward option.";
                case RoomModifierType.Recovery:
                    return "A green healing field marks the rest point. Resting here restores 50% more HP.";
                case RoomModifierType.BondSignal:
                    return $"The AI caught a Bond Signal pulse. Trust +{GetTrustDelta(modifierType)}, Affection +{GetAffectionDelta(modifierType)}, reward draft +1 option.";
                default:
                    return string.Empty;
            }
        }

        public static string BuildCompanionFeedbackLine(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.BondSignal:
                    return $"Bond Signal received. Trust +{GetTrustDelta(modifierType)} / Affection +{GetAffectionDelta(modifierType)}.";
                default:
                    return string.Empty;
            }
        }

        public static float GetSafeHealMultiplier(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.Recovery ? 1.5f : 1f;
        }

        public static int GetTrustDelta(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.BondSignal ? 1 : 0;
        }

        public static int GetAffectionDelta(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.BondSignal ? 1 : 0;
        }

        public static RelationshipMemoryTag GetMemoryTag(RoomModifierType modifierType)
        {
            return modifierType == RoomModifierType.BondSignal
                ? RelationshipMemoryTag.Reliable
                : RelationshipMemoryTag.Brave;
        }

        public static string GetShortLabel(RoomModifierType modifierType)
        {
            switch (modifierType)
            {
                case RoomModifierType.Reinforced:
                    return "Reinforced";
                case RoomModifierType.Ambush:
                    return "Ambush";
                case RoomModifierType.Recovery:
                    return "Recovery";
                case RoomModifierType.BondSignal:
                    return "Bond";
                default:
                    return string.Empty;
            }
        }

        public static string FormatRoomWithModifier(string roomLabel, RoomModifierType modifierType)
        {
            string modifierLabel = GetShortLabel(modifierType);
            return string.IsNullOrWhiteSpace(modifierLabel)
                ? roomLabel
                : $"{roomLabel}+{modifierLabel}";
        }

        private static string BuildDefaultRisk(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.SafeRoom:
                case RoomType.ShopRoom:
                    return "Risk: safe, no enemies.";
                case RoomType.BossRoom:
                    return "Risk: final boss.";
                case RoomType.EliteRoom:
                    return "Risk: stronger enemy group.";
                default:
                    return "Risk: normal enemy group.";
            }
        }

        private static string BuildDefaultReward(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.SafeRoom:
                    return "Reward: recover health.";
                case RoomType.BossRoom:
                    return "Reward: complete the run.";
                default:
                    return "Reward: standard reward draft.";
            }
        }
    }
}
