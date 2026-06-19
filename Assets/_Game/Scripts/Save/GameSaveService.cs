using System;
using AICompanionRoguelike.Memory;

namespace AICompanionRoguelike.Save
{
    public sealed class GameSaveService
    {
        private readonly JsonGameSaveStore store;

        public GameSaveService(JsonGameSaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public string FilePath => store.FilePath;

        public GameSaveLoadResult LoadIntoSession()
        {
            GameSaveLoadResult result = store.Load();
            if (!result.IsLoaded)
            {
                return result;
            }

            RelationshipSaveData relationship = result.Data.relationship;
            if (relationship != null && relationship.hasData)
            {
                CompanionRelationshipState.RestoreSnapshot(
                    relationship.trust,
                    relationship.affection,
                    relationship.memoryTags);
            }

            return result;
        }

        public bool SaveSession()
        {
            if (!CompanionRelationshipState.HasState)
            {
                return false;
            }

            GameSaveData data = new GameSaveData
            {
                saveVersion = GameSaveData.CurrentVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                relationship = CaptureRelationship()
            };

            store.Save(data);
            return true;
        }

        public bool Delete()
        {
            return store.Delete();
        }

        private static RelationshipSaveData CaptureRelationship()
        {
            RelationshipSaveData relationship = new RelationshipSaveData
            {
                hasData = true,
                trust = CompanionRelationshipState.Trust,
                affection = CompanionRelationshipState.Affection
            };

            for (int i = 0; i < CompanionRelationshipState.MemoryTags.Count; i++)
            {
                relationship.memoryTags.Add(CompanionRelationshipState.MemoryTags[i]);
            }

            return relationship;
        }
    }
}
