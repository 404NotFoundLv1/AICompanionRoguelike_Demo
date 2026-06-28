using AICompanionRoguelike.Roguelike;
using UnityEngine;

namespace AICompanionRoguelike.Progression
{
    public static class MetaProgressionRewardRules
    {
        public static int CalculateCoreFragments(
            RunEndReason reason,
            int roomsCleared,
            int growthRouteLevel,
            int routeSpecializationCount)
        {
            int fragments = Mathf.Max(0, roomsCleared) * 3;

            if (reason == RunEndReason.Victory)
            {
                fragments += 8;
            }

            fragments += Mathf.Max(0, growthRouteLevel) * 2;
            fragments += Mathf.Max(0, routeSpecializationCount) * 2;
            return Mathf.Max(0, fragments);
        }
    }
}
