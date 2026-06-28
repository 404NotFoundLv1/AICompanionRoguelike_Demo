using System;
using System.Collections.Generic;
using AICompanionRoguelike.Progression;
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
        private static readonly List<RoomType> currentRoutePath = new List<RoomType>(8);
        private static readonly List<RoomModifierType> currentRouteModifiers = new List<RoomModifierType>(8);
        private static string companionFeedbackLine = string.Empty;
        private static int companionTrustDelta;
        private static int companionAffectionDelta;
        private static int bossSupportActivations;
        private static int bossWarningHits;
        private static int bossWarningDodges;
        private static string growthRouteLabel = string.Empty;
        private static string growthRouteEffectLabel = string.Empty;
        private static int growthRouteSpecializationCount;
        private static int growthRouteLevel;
        private static int metaFragmentsEarned;
        private static int metaFragmentsTotal;

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

        public static void RecordRoomEntered(RoomType roomType, int roomNumber)
        {
            RecordRoomEntered(roomType, roomNumber, RoomModifierType.None);
        }

        public static void RecordRoomEntered(RoomType roomType, int roomNumber, RoomModifierType roomModifier)
        {
            currentRoutePath.Add(roomType);
            currentRouteModifiers.Add(roomModifier);
            lastRoomType = roomType;
            lastRoomNumber = Mathf.Max(lastRoomNumber, roomNumber);
        }

        public static void RecordRewardSelected(string rewardTitle)
        {
            if (string.IsNullOrWhiteSpace(rewardTitle))
            {
                return;
            }

            currentRewardTitles.Add(rewardTitle);
        }

        public static void RecordCompanionBossFeedback(
            string feedbackLine,
            int trustDelta,
            int affectionDelta,
            int supportActivations,
            int warningHits,
            int warningDodges)
        {
            companionFeedbackLine = feedbackLine ?? string.Empty;
            companionTrustDelta = trustDelta;
            companionAffectionDelta = affectionDelta;
            bossSupportActivations = Mathf.Max(0, supportActivations);
            bossWarningHits = Mathf.Max(0, warningHits);
            bossWarningDodges = Mathf.Max(0, warningDodges);
        }

        public static void RecordGrowthRouteSummary(
            string routeLabel,
            string effectLabel,
            int specializationCount)
        {
            RecordGrowthRouteSummary(routeLabel, effectLabel, specializationCount, 0);
        }

        public static void RecordGrowthRouteSummary(
            string routeLabel,
            string effectLabel,
            int specializationCount,
            int routeLevel)
        {
            growthRouteLabel = routeLabel ?? string.Empty;
            growthRouteEffectLabel = effectLabel ?? string.Empty;
            growthRouteSpecializationCount = Mathf.Max(0, specializationCount);
            growthRouteLevel = Mathf.Max(0, routeLevel);
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
            AwardMetaProgression(reason);
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
            currentRoutePath.Clear();
            currentRouteModifiers.Clear();
            companionFeedbackLine = string.Empty;
            companionTrustDelta = 0;
            companionAffectionDelta = 0;
            bossSupportActivations = 0;
            bossWarningHits = 0;
            bossWarningDodges = 0;
            growthRouteLabel = string.Empty;
            growthRouteEffectLabel = string.Empty;
            growthRouteSpecializationCount = 0;
            growthRouteLevel = 0;
            metaFragmentsEarned = 0;
            metaFragmentsTotal = MetaProgressionState.CoreFragments;
        }

        private static void AwardMetaProgression(RunEndReason reason)
        {
            metaFragmentsEarned = MetaProgressionRewardRules.CalculateCoreFragments(
                reason,
                currentRoomsCleared,
                growthRouteLevel,
                growthRouteSpecializationCount);

            if (metaFragmentsEarned > 0)
            {
                MetaProgressionState.AddCoreFragments(metaFragmentsEarned);
            }

            metaFragmentsTotal = MetaProgressionState.CoreFragments;
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
                finalAffection,
                companionFeedbackLine,
                companionTrustDelta,
                companionAffectionDelta,
                bossSupportActivations,
                bossWarningHits,
                bossWarningDodges,
                currentRoutePath.ToArray(),
                currentRouteModifiers.ToArray(),
                growthRouteLabel,
                growthRouteEffectLabel,
                growthRouteSpecializationCount,
                growthRouteLevel,
                metaFragmentsEarned,
                metaFragmentsTotal);
        }
    }
}
