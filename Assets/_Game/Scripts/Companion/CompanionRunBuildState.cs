using System;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionRunBuildState
    {
        private static CompanionSkillTendency currentTendency = CompanionSkillTendency.Balanced;
        private static int guardianUpgradeLevel;
        private static int suppressorUpgradeLevel;
        private static int linkUpgradeLevel;

        public static event Action<CompanionSkillTendency> TendencyChanged;
        public static event Action<CompanionSkillTendency, int> UpgradeChanged;

        public static CompanionSkillTendency CurrentTendency => currentTendency;
        public static bool HasSelectedTendency => currentTendency != CompanionSkillTendency.Balanced;

        public static void SetTendency(CompanionSkillTendency tendency)
        {
            if (currentTendency == tendency)
            {
                return;
            }

            currentTendency = tendency;
            TendencyChanged?.Invoke(currentTendency);
        }

        public static void Reset()
        {
            SetTendency(CompanionSkillTendency.Balanced);
            guardianUpgradeLevel = 0;
            suppressorUpgradeLevel = 0;
            linkUpgradeLevel = 0;
        }

        public static int GetUpgradeLevel(CompanionSkillTendency tendency)
        {
            return tendency switch
            {
                CompanionSkillTendency.Guardian => guardianUpgradeLevel,
                CompanionSkillTendency.Suppressor => suppressorUpgradeLevel,
                CompanionSkillTendency.Link => linkUpgradeLevel,
                _ => 0
            };
        }

        public static int AddUpgrade(CompanionSkillTendency tendency)
        {
            int level;
            switch (tendency)
            {
                case CompanionSkillTendency.Guardian:
                    guardianUpgradeLevel++;
                    level = guardianUpgradeLevel;
                    break;
                case CompanionSkillTendency.Suppressor:
                    suppressorUpgradeLevel++;
                    level = suppressorUpgradeLevel;
                    break;
                case CompanionSkillTendency.Link:
                    linkUpgradeLevel++;
                    level = linkUpgradeLevel;
                    break;
                default:
                    return 0;
            }

            UpgradeChanged?.Invoke(tendency, level);
            return level;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            currentTendency = CompanionSkillTendency.Balanced;
            guardianUpgradeLevel = 0;
            suppressorUpgradeLevel = 0;
            linkUpgradeLevel = 0;
            TendencyChanged = null;
            UpgradeChanged = null;
        }
    }
}
