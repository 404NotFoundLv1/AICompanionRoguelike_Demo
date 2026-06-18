namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RunSessionSnapshot
    {
        public RunSessionSnapshot(
            int runId,
            bool isRunActive,
            string startScenePath,
            string battleScenePath,
            RunEndReason endReason)
        {
            RunId = runId;
            IsRunActive = isRunActive;
            StartScenePath = startScenePath;
            BattleScenePath = battleScenePath;
            EndReason = endReason;
        }

        public int RunId { get; }
        public bool IsRunActive { get; }
        public string StartScenePath { get; }
        public string BattleScenePath { get; }
        public RunEndReason EndReason { get; }
    }
}
