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
        {
            RunId = runId;
            EndReason = endReason;
            RoomsCleared = roomsCleared;
            LastRoomNumber = lastRoomNumber;
            LastRoomType = lastRoomType;
            RewardTitles = rewardTitles ?? Array.Empty<string>();
            FinalTrust = finalTrust;
            FinalAffection = finalAffection;
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
        public bool HasSummary => RunId > 0 && EndReason != RunEndReason.None;
        public bool HasRelationship => FinalTrust >= 0 && FinalAffection >= 0;
    }
}
