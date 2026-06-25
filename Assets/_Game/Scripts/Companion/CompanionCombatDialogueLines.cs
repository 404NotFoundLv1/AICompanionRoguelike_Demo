using AICompanionRoguelike.Memory;

namespace AICompanionRoguelike.Companion
{
    public static class CompanionCombatDialogueLines
    {
        public static string BuildLine(
            CompanionDialogueEventType eventType,
            CompanionRelationshipProfileSnapshot profile)
        {
            switch (eventType)
            {
                case CompanionDialogueEventType.RoomStarted:
                    return BuildRoomStartedLine(profile);
                case CompanionDialogueEventType.RoomCleared:
                    return BuildRoomClearedLine(profile);
                case CompanionDialogueEventType.QTEStarted:
                    return BuildQteStartedLine(profile);
                case CompanionDialogueEventType.QTESuccess:
                    return BuildQteSuccessLine(profile);
                case CompanionDialogueEventType.QTEWrongInput:
                    return BuildQteWrongInputLine(profile);
                case CompanionDialogueEventType.QTEIgnored:
                    return BuildQteIgnoredLine(profile);
                case CompanionDialogueEventType.PlayerHit:
                    return BuildPlayerHitLine(profile);
                case CompanionDialogueEventType.PlayerLowHealth:
                    return BuildPlayerLowHealthLine(profile);
                case CompanionDialogueEventType.TacticalGuard:
                    return BuildTacticalGuardLine(profile);
                case CompanionDialogueEventType.TacticalSuppression:
                    return BuildTacticalSuppressionLine(profile);
                case CompanionDialogueEventType.BossSupportActivated:
                    return "AI: Shield link is active. Take the opening.";
                case CompanionDialogueEventType.BossSupportBlocked:
                    return "AI: I can warn you, but I cannot shield this one.";
                default:
                    return BuildOpeningLine(profile);
            }
        }

        private static string BuildOpeningLine(CompanionRelationshipProfileSnapshot profile)
        {
            switch (profile.Tone)
            {
                case CompanionDialogueTone.Guarded:
                    return "AI: I'll cover the mission. Keep this precise.";
                case CompanionDialogueTone.Warm:
                    return AddMemoryFollowUp(
                        "AI: Back together. I know your pace.",
                        profile);
                default:
                    return AddMemoryFollowUp(
                        "AI: I'm ready. Keep the signal clear.",
                        profile);
            }
        }

        private static string BuildRoomStartedLine(CompanionRelationshipProfileSnapshot profile)
        {
            switch (profile.Tier)
            {
                case CompanionBondTier.Distant:
                    return "AI: New room. I will keep my distance and cover angles.";
                case CompanionBondTier.Synchronized:
                    return "AI: New room. We move on your first strike.";
                default:
                    return "AI: New room. I am with you.";
            }
        }

        private static string BuildRoomClearedLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Brave)
            {
                return "AI: Room clear. You never backed away from the hard fight.";
            }

            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Reliable)
            {
                return "AI: Room clear. Dependable, just like before.";
            }

            return profile.Tier == CompanionBondTier.Synchronized
                ? "AI: Room clear. Clean rhythm."
                : "AI: Room clear. Reset before the next door.";
        }

        private static string BuildQteStartedLine(CompanionRelationshipProfileSnapshot profile)
        {
            switch (profile.Tone)
            {
                case CompanionDialogueTone.Guarded:
                    return "AI: Finisher window. Hit the called skill.";
                case CompanionDialogueTone.Warm:
                    return "AI: I've got your angle. Answer my call.";
                default:
                    return "AI: Finisher window open. Answer me.";
            }
        }

        private static string BuildQteSuccessLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory)
            {
                switch (profile.DominantMemoryTag)
                {
                    case RelationshipMemoryTag.Reliable:
                        return "AI: Good sync. You stayed dependable when it mattered.";
                    case RelationshipMemoryTag.Brave:
                        return "AI: Good sync. You took the hard opening and made it ours.";
                    case RelationshipMemoryTag.Protected:
                        return "AI: Good sync. We protected the line together.";
                }
            }

            return profile.Tier == CompanionBondTier.Synchronized
                ? "AI: Good sync. That was our timing."
                : "AI: Good sync. Target is open.";
        }

        private static string BuildQteWrongInputLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Stubborn)
            {
                return "AI: Wrong skill. Listen to the call next time.";
            }

            return profile.Tone == CompanionDialogueTone.Guarded
                ? "AI: That was not the cue."
                : "AI: Wrong skill. Reset with me.";
        }

        private static string BuildQteIgnoredLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Cold)
            {
                return "AI: Silence again. I noticed.";
            }

            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Abandoned)
            {
                return "AI: You left the call unanswered. I remember that.";
            }

            return profile.Tone == CompanionDialogueTone.Warm
                ? "AI: You missed me. Reset."
                : "AI: QTE missed. Keep moving.";
        }

        private static string BuildPlayerHitLine(CompanionRelationshipProfileSnapshot profile)
        {
            return profile.Tone == CompanionDialogueTone.Warm
                ? "AI: I saw that hit. Stay close."
                : "AI: You took a hit. Create space.";
        }

        private static string BuildPlayerLowHealthLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Protected)
            {
                return "AI: Stay behind me. We have protected each other before.";
            }

            switch (profile.Tone)
            {
                case CompanionDialogueTone.Guarded:
                    return "AI: Critical health. Stop trading hits.";
                case CompanionDialogueTone.Warm:
                    return "AI: You're hurt. I am with you.";
                default:
                    return "AI: Low health. Make space now.";
            }
        }

        private static string BuildTacticalGuardLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Protected)
            {
                return "AI: Guard up. Stay behind me, like before.";
            }

            return profile.Tier == CompanionBondTier.Distant
                ? "AI: Weak guard up. Do not waste it."
                : "AI: Guard up. Breathe and reset.";
        }

        private static string BuildTacticalSuppressionLine(CompanionRelationshipProfileSnapshot profile)
        {
            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Brave)
            {
                return "AI: I will pin it down. Take the brave opening.";
            }

            if (profile.HasDominantMemory && profile.DominantMemoryTag == RelationshipMemoryTag.Reliable)
            {
                return "AI: Suppressing target. Dependable timing, now.";
            }

            return profile.Tier == CompanionBondTier.Synchronized
                ? "AI: Suppression on target. Your turn."
                : "AI: I can slow it. Find your hit.";
        }

        private static string AddMemoryFollowUp(
            string line,
            CompanionRelationshipProfileSnapshot profile)
        {
            if (!profile.HasDominantMemory)
            {
                return line;
            }

            switch (profile.DominantMemoryTag)
            {
                case RelationshipMemoryTag.Reliable:
                    return $"{line} You have been dependable when it matters.";
                case RelationshipMemoryTag.Cold:
                    return $"{line} I remember when you kept your distance.";
                case RelationshipMemoryTag.Stubborn:
                    return $"{line} Listen when I call the timing.";
                case RelationshipMemoryTag.Protected:
                    return $"{line} We have protected each other before.";
                case RelationshipMemoryTag.Abandoned:
                    return $"{line} I have not forgotten that you left.";
                case RelationshipMemoryTag.Brave:
                    return $"{line} You never backed away from the hard fight.";
                default:
                    return line;
            }
        }
    }
}
