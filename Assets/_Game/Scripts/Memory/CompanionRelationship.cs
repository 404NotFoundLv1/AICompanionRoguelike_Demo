using System;
using System.Collections.Generic;
using AICompanionRoguelike.QTE;
using UnityEngine;

namespace AICompanionRoguelike.Memory
{
    public enum RelationshipMemoryTag
    {
        Reliable,
        Cold,
        Stubborn,
        Protected,
        Abandoned,
        Brave
    }

    [Serializable]
    public struct RelationshipMemoryTagScore
    {
        public RelationshipMemoryTag tag;
        public int score;
    }

    public readonly struct RelationshipChange
    {
        public readonly QTEResultType sourceResult;
        public readonly string sourceLabel;
        public readonly int trustDelta;
        public readonly int affectionDelta;
        public readonly int previousTrust;
        public readonly int previousAffection;
        public readonly int currentTrust;
        public readonly int currentAffection;
        public readonly RelationshipMemoryTag memoryTag;

        public RelationshipChange(
            QTEResultType sourceResult,
            int trustDelta,
            int affectionDelta,
            int previousTrust,
            int previousAffection,
            int currentTrust,
            int currentAffection,
            RelationshipMemoryTag memoryTag)
        {
            this.sourceResult = sourceResult;
            sourceLabel = sourceResult.ToString();
            this.trustDelta = trustDelta;
            this.affectionDelta = affectionDelta;
            this.previousTrust = previousTrust;
            this.previousAffection = previousAffection;
            this.currentTrust = currentTrust;
            this.currentAffection = currentAffection;
            this.memoryTag = memoryTag;
        }

        public RelationshipChange(
            string sourceLabel,
            int trustDelta,
            int affectionDelta,
            int previousTrust,
            int previousAffection,
            int currentTrust,
            int currentAffection,
            RelationshipMemoryTag memoryTag)
        {
            sourceResult = default(QTEResultType);
            this.sourceLabel = sourceLabel;
            this.trustDelta = trustDelta;
            this.affectionDelta = affectionDelta;
            this.previousTrust = previousTrust;
            this.previousAffection = previousAffection;
            this.currentTrust = currentTrust;
            this.currentAffection = currentAffection;
            this.memoryTag = memoryTag;
        }
    }

    public sealed class CompanionRelationship : MonoBehaviour
    {
        [Header("Initial Values")]
        [SerializeField, Range(0, 100)] private int initialTrust = 50;
        [SerializeField, Range(0, 100)] private int initialAffection = 50;

        [Header("QTE Changes")]
        [SerializeField] private int successTrustDelta = 5;
        [SerializeField] private int successAffectionDelta = 2;
        [SerializeField] private int wrongInputTrustDelta = -1;
        [SerializeField] private int wrongInputAffectionDelta = -1;
        [SerializeField] private int ignoredTrustDelta;
        [SerializeField] private int ignoredAffectionDelta = -3;

        [Header("Debug")]
        [SerializeField] private bool logRelationshipChanges = true;

        private readonly List<RelationshipMemoryTagScore> memoryTags = new List<RelationshipMemoryTagScore>(4);

        private QTEManager subscribedManager;
        private int trust;
        private int affection;

        public static event Action<CompanionRelationship, RelationshipChange> AnyRelationshipChanged;
        public event Action<CompanionRelationship, RelationshipChange> RelationshipChanged;

        public int Trust => trust;
        public int Affection => affection;
        public IReadOnlyList<RelationshipMemoryTagScore> MemoryTags => memoryTags;

        private void Awake()
        {
            trust = ClampRelationshipValue(initialTrust);
            affection = ClampRelationshipValue(initialAffection);
        }

        private void OnEnable()
        {
            TrySubscribeToQTEManager();
        }

        private void Start()
        {
            TrySubscribeToQTEManager();
        }

        private void Update()
        {
            if (subscribedManager == null)
            {
                TrySubscribeToQTEManager();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromQTEManager();
        }

        private void OnValidate()
        {
            initialTrust = ClampRelationshipValue(initialTrust);
            initialAffection = ClampRelationshipValue(initialAffection);
        }

        public void ApplyQTEResult(QTEResultType resultType)
        {
            int trustDelta;
            int affectionDelta;
            RelationshipMemoryTag memoryTag;

            switch (resultType)
            {
                case QTEResultType.Success:
                    trustDelta = successTrustDelta;
                    affectionDelta = successAffectionDelta;
                    memoryTag = RelationshipMemoryTag.Reliable;
                    break;
                case QTEResultType.WrongInput:
                    trustDelta = wrongInputTrustDelta;
                    affectionDelta = wrongInputAffectionDelta;
                    memoryTag = RelationshipMemoryTag.Stubborn;
                    break;
                case QTEResultType.Ignored:
                    trustDelta = ignoredTrustDelta;
                    affectionDelta = ignoredAffectionDelta;
                    memoryTag = RelationshipMemoryTag.Cold;
                    break;
                default:
                    return;
            }

            ApplyRelationshipChange(resultType, trustDelta, affectionDelta, memoryTag);
        }

        public void ApplyMemoryEvent(string sourceLabel, int trustDelta, int affectionDelta, RelationshipMemoryTag memoryTag)
        {
            ApplyRelationshipChange(sourceLabel, trustDelta, affectionDelta, memoryTag);
        }

        public int GetMemoryTagScore(RelationshipMemoryTag tag)
        {
            int index = FindMemoryTagIndex(tag);
            return index >= 0 ? memoryTags[index].score : 0;
        }

        private void TrySubscribeToQTEManager()
        {
            QTEManager manager = QTEManager.Instance;
            if (manager == null || manager == subscribedManager)
            {
                return;
            }

            UnsubscribeFromQTEManager();
            subscribedManager = manager;
            subscribedManager.QTECompleted += HandleQTECompleted;
        }

        private void UnsubscribeFromQTEManager()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.QTECompleted -= HandleQTECompleted;
            subscribedManager = null;
        }

        private void HandleQTECompleted(QTEManager manager, QTEResultType resultType)
        {
            if (manager.Requester != null && manager.Requester != gameObject)
            {
                return;
            }

            ApplyQTEResult(resultType);
        }

        private void ApplyRelationshipChange(
            string sourceLabel,
            int trustDelta,
            int affectionDelta,
            RelationshipMemoryTag memoryTag)
        {
            int previousTrust = trust;
            int previousAffection = affection;

            trust = ClampRelationshipValue(trust + trustDelta);
            affection = ClampRelationshipValue(affection + affectionDelta);
            AddMemoryTagScore(memoryTag, 1);

            RelationshipChange change = new RelationshipChange(
                sourceLabel,
                trust - previousTrust,
                affection - previousAffection,
                previousTrust,
                previousAffection,
                trust,
                affection,
                memoryTag);

            RelationshipChanged?.Invoke(this, change);
            AnyRelationshipChanged?.Invoke(this, change);

            if (logRelationshipChanges)
            {
                Debug.Log(
                    $"Companion relationship changed by {change.sourceLabel}: Trust {previousTrust}->{trust} ({change.trustDelta:+#;-#;0}), Affection {previousAffection}->{affection} ({change.affectionDelta:+#;-#;0}), Tag {memoryTag}={GetMemoryTagScore(memoryTag)}",
                    this);
            }
        }

        private void ApplyRelationshipChange(
            QTEResultType sourceResult,
            int trustDelta,
            int affectionDelta,
            RelationshipMemoryTag memoryTag)
        {
            int previousTrust = trust;
            int previousAffection = affection;

            trust = ClampRelationshipValue(trust + trustDelta);
            affection = ClampRelationshipValue(affection + affectionDelta);
            AddMemoryTagScore(memoryTag, 1);

            RelationshipChange change = new RelationshipChange(
                sourceResult,
                trust - previousTrust,
                affection - previousAffection,
                previousTrust,
                previousAffection,
                trust,
                affection,
                memoryTag);

            RelationshipChanged?.Invoke(this, change);
            AnyRelationshipChanged?.Invoke(this, change);

            if (logRelationshipChanges)
            {
                Debug.Log(
                    $"Companion relationship changed by {change.sourceLabel}: Trust {previousTrust}->{trust} ({change.trustDelta:+#;-#;0}), Affection {previousAffection}->{affection} ({change.affectionDelta:+#;-#;0}), Tag {memoryTag}={GetMemoryTagScore(memoryTag)}",
                    this);
            }
        }

        private void AddMemoryTagScore(RelationshipMemoryTag tag, int delta)
        {
            int index = FindMemoryTagIndex(tag);
            if (index < 0)
            {
                memoryTags.Add(new RelationshipMemoryTagScore
                {
                    tag = tag,
                    score = Mathf.Max(0, delta)
                });
                return;
            }

            RelationshipMemoryTagScore entry = memoryTags[index];
            entry.score = Mathf.Max(0, entry.score + delta);
            memoryTags[index] = entry;
        }

        private int FindMemoryTagIndex(RelationshipMemoryTag tag)
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
