using System;
using System.Collections.Generic;
using AICompanionRoguelike.Memory;

namespace AICompanionRoguelike.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public string savedAtUtc = string.Empty;
        public RelationshipSaveData relationship = new RelationshipSaveData();

        public void Normalize()
        {
            if (relationship == null)
            {
                relationship = new RelationshipSaveData();
            }

            relationship.Normalize();
        }
    }

    [Serializable]
    public sealed class RelationshipSaveData
    {
        public bool hasData;
        public int trust;
        public int affection;
        public List<RelationshipMemoryTagScore> memoryTags = new List<RelationshipMemoryTagScore>();

        public void Normalize()
        {
            if (memoryTags == null)
            {
                memoryTags = new List<RelationshipMemoryTagScore>();
            }
        }
    }
}
