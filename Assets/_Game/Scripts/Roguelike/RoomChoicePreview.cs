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
            : this(
                roomType,
                title,
                threatPreview,
                rewardPreview,
                routeNote,
                RoomModifierType.None,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty)
        {
        }

        public RoomChoicePreview(
            RoomType roomType,
            string title,
            string threatPreview,
            string rewardPreview,
            string routeNote,
            RoomModifierType modifierType,
            string modifierTitle,
            string modifierRiskPreview,
            string modifierRewardPreview,
            string modifierRouteNote)
        {
            RoomType = roomType;
            Title = string.IsNullOrWhiteSpace(title) ? roomType.ToString() : title;
            ThreatPreview = threatPreview ?? string.Empty;
            RewardPreview = rewardPreview ?? string.Empty;
            RouteNote = routeNote ?? string.Empty;
            ModifierType = modifierType;
            ModifierTitle = modifierType == RoomModifierType.None
                ? string.Empty
                : (string.IsNullOrWhiteSpace(modifierTitle) ? modifierType.ToString() : modifierTitle);
            ModifierRiskPreview = modifierRiskPreview ?? string.Empty;
            ModifierRewardPreview = modifierRewardPreview ?? string.Empty;
            ModifierRouteNote = modifierRouteNote ?? string.Empty;
        }

        public RoomType RoomType { get; }
        public string Title { get; }
        public string ThreatPreview { get; }
        public string RewardPreview { get; }
        public string RouteNote { get; }
        public RoomModifierType ModifierType { get; }
        public string ModifierTitle { get; }
        public string ModifierRiskPreview { get; }
        public string ModifierRewardPreview { get; }
        public string ModifierRouteNote { get; }
        public bool HasModifier => ModifierType != RoomModifierType.None;
    }
}
