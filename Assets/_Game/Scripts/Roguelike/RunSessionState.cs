using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Roguelike
{
    public static class RunSessionState
    {
        private static string startScenePath;
        private static string battleScenePath;
        private static int currentRoomsCleared;
        private static int lastRoomNumber;
        private static RoomType lastRoomType = RoomType.BattleRoom;
        private static readonly List<string> currentRewardTitles = new List<string>(8);

        public static event Action<RunSessionSnapshot> RunStarted;
        public static event Action<RunSessionSnapshot> RunEnded;

        public static int CurrentRunId { get; private set; }
        public static bool IsRunActive { get; private set; }
        public static RunEndReason LastEndReason { get; private set; } = RunEndReason.None;
        public static RunSessionSummary LastSummary { get; private set; } = RunSessionSummary.Empty;

        public static RunSessionSnapshot CurrentSnapshot =>
            new RunSessionSnapshot(CurrentRunId, IsRunActive, startScenePath, battleScenePath, LastEndReason);

        public static void StartRunFromHome(string targetBattleScenePath)
        {
            CurrentRunId++;
            IsRunActive = true;
            LastEndReason = RunEndReason.None;
            ResetCurrentRunProgress();
            startScenePath = SceneManager.GetActiveScene().path;
            battleScenePath = targetBattleScenePath;

            Debug.Log($"Run session #{CurrentRunId} started from home. Battle scene: {battleScenePath}");
            RunStarted?.Invoke(CurrentSnapshot);
        }

        public static void EnsureRunStartedFromBattleScene(string currentBattleScenePath)
        {
            if (IsRunActive)
            {
                return;
            }

            CurrentRunId++;
            IsRunActive = true;
            LastEndReason = RunEndReason.None;
            ResetCurrentRunProgress();
            startScenePath = SceneManager.GetActiveScene().path;
            battleScenePath = currentBattleScenePath;

            Debug.Log($"Run session #{CurrentRunId} started directly from battle scene: {battleScenePath}");
            RunStarted?.Invoke(CurrentSnapshot);
        }

        public static void RecordRoomCleared(RoomType roomType, int roomNumber)
        {
            lastRoomType = roomType;
            lastRoomNumber = Mathf.Max(lastRoomNumber, roomNumber);
            currentRoomsCleared = Mathf.Max(currentRoomsCleared, roomNumber);
        }

        public static void RecordRewardSelected(string rewardTitle)
        {
            if (string.IsNullOrWhiteSpace(rewardTitle))
            {
                return;
            }

            currentRewardTitles.Add(rewardTitle);
        }

        public static void EndRun(RunEndReason reason, int finalTrust = -1, int finalAffection = -1)
        {
            if (!IsRunActive)
            {
                LastEndReason = reason;
                LastSummary = CreateSummary(reason, finalTrust, finalAffection);
                return;
            }

            IsRunActive = false;
            LastEndReason = reason;
            LastSummary = CreateSummary(reason, finalTrust, finalAffection);

            Debug.Log($"Run session #{CurrentRunId} ended. Reason: {reason}");
            RunEnded?.Invoke(CurrentSnapshot);
        }

        private static void ResetCurrentRunProgress()
        {
            currentRoomsCleared = 0;
            lastRoomNumber = 0;
            lastRoomType = RoomType.BattleRoom;
            currentRewardTitles.Clear();
        }

        private static RunSessionSummary CreateSummary(RunEndReason reason, int finalTrust, int finalAffection)
        {
            return new RunSessionSummary(
                CurrentRunId,
                reason,
                currentRoomsCleared,
                lastRoomNumber,
                lastRoomType,
                currentRewardTitles.ToArray(),
                finalTrust,
                finalAffection);
        }
    }
}
