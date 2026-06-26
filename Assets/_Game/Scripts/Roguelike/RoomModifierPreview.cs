namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RoomModifierPreview
    {
        public RoomModifierPreview(
            RoomModifierType modifierType,
            string title,
            string riskPreview,
            string rewardPreview,
            string routeNote)
        {
            ModifierType = modifierType;
            Title = string.IsNullOrWhiteSpace(title) ? modifierType.ToString() : title;
            RiskPreview = riskPreview ?? string.Empty;
            RewardPreview = rewardPreview ?? string.Empty;
            RouteNote = routeNote ?? string.Empty;
        }

        public RoomModifierType ModifierType { get; }
        public string Title { get; }
        public string RiskPreview { get; }
        public string RewardPreview { get; }
        public string RouteNote { get; }
        public bool HasModifier => ModifierType != RoomModifierType.None;
    }
}
