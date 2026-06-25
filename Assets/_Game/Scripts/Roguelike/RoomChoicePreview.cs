namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RoomChoicePreview
    {
        public RoomChoicePreview(
            RoomType roomType,
            string title,
            string threatPreview,
            string rewardPreview,
            string routeNote)
        {
            RoomType = roomType;
            Title = string.IsNullOrWhiteSpace(title) ? roomType.ToString() : title;
            ThreatPreview = threatPreview ?? string.Empty;
            RewardPreview = rewardPreview ?? string.Empty;
            RouteNote = routeNote ?? string.Empty;
        }

        public RoomType RoomType { get; }
        public string Title { get; }
        public string ThreatPreview { get; }
        public string RewardPreview { get; }
        public string RouteNote { get; }
    }
}
