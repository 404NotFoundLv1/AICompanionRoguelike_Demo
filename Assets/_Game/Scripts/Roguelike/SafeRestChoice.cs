namespace AICompanionRoguelike.Roguelike
{
    public enum SafeRestChoiceType
    {
        Rest,
        Talk,
        Prepare
    }

    public readonly struct SafeRestChoice
    {
        public SafeRestChoice(
            SafeRestChoiceType choiceType,
            string title,
            string description,
            string previewLine)
        {
            ChoiceType = choiceType;
            Title = title;
            Description = description;
            PreviewLine = previewLine;
        }

        public SafeRestChoiceType ChoiceType { get; }
        public string Title { get; }
        public string Description { get; }
        public string PreviewLine { get; }
    }
}
