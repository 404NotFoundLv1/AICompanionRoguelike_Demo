using System.Collections.Generic;
using UnityEngine;

namespace AICompanionRoguelike.Memory
{
    public static class CompanionRelationshipState
    {
        private static readonly List<RelationshipMemoryTagScore> memoryTags = new List<RelationshipMemoryTagScore>(8);

        public static bool HasState { get; private set; }
        public static int Trust { get; private set; }
        public static int Affection { get; private set; }
        public static IReadOnlyList<RelationshipMemoryTagScore> MemoryTags => memoryTags;

        public static void Clear()
        {
            HasState = false;
            Trust = 0;
            Affection = 0;
            memoryTags.Clear();
        }

        public static void SaveFrom(CompanionRelationship relationship)
        {
            if (relationship == null)
            {
                return;
            }

            SaveSnapshot(relationship.Trust, relationship.Affection, relationship.MemoryTags);
        }

        public static void SaveSnapshot(
            int trust,
            int affection,
            IReadOnlyList<RelationshipMemoryTagScore> tags)
        {
            HasState = true;
            Trust = ClampRelationshipValue(trust);
            Affection = ClampRelationshipValue(affection);
            ReplaceMemoryTags(tags);
        }

        public static bool TryApplyTo(CompanionRelationship relationship)
        {
            if (!HasState || relationship == null)
            {
                return false;
            }

            relationship.SetRelationshipSnapshot(Trust, Affection, memoryTags, updateSessionState: false);
            return true;
        }

        private static void ReplaceMemoryTags(IReadOnlyList<RelationshipMemoryTagScore> tags)
        {
            memoryTags.Clear();
            if (tags == null)
            {
                return;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                RelationshipMemoryTagScore tagScore = tags[i];
                AddMemoryTagScore(tagScore.tag, tagScore.score);
            }
        }

        private static void AddMemoryTagScore(RelationshipMemoryTag tag, int score)
        {
            if (score <= 0)
            {
                return;
            }

            int index = FindMemoryTagIndex(tag);
            if (index < 0)
            {
                memoryTags.Add(new RelationshipMemoryTagScore
                {
                    tag = tag,
                    score = score
                });
                return;
            }

            RelationshipMemoryTagScore entry = memoryTags[index];
            entry.score += score;
            memoryTags[index] = entry;
        }

        private static int FindMemoryTagIndex(RelationshipMemoryTag tag)
        {
            for (int i = 0; i < memoryTags.Count; i++)
            {
                if (memoryTags[i].tag == tag)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ClampRelationshipValue(int value)
        {
            return Mathf.Clamp(value, 0, 100);
        }
    }
}
