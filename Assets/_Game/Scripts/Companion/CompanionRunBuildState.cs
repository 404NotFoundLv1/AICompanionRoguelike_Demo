using System;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionRunBuildState
    {
        private static CompanionSkillTendency currentTendency = CompanionSkillTendency.Balanced;

        public static event Action<CompanionSkillTendency> TendencyChanged;

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
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            currentTendency = CompanionSkillTendency.Balanced;
            TendencyChanged = null;
        }
    }
}
