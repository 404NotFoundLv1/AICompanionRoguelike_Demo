namespace AICompanionRoguelike.Roguelike
{
    public readonly struct RouteMapNode
    {
        public RouteMapNode(
            RoomType roomType,
            string label,
            int stepNumber,
            bool isCompleted,
            bool isCurrent,
            bool isNextChoice,
            bool isBossEndpoint,
            int choiceIndex)
        {
            RoomType = roomType;
            Label = string.IsNullOrWhiteSpace(label) ? roomType.ToString() : label;
            StepNumber = stepNumber;
            IsCompleted = isCompleted;
            IsCurrent = isCurrent;
            IsNextChoice = isNextChoice;
            IsBossEndpoint = isBossEndpoint;
            ChoiceIndex = isNextChoice ? choiceIndex : -1;
        }

        public RoomType RoomType { get; }
        public string Label { get; }
        public int StepNumber { get; }
        public bool IsCompleted { get; }
        public bool IsCurrent { get; }
        public bool IsNextChoice { get; }
        public bool IsBossEndpoint { get; }
        public int ChoiceIndex { get; }
    }
}
