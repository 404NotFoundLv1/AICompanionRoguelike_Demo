using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Roguelike
{
    public static class RunSessionState
    {
        private static string startScenePath;
        private static string battleScenePath;

        public static event Action<RunSessionSnapshot> RunStarted;
        public static event Action<RunSessionSnapshot> RunEnded;

        public static int CurrentRunId { get; private set; }
        public static bool IsRunActive { get; private set; }
        public static RunEndReason LastEndReason { get; private set; } = RunEndReason.None;

        public static RunSessionSnapshot CurrentSnapshot =>
            new RunSessionSnapshot(CurrentRunId, IsRunActive, startScenePath, battleScenePath, LastEndReason);

        public static void StartRunFromHome(string targetBattleScenePath)
        {
            CurrentRunId++;
            IsRunActive = true;
            LastEndReason = RunEndReason.None;
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
            startScenePath = SceneManager.GetActiveScene().path;
            battleScenePath = currentBattleScenePath;

            Debug.Log($"Run session #{CurrentRunId} started directly from battle scene: {battleScenePath}");
            RunStarted?.Invoke(CurrentSnapshot);
        }

        public static void EndRun(RunEndReason reason)
        {
            if (!IsRunActive)
            {
                LastEndReason = reason;
                return;
            }

            IsRunActive = false;
            LastEndReason = reason;

            Debug.Log($"Run session #{CurrentRunId} ended. Reason: {reason}");
            RunEnded?.Invoke(CurrentSnapshot);
        }
    }
}
