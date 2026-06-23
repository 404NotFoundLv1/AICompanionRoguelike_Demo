using System;
using AICompanionRoguelike.Memory;
using UnityEngine;

namespace AICompanionRoguelike.Companion
{
    [RequireComponent(typeof(CompanionRelationship))]
    public sealed class CompanionRunFeedback : MonoBehaviour
    {
        [SerializeField] private CompanionRelationship relationship;
        [SerializeField] private bool emitOnStart = true;
        [SerializeField] private bool logFeedback = true;

        public static event Action<CompanionRunFeedback, string> FeedbackRaised;

        public CompanionRelationshipProfileSnapshot CurrentProfile
        {
            get
            {
                ResolveRelationship();
                return relationship != null
                    ? CompanionRelationshipProfile.Evaluate(
                        relationship.Trust,
                        relationship.Affection,
                        relationship.MemoryTags)
                    : CompanionRelationshipProfile.Evaluate(
                        50,
                        50,
                        Array.Empty<RelationshipMemoryTagScore>());
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            FeedbackRaised = null;
        }

        private void Awake()
        {
            ResolveRelationship();
        }

        private void Start()
        {
            if (emitOnStart)
            {
                EmitOpeningFeedback();
            }
        }

        public string BuildOpeningFeedback()
        {
            CompanionRelationshipProfileSnapshot profile = CurrentProfile;
            string openingLine = GetToneOpeningLine(profile.Tone);
            if (!profile.HasDominantMemory)
            {
                return openingLine;
            }

            return $"{openingLine} {GetMemoryFollowUp(profile.DominantMemoryTag)}";
        }

        public void EmitOpeningFeedback()
        {
            string line = BuildOpeningFeedback();
            FeedbackRaised?.Invoke(this, line);

            if (logFeedback)
            {
                Debug.Log(line, this);
            }
        }

        private void ResolveRelationship()
        {
            if (relationship == null)
            {
                relationship = GetComponent<CompanionRelationship>();
            }
        }

        private static string GetToneOpeningLine(CompanionDialogueTone tone)
        {
            switch (tone)
            {
                case CompanionDialogueTone.Guarded:
                    return "AI: I'll cover the mission. Keep this precise.";
                case CompanionDialogueTone.Warm:
                    return "AI: Back together. I know your pace.";
                default:
                    return "AI: I'm ready. Keep the signal clear.";
            }
        }

        private static string GetMemoryFollowUp(RelationshipMemoryTag memoryTag)
        {
            switch (memoryTag)
            {
                case RelationshipMemoryTag.Reliable:
                    return "You have been dependable when it matters.";
                case RelationshipMemoryTag.Cold:
                    return "I remember when you kept your distance.";
                case RelationshipMemoryTag.Stubborn:
                    return "Listen when I call the timing.";
                case RelationshipMemoryTag.Protected:
                    return "We have protected each other before.";
                case RelationshipMemoryTag.Abandoned:
                    return "I have not forgotten that you left.";
                case RelationshipMemoryTag.Brave:
                    return "You never backed away from the hard fight.";
                default:
                    return string.Empty;
            }
        }
    }
}
