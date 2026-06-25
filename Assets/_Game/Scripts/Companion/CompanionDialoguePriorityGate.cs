namespace AICompanionRoguelike.Companion
{
    public static class CompanionDialoguePriorityGate
    {
        public static bool ShouldShow(
            int incomingPriority,
            int currentPriority,
            bool bubbleVisible,
            float now,
            float nextAllowedTime)
        {
            int normalizedIncomingPriority = incomingPriority < 0 ? 0 : incomingPriority;
            int normalizedCurrentPriority = currentPriority < 0 ? 0 : currentPriority;

            if (bubbleVisible && normalizedIncomingPriority < normalizedCurrentPriority)
            {
                return false;
            }

            if (bubbleVisible && normalizedIncomingPriority > normalizedCurrentPriority)
            {
                return true;
            }

            if (normalizedIncomingPriority >= 4)
            {
                return true;
            }

            return now >= nextAllowedTime;
        }
    }
}
