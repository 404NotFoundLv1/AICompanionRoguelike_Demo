namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RunRewardChoice
    {
        public RunRewardChoice(RunRewardType rewardType, string title, string description)
        {
            RewardType = rewardType;
            Title = title;
            Description = description;
        }

        public RunRewardType RewardType { get; }
        public string Title { get; }
        public string Description { get; }
    }
}
