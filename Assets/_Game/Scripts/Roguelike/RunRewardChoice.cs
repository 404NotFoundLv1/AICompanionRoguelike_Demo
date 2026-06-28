namespace AICompanionRoguelike.Roguelike
{
    public enum RunRewardCategory
    {
        Player,
        Companion,
        Counterplay,
        Survival,
        Build
    }

    public readonly struct RunRewardChoice
    {
        public RunRewardChoice(RunRewardType rewardType, string title, string description)
            : this(rewardType, title, description, RunRewardCategory.Player, string.Empty, string.Empty)
        {
        }

        public RunRewardChoice(
            RunRewardType rewardType,
            string title,
            string description,
            RunRewardCategory category,
            string previewLine,
            string growthTag)
        {
            RewardType = rewardType;
            Title = title;
            Description = description;
            Category = category;
            PreviewLine = previewLine ?? string.Empty;
            GrowthTag = growthTag ?? string.Empty;
        }

        public RunRewardType RewardType { get; }
        public string Title { get; }
        public string Description { get; }
        public RunRewardCategory Category { get; }
        public string PreviewLine { get; }
        public string GrowthTag { get; }
        public string CategoryLabel => GetCategoryLabel(Category);

        public static string GetCategoryLabel(RunRewardCategory category)
        {
            switch (category)
            {
                case RunRewardCategory.Player:
                    return "Player";
                case RunRewardCategory.Companion:
                    return "AI";
                case RunRewardCategory.Counterplay:
                    return "Counterplay";
                case RunRewardCategory.Survival:
                    return "Survival";
                case RunRewardCategory.Build:
                    return "Build";
                default:
                    return "Reward";
            }
        }
    }
}
