using System;

namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RunSessionSummary
    {
        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection)
            : this(
                runId,
                endReason,
                roomsCleared,
                lastRoomNumber,
                lastRoomType,
                rewardTitles,
                finalTrust,
                finalAffection,
                string.Empty,
                0,
                0,
                0,
                0,
                0)
        {
        }

        public RunSessionSummary(
            int runId,
            RunEndReason endReason,
            int roomsCleared,
            int lastRoomNumber,
            RoomType lastRoomType,
            string[] rewardTitles,
            int finalTrust,
            int finalAffection,
            string companionFeedbackLine,
            int companionTrustDelta,
            int companionAffectionDelta,
            int bossSupportActivations,
            int bossWarningHits,
            int bossWarningDodges)
        {
            RunId = runId;
            EndReason = endReason;
            RoomsCleared = roomsCleared;
            LastRoomNumber = lastRoomNumber;
            LastRoomType = lastRoomType;
            RewardTitles = rewardTitles ?? Array.Empty<string>();
            FinalTrust = finalTrust;
            FinalAffection = finalAffection;
            CompanionFeedbackLine = companionFeedbackLine ?? string.Empty;
            CompanionTrustDelta = companionTrustDelta;
            CompanionAffectionDelta = companionAffectionDelta;
            BossSupportActivations = Math.Max(0, bossSupportActivations);
            BossWarningHits = Math.Max(0, bossWarningHits);
            BossWarningDodges = Math.Max(0, bossWarningDodges);
        }

        public static RunSessionSummary Empty =>
            new RunSessionSummary(0, RunEndReason.None, 0, 0, RoomType.BattleRoom, Array.Empty<string>(), -1, -1);

        public int RunId { get; }
        public RunEndReason EndReason { get; }
        public int RoomsCleared { get; }
        public int LastRoomNumber { get; }
        public RoomType LastRoomType { get; }
        public string[] RewardTitles { get; }
        public int FinalTrust { get; }
        public int FinalAffection { get; }
        public string CompanionFeedbackLine { get; }
        public int CompanionTrustDelta { get; }
        public int CompanionAffectionDelta { get; }
        public int BossSupportActivations { get; }
        public int BossWarningHits { get; }
        public int BossWarningDodges { get; }
        public bool HasSummary => RunId > 0 && EndReason != RunEndReason.None;
        public bool HasRelationship => FinalTrust >= 0 && FinalAffection >= 0;
        public bool HasCompanionFeedback => !string.IsNullOrEmpty(CompanionFeedbackLine);
    }
}
