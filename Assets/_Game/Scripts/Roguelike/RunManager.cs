using System;
using System.Collections.Generic;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Roguelike
{
    [RequireComponent(typeof(RoomManager))]
    public sealed class RunManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private BranchEventRoomController branchEventRoomController;

        [Header("Run Flow")]
        [SerializeField] private bool startRunOnStart = true;
        [SerializeField] private Key nextRoomKey = Key.N;
        [SerializeField] private bool allowDebugNextRoomKey;
        [SerializeField] private bool useRoomChoicePortal = true;
        [SerializeField] private bool useRoomModifiers = true;
        [SerializeField, Min(1)] private int roomChoiceCount = 3;
        [SerializeField] private RoomType[] selectableRoomTypes =
        {
            RoomType.BattleRoom,
            RoomType.SafeRoom,
            RoomType.ShopRoom,
            RoomType.EliteRoom
        };
        [SerializeField] private RoomType[] roomSequence =
        {
            RoomType.BattleRoom,
            RoomType.SafeRoom,
            RoomType.BattleRoom,
            RoomType.ShopRoom,
            RoomType.EliteRoom
        };

        [Header("Run Completion")]
        [SerializeField] private bool useRunCompletion = true;
        [SerializeField, Min(1)] private int roomsToCompleteRun = 4;
        [SerializeField] private bool useBossFinalRoom = true;
        [SerializeField] private string homeScenePath = "Assets/_Game/Scenes/HomeScene.unity";
        [SerializeField] private Key completionReturnHomeKey = Key.E;
        [SerializeField] private bool showCompletionPanel = true;
        [SerializeField] private Rect completionPanelRect = new Rect(0f, 92f, 580f, 300f);

        [Header("Room Feedback")]
        [SerializeField] private bool showRoomFeedbackPanel = true;
        [SerializeField] private Rect roomFeedbackPanelRect = new Rect(360f, 16f, 520f, 98f);

        [Header("Rewards")]
        [SerializeField] private bool useRoomRewards = true;
        [SerializeField, Min(1)] private int rewardChoiceCount = 3;
        [SerializeField] private RunRewardType[] selectableRewards =
        {
            RunRewardType.MaxHealth,
            RunRewardType.PlayerDamage,
            RunRewardType.MoveSpeed,
            RunRewardType.CompanionCooldown,
            RunRewardType.BondRescueHealth
        };
        [SerializeField, Min(0f)] private float maxHealthReward = 20f;
        [SerializeField, Min(1f)] private float playerDamageRewardMultiplier = 1.2f;
        [SerializeField, Min(1f)] private float moveSpeedRewardMultiplier = 1.1f;
        [SerializeField, Range(0.1f, 1f)] private float companionCooldownRewardMultiplier = 0.85f;
        [SerializeField, Min(0f)] private float bondRescueHealthReward = 10f;
        [SerializeField, Min(0)] private int eliteBonusRewardChoices = 1;
        [SerializeField, Min(1)] private int shopRewardChoiceCount = 2;
        [SerializeField, Min(0f)] private float safeRoomHealAmount = 25f;

        [Header("Debug")]
        [SerializeField] private bool logRunMessages = true;

        private int roomIndex = -1;
        private bool waitingForNextRoom;
        private bool waitingForReward;
        private bool runCompleted;
        private string lastRoomFeedbackMessage;
        private string lastRoomModifierFeedbackTitle;
        private string lastRoomModifierFeedbackLine;
        private Color lastRoomModifierFeedbackColor = Color.white;
        private RoomModifierType currentRoomModifier = RoomModifierType.None;
        private RoomModifierType pendingSelectedRoomModifier = RoomModifierType.None;
        private RoomManager subscribedRoomManager;
        private readonly List<RoomType> currentRouteHistory = new List<RoomType>(8);
        private readonly List<RoomModifierType> currentRouteModifierHistory = new List<RoomModifierType>(8);
        private readonly List<RoomType> currentRoomChoices = new List<RoomType>(4);
        private readonly List<RoomModifierType> currentRoomChoiceModifiers = new List<RoomModifierType>(4);
        private readonly List<RoomChoicePreview> currentRoomChoicePreviews = new List<RoomChoicePreview>(4);
        private readonly List<RouteMapNode> currentRouteMapNodes = new List<RouteMapNode>(8);
        private readonly List<RunRewardChoice> currentRewardChoices = new List<RunRewardChoice>(5);

        public static event Action<RunManager> AnyRunStarted;
        public event Action<RunManager> RunStarted;
        public event Action<RunManager, RoomType, int> RoomAdvanced;
        public event Action<RunManager, IReadOnlyList<RoomType>> RoomChoicesPrepared;
        public event Action<RunManager> RoomChoicesCleared;
        public event Action<RunManager, IReadOnlyList<RunRewardChoice>> RewardChoicesPrepared;
        public event Action<RunManager, RunRewardChoice> RewardSelected;
        public event Action<RunManager> RewardChoicesCleared;

        public int CurrentRoomNumber => Mathf.Max(0, roomIndex + 1);
        public RoomType CurrentRoomType => roomManager != null ? roomManager.CurrentRoomType : RoomType.BattleRoom;
        public bool IsWaitingForNextRoom => waitingForNextRoom;
        public bool IsWaitingForReward => waitingForReward;
        public bool IsRunCompleted => runCompleted;
        public int RoomsToCompleteRun => roomsToCompleteRun;
        public RoomModifierType CurrentRoomModifier => currentRoomModifier;
        public IReadOnlyList<RoomType> CurrentRouteHistory => currentRouteHistory;
        public IReadOnlyList<RoomModifierType> CurrentRouteModifierHistory => currentRouteModifierHistory;
        public string CurrentRouteProgressLabel => BuildCurrentRouteProgressLabel();
        public string CurrentRoutePathLabel => BuildCurrentRoutePathLabel();
        public string CurrentRouteMapLabel => BuildCurrentRouteMapLabel();
        public IReadOnlyList<RoomType> CurrentRoomChoices => currentRoomChoices;
        public IReadOnlyList<RoomModifierType> CurrentRoomChoiceModifiers => currentRoomChoiceModifiers;
        public IReadOnlyList<RoomChoicePreview> CurrentRoomChoicePreviews => currentRoomChoicePreviews;
        public IReadOnlyList<RouteMapNode> CurrentRouteMapNodes => currentRouteMapNodes;
        public IReadOnlyList<RunRewardChoice> CurrentRewardChoices => currentRewardChoices;
        public string LastRoomFeedbackMessage => lastRoomFeedbackMessage;
        public string LastRoomModifierFeedbackTitle => lastRoomModifierFeedbackTitle;
        public string LastRoomModifierFeedbackLine => lastRoomModifierFeedbackLine;
        public Color LastRoomModifierFeedbackColor => lastRoomModifierFeedbackColor;

        private void Reset()
        {
            roomManager = GetComponent<RoomManager>();
            branchEventRoomController = GetComponent<BranchEventRoomController>();
        }

        private void Awake()
        {
            ResolveRoomManager();
            branchEventRoomController = branchEventRoomController != null ? branchEventRoomController : GetComponent<BranchEventRoomController>();
        }

        private void ResolveRoomManager()
        {
            roomManager = roomManager != null ? roomManager : GetComponent<RoomManager>();
        }

        private void SubscribeToRoomManager()
        {
            if (roomManager == subscribedRoomManager)
            {
                return;
            }

            UnsubscribeFromRoomManager();

            if (roomManager == null)
            {
                return;
            }

            subscribedRoomManager = roomManager;
            subscribedRoomManager.RoomCleared += HandleRoomCleared;
        }

        private void UnsubscribeFromRoomManager()
        {
            if (subscribedRoomManager == null)
            {
                return;
            }

            subscribedRoomManager.RoomCleared -= HandleRoomCleared;
            subscribedRoomManager = null;
        }

        private void OnEnable()
        {
            ResolveRoomManager();
            SubscribeToRoomManager();
        }

        private void Start()
        {
            ResolveRoomManager();
            SubscribeToRoomManager();

            if (startRunOnStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (runCompleted)
            {
                if (WasCompletionReturnHomePressed())
                {
                    ReturnHomeAfterCompletion();
                }

                return;
            }

            if (!allowDebugNextRoomKey || !waitingForNextRoom || !WasNextRoomPressed())
            {
                return;
            }

            AdvanceToNextRoom();
        }

        private void OnDisable()
        {
            UnsubscribeFromRoomManager();
        }

        public void StartRun()
        {
            RunSessionState.EnsureRunStartedFromBattleScene(SceneManager.GetActiveScene().path);
            roomIndex = -1;
            waitingForNextRoom = false;
            waitingForReward = false;
            runCompleted = false;
            SetRoomFeedback(string.Empty);
            ClearRoomModifierFeedback();
            currentRoomModifier = RoomModifierType.None;
            pendingSelectedRoomModifier = RoomModifierType.None;
            currentRouteHistory.Clear();
            currentRouteModifierHistory.Clear();
            ClearRewardChoices();
            ClearPreparedRoomChoices();

            if (logRunMessages)
            {
                Debug.Log("Run started.", this);
            }

            RunStarted?.Invoke(this);
            AnyRunStarted?.Invoke(this);
            AdvanceToNextRoom();
        }

        public void AdvanceToNextRoom()
        {
            RoomType nextRoomType = GetRoomTypeForIndex(roomIndex + 1);
            pendingSelectedRoomModifier = RoomModifierType.None;
            AdvanceToRoom(nextRoomType);
        }

        public void AdvanceToSelectedRoom(int selectedChoiceIndex)
        {
            if (!waitingForNextRoom)
            {
                Debug.LogWarning($"RunManager ignored selected room index {selectedChoiceIndex} because no next-room choice is active.", this);
                return;
            }

            if (selectedChoiceIndex < 0 || selectedChoiceIndex >= currentRoomChoices.Count)
            {
                Debug.LogWarning($"RunManager rejected room choice index {selectedChoiceIndex}.", this);
                return;
            }

            pendingSelectedRoomModifier = GetPreparedRoomChoiceModifier(selectedChoiceIndex);
            AdvanceToRoom(currentRoomChoices[selectedChoiceIndex]);
        }

        public void AdvanceToSelectedRoom(RoomType selectedRoomType)
        {
            if (!waitingForNextRoom)
            {
                Debug.LogWarning($"RunManager ignored selected room {selectedRoomType} because no next-room choice is active.", this);
                return;
            }

            if (!IsSelectableRoomType(selectedRoomType))
            {
                Debug.LogWarning($"RunManager rejected non-selectable room type: {selectedRoomType}.", this);
                return;
            }

            int selectedIndex = currentRoomChoices.IndexOf(selectedRoomType);
            pendingSelectedRoomModifier = selectedIndex >= 0
                ? GetPreparedRoomChoiceModifier(selectedIndex)
                : RoomModifierType.None;
            AdvanceToRoom(selectedRoomType);
        }

        private void AdvanceToRoom(RoomType nextRoomType)
        {
            ResolveRoomManager();
            SubscribeToRoomManager();

            if (roomManager == null)
            {
                Debug.LogWarning("RunManager cannot advance because RoomManager is missing.", this);
                return;
            }

            RoomModifierType nextRoomModifier = ShouldAllowModifier(nextRoomType)
                ? pendingSelectedRoomModifier
                : RoomModifierType.None;
            pendingSelectedRoomModifier = RoomModifierType.None;
            currentRoomModifier = nextRoomModifier;
            roomIndex++;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();

            int roomNumber = roomIndex + 1;

            float restoredHealth = ApplyRoomEntryEffect(nextRoomType, currentRoomModifier);
            ApplyRoomModifierEntryEffect(currentRoomModifier);
            SetRoomModifierFeedback(currentRoomModifier, restoredHealth);
            SetRoomFeedback(BuildRoomFeedbackMessage(nextRoomType, restoredHealth, currentRoomModifier));
            RecordRouteEntry(nextRoomType, roomNumber, currentRoomModifier);
            roomManager.EnterRoom(nextRoomType, roomNumber, currentRoomModifier);
            RoomAdvanced?.Invoke(this, nextRoomType, roomNumber);

            if (logRunMessages)
            {
                Debug.Log($"Advanced to room #{roomNumber}: {FormatRoomTypeWithModifier(nextRoomType, currentRoomModifier)}", this);
            }
        }

        public void EnterBranchEventRoom()
        {
            if (branchEventRoomController == null)
            {
                Debug.LogWarning("RunManager cannot enter BranchEventRoom because BranchEventRoomController is missing.", this);
                return;
            }

            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();
            branchEventRoomController.BeginBranchEventRoom(CurrentRoomNumber, CurrentRoomType);

            if (logRunMessages)
            {
                Debug.Log($"Entered BranchEventRoom from {CurrentRoomType} #{CurrentRoomNumber}.", this);
            }
        }

        public void ForceClearCurrentRoom()
        {
            if (roomManager != null)
            {
                roomManager.ForceClearCurrentRoom();
            }
        }

        private RoomType GetRoomTypeForIndex(int index)
        {
            if (ShouldUseBossRoomForIndex(index))
            {
                return RoomType.BossRoom;
            }

            if (roomSequence == null || roomSequence.Length == 0)
            {
                return RoomType.BattleRoom;
            }

            return roomSequence[index % roomSequence.Length];
        }

        private void HandleRoomCleared(RoomManager clearedRoomManager, RoomType roomType, int roomNumber)
        {
            waitingForNextRoom = false;
            RunSessionState.RecordRoomCleared(roomType, roomNumber);

            if (logRunMessages)
            {
                string message = ShouldCompleteRun(roomNumber)
                    ? $"Room #{roomNumber} cleared. Run complete."
                    : IsNextRoomBossRoom()
                    ? $"Room #{roomNumber} cleared. Find the final portal to challenge the boss."
                    : ShouldOfferReward(roomType)
                    ? $"Room #{roomNumber} cleared. Choose a reward before selecting the next route."
                    : (useRoomChoicePortal
                        ? $"Room #{roomNumber} cleared. Find the next-room portal to choose a route."
                        : $"Room #{roomNumber} cleared. Press {nextRoomKey} to enter the next room.");
                Debug.Log(message, this);
            }

            if (ShouldCompleteRun(roomNumber))
            {
                CompleteRun(roomType, roomNumber);
                return;
            }

            if (ShouldOfferReward(roomType))
            {
                PrepareRewardChoices(roomType);
                return;
            }

            BeginNextRoomChoiceFlow();
        }

        public void SelectReward(int index)
        {
            if (!waitingForReward || index < 0 || index >= currentRewardChoices.Count)
            {
                return;
            }

            RunRewardChoice reward = currentRewardChoices[index];
            ApplyReward(reward.RewardType);
            RunSessionState.RecordRewardSelected(reward.Title);
            waitingForReward = false;
            ClearRewardChoices();

            if (logRunMessages)
            {
                Debug.Log($"Reward selected: {reward.Title}. {reward.Description}", this);
            }

            RewardSelected?.Invoke(this, reward);
            BeginNextRoomChoiceFlow();
        }

        private void BeginNextRoomChoiceFlow()
        {
            if (runCompleted)
            {
                return;
            }

            waitingForNextRoom = true;

            if (useRoomChoicePortal)
            {
                PrepareNextRoomChoices();
            }
        }

        private bool WasNextRoomPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && nextRoomKey != Key.None && keyboard[nextRoomKey].wasPressedThisFrame;
        }

        private bool WasCompletionReturnHomePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && completionReturnHomeKey != Key.None
                && keyboard[completionReturnHomeKey].wasPressedThisFrame;
        }

        private bool ShouldOfferReward(RoomType roomType)
        {
            return !runCompleted
                && useRoomRewards
                && (roomType == RoomType.BattleRoom || roomType == RoomType.EliteRoom || roomType == RoomType.ShopRoom);
        }

        private bool ShouldCompleteRun(int roomNumber)
        {
            return useRunCompletion
                && !runCompleted
                && CurrentRoomType == RoomType.BossRoom
                && roomNumber >= roomsToCompleteRun;
        }

        private bool ShouldUseBossRoomForIndex(int index)
        {
            return useRunCompletion
                && useBossFinalRoom
                && index >= Mathf.Max(0, roomsToCompleteRun - 1);
        }

        private bool IsNextRoomBossRoom()
        {
            return GetRoomTypeForIndex(roomIndex + 1) == RoomType.BossRoom;
        }

        private void CompleteRun(RoomType roomType, int roomNumber)
        {
            runCompleted = true;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();

            CompanionBossPostFightSettlement postFightSettlement = FindAnyObjectByType<CompanionBossPostFightSettlement>();
            if (postFightSettlement != null)
            {
                postFightSettlement.SettleBossVictory();
            }

            CompanionRelationship relationship = FindAnyObjectByType<CompanionRelationship>();
            int finalTrust = relationship != null ? relationship.Trust : -1;
            int finalAffection = relationship != null ? relationship.Affection : -1;
            RunSessionState.EndRun(RunEndReason.Victory, finalTrust, finalAffection);

            if (logRunMessages)
            {
                Debug.Log($"Run complete after room #{roomNumber}: {roomType}. Press {completionReturnHomeKey} to return home.", this);
            }
        }

        public void ReturnHomeAfterCompletion()
        {
            if (!runCompleted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(homeScenePath))
            {
                Debug.LogWarning("RunManager cannot return home after completion because homeScenePath is empty.", this);
                return;
            }

            SceneManager.LoadScene(homeScenePath, LoadSceneMode.Single);
        }

        private void PrepareRewardChoices()
        {
            PrepareRewardChoices(CurrentRoomType);
        }

        private void PrepareRewardChoices(RoomType sourceRoomType)
        {
            waitingForReward = true;
            currentRewardChoices.Clear();

            List<RunRewardType> candidates = BuildRewardCandidateList();
            int targetCount = GetRewardChoiceTargetCount(sourceRoomType, candidates.Count);
            RunRewardType? buildRewardType = GetCurrentBuildRewardType();
            if (buildRewardType.HasValue && currentRewardChoices.Count < targetCount)
            {
                currentRewardChoices.Add(CreateRewardChoice(buildRewardType.Value));
                candidates.Remove(buildRewardType.Value);
            }

            while (currentRewardChoices.Count < targetCount)
            {
                int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
                currentRewardChoices.Add(CreateRewardChoice(candidates[selectedIndex]));
                candidates.RemoveAt(selectedIndex);
            }

            if (currentRewardChoices.Count == 0)
            {
                currentRewardChoices.Add(CreateRewardChoice(RunRewardType.MaxHealth));
            }

            RewardChoicesPrepared?.Invoke(this, currentRewardChoices);
        }

        private int GetRewardChoiceTargetCount(RoomType sourceRoomType, int candidateCount)
        {
            return GetRewardChoiceTargetCount(sourceRoomType, currentRoomModifier, candidateCount);
        }

        private int GetRewardChoiceTargetCount(
            RoomType sourceRoomType,
            RoomModifierType roomModifier,
            int candidateCount)
        {
            if (candidateCount <= 0)
            {
                return 0;
            }

            int targetCount = rewardChoiceCount;
            switch (sourceRoomType)
            {
                case RoomType.EliteRoom:
                    targetCount += eliteBonusRewardChoices;
                    break;
                case RoomType.ShopRoom:
                    targetCount = shopRewardChoiceCount;
                    break;
            }

            targetCount += RoomModifierRules.GetBonusRewardChoices(roomModifier);
            return Mathf.Min(Mathf.Max(1, targetCount), candidateCount);
        }

        private void ClearRewardChoices()
        {
            if (currentRewardChoices.Count == 0)
            {
                return;
            }

            currentRewardChoices.Clear();
            RewardChoicesCleared?.Invoke(this);
        }

        private List<RunRewardType> BuildRewardCandidateList()
        {
            List<RunRewardType> candidates = new List<RunRewardType>(8);

            if (selectableRewards != null)
            {
                for (int i = 0; i < selectableRewards.Length; i++)
                {
                    RunRewardType reward = selectableRewards[i];
                    if (!candidates.Contains(reward))
                    {
                        candidates.Add(reward);
                    }
                }
            }

            RunRewardType? buildRewardType = GetCurrentBuildRewardType();
            if (buildRewardType.HasValue && !candidates.Contains(buildRewardType.Value))
            {
                candidates.Add(buildRewardType.Value);
            }

            return candidates;
        }

        private RunRewardChoice CreateRewardChoice(RunRewardType rewardType)
        {
            switch (rewardType)
            {
                case RunRewardType.MaxHealth:
                    return new RunRewardChoice(rewardType, $"最大生命 +{maxHealthReward:0}", "提高玩家最大生命，并立即补满生命。");
                case RunRewardType.PlayerDamage:
                    return new RunRewardChoice(rewardType, $"攻击力 +{(playerDamageRewardMultiplier - 1f) * 100f:0}%", "玩家普通攻击造成更多伤害。");
                case RunRewardType.MoveSpeed:
                    return new RunRewardChoice(rewardType, $"移动速度 +{(moveSpeedRewardMultiplier - 1f) * 100f:0}%", "玩家横向移动更快。");
                case RunRewardType.CompanionCooldown:
                    return new RunRewardChoice(rewardType, $"AI 支援冷却 -{(1f - companionCooldownRewardMultiplier) * 100f:0}%", "AI 队友支援攻击更频繁。");
                case RunRewardType.BondRescueHealth:
                    return new RunRewardChoice(rewardType, $"濒死保护生命 +{bondRescueHealthReward:0}", "濒死保护触发后保留更多生命。");
                case RunRewardType.GuardianBuildUpgrade:
                    return new RunRewardChoice(
                        rewardType,
                        CompanionSkillTendencyRules.GetBuildRewardTitle(CompanionSkillTendency.Guardian),
                        CompanionSkillTendencyRules.GetBuildRewardDescription(CompanionSkillTendency.Guardian));
                case RunRewardType.SuppressorBuildUpgrade:
                    return new RunRewardChoice(
                        rewardType,
                        CompanionSkillTendencyRules.GetBuildRewardTitle(CompanionSkillTendency.Suppressor),
                        CompanionSkillTendencyRules.GetBuildRewardDescription(CompanionSkillTendency.Suppressor));
                case RunRewardType.LinkBuildUpgrade:
                    return new RunRewardChoice(
                        rewardType,
                        CompanionSkillTendencyRules.GetBuildRewardTitle(CompanionSkillTendency.Link),
                        CompanionSkillTendencyRules.GetBuildRewardDescription(CompanionSkillTendency.Link));
                default:
                    return new RunRewardChoice(rewardType, rewardType.ToString(), "未知奖励。");
            }
        }

        private void ApplyReward(RunRewardType rewardType)
        {
            GameObject player = GameObject.Find("Player");

            switch (rewardType)
            {
                case RunRewardType.MaxHealth:
                    ApplyMaxHealthReward(player);
                    break;
                case RunRewardType.PlayerDamage:
                    ApplyPlayerDamageReward(player);
                    break;
                case RunRewardType.MoveSpeed:
                    ApplyMoveSpeedReward(player);
                    break;
                case RunRewardType.CompanionCooldown:
                    ApplyCompanionCooldownReward();
                    break;
                case RunRewardType.BondRescueHealth:
                    ApplyBondRescueHealthReward(player);
                    break;
                case RunRewardType.GuardianBuildUpgrade:
                    ApplyBuildUpgradeReward(CompanionSkillTendency.Guardian);
                    break;
                case RunRewardType.SuppressorBuildUpgrade:
                    ApplyBuildUpgradeReward(CompanionSkillTendency.Suppressor);
                    break;
                case RunRewardType.LinkBuildUpgrade:
                    ApplyBuildUpgradeReward(CompanionSkillTendency.Link);
                    break;
            }
        }

        private static RunRewardType? GetCurrentBuildRewardType()
        {
            switch (CompanionRunBuildState.CurrentTendency)
            {
                case CompanionSkillTendency.Guardian:
                    return RunRewardType.GuardianBuildUpgrade;
                case CompanionSkillTendency.Suppressor:
                    return RunRewardType.SuppressorBuildUpgrade;
                case CompanionSkillTendency.Link:
                    return RunRewardType.LinkBuildUpgrade;
                default:
                    return null;
            }
        }

        private void ApplyBuildUpgradeReward(CompanionSkillTendency tendency)
        {
            CompanionRunBuildState.AddUpgrade(tendency);
        }

        private float ApplyRoomEntryEffect(RoomType roomType, RoomModifierType roomModifier)
        {
            if (roomType != RoomType.SafeRoom)
            {
                return 0f;
            }

            GameObject player = GameObject.Find("Player");
            HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
            if (health == null)
            {
                return 0f;
            }

            float before = health.CurrentHealth;
            float healAmount = safeRoomHealAmount * RoomModifierRules.GetSafeHealMultiplier(roomModifier);
            health.Heal(healAmount);
            float restoredHealth = health.CurrentHealth - before;

            if (logRunMessages && restoredHealth > 0f)
            {
                Debug.Log($"Safe room restored {restoredHealth:0} HP.", this);
            }

            return restoredHealth;
        }

        private void ApplyRoomModifierEntryEffect(RoomModifierType roomModifier)
        {
            if (roomModifier != RoomModifierType.BondSignal)
            {
                return;
            }

            CompanionRelationship relationship = FindAnyObjectByType<CompanionRelationship>();
            if (relationship != null)
            {
                relationship.ApplyMemoryEvent(
                    "Bond Signal Room",
                    RoomModifierRules.GetTrustDelta(roomModifier),
                    RoomModifierRules.GetAffectionDelta(roomModifier),
                    RoomModifierRules.GetMemoryTag(roomModifier));
            }

            ShowCompanionModifierFeedback(roomModifier);
        }

        private string BuildRoomFeedbackMessage(RoomType roomType, float restoredHealth, RoomModifierType roomModifier)
        {
            string modifierLine = BuildModifierFeedbackLine(roomModifier);
            switch (roomType)
            {
                case RoomType.BattleRoom:
                    return AppendModifierFeedback(
                        $"Battle Room: standard combat. Clear enemies for {GetRewardChoiceTargetCount(roomType, roomModifier, BuildRewardCandidateList().Count)} reward options.",
                        modifierLine);
                case RoomType.EliteRoom:
                    return AppendModifierFeedback(
                        $"Elite Room: enemies are larger, stronger, and worth {GetRewardChoiceTargetCount(roomType, roomModifier, BuildRewardCandidateList().Count)} reward options.",
                        modifierLine);
                case RoomType.SafeRoom:
                    return AppendModifierFeedback($"Safe Room: restored {restoredHealth:0} HP. No enemies here.", modifierLine);
                case RoomType.ShopRoom:
                    return AppendModifierFeedback(
                        $"Supply Room: no enemies. Choose from {GetRewardChoiceTargetCount(roomType, roomModifier, BuildRewardCandidateList().Count)} reward options.",
                        modifierLine);
                case RoomType.BossRoom:
                    return "Boss Room: final challenge. Defeat the boss to complete the run.";
                default:
                    return AppendModifierFeedback($"{roomType}: scout ahead.", modifierLine);
            }
        }

        private static string BuildModifierFeedbackLine(RoomModifierType roomModifier)
        {
            string title = RoomModifierRules.GetFeedbackTitle(roomModifier);
            string visualHint = RoomModifierRules.GetReadableVisualHint(roomModifier);
            return string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(visualHint)
                ? string.Empty
                : $"{title}: {visualHint}.";
        }

        private static string AppendModifierFeedback(string baseMessage, string modifierLine)
        {
            return string.IsNullOrWhiteSpace(modifierLine)
                ? baseMessage
                : $"{baseMessage} Modifier {modifierLine}";
        }

        private void SetRoomFeedback(string message)
        {
            lastRoomFeedbackMessage = message ?? string.Empty;
        }

        private void SetRoomModifierFeedback(RoomModifierType roomModifier, float restoredHealth)
        {
            if (roomModifier == RoomModifierType.None)
            {
                ClearRoomModifierFeedback();
                return;
            }

            lastRoomModifierFeedbackTitle = RoomModifierRules.GetFeedbackTitle(roomModifier);
            lastRoomModifierFeedbackLine = RoomModifierRules.BuildEntryFeedbackLine(roomModifier, restoredHealth);
            lastRoomModifierFeedbackColor = RoomModifierRules.GetFeedbackColor(roomModifier);
        }

        private void ClearRoomModifierFeedback()
        {
            lastRoomModifierFeedbackTitle = string.Empty;
            lastRoomModifierFeedbackLine = string.Empty;
            lastRoomModifierFeedbackColor = Color.white;
        }

        private static void ShowCompanionModifierFeedback(RoomModifierType roomModifier)
        {
            string message = RoomModifierRules.BuildCompanionFeedbackLine(roomModifier);
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            CompanionSpeechBubbleUI speechBubble = UnityEngine.Object.FindAnyObjectByType<CompanionSpeechBubbleUI>();
            if (speechBubble != null)
            {
                speechBubble.ShowMessage(message, 4f, 3);
            }
        }

        private void ApplyMaxHealthReward(GameObject player)
        {
            HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
            if (health != null)
            {
                health.SetMaxHealth(health.MaxHealth + maxHealthReward, true);
            }
        }

        private void ApplyPlayerDamageReward(GameObject player)
        {
            PlayerCombat2D combat = player != null ? player.GetComponent<PlayerCombat2D>() : null;
            if (combat != null)
            {
                combat.MultiplyDamage(playerDamageRewardMultiplier);
            }
        }

        private void ApplyMoveSpeedReward(GameObject player)
        {
            PlayerMovement2D movement = player != null ? player.GetComponent<PlayerMovement2D>() : null;
            if (movement != null)
            {
                movement.MultiplyMoveSpeed(moveSpeedRewardMultiplier);
            }
        }

        private void ApplyCompanionCooldownReward()
        {
            CompanionCombat companionCombat = FindAnyObjectByType<CompanionCombat>();
            if (companionCombat != null)
            {
                companionCombat.MultiplyCooldown(companionCooldownRewardMultiplier);
            }
        }

        private void ApplyBondRescueHealthReward(GameObject player)
        {
            BondRescueSystem bondRescueSystem = player != null ? player.GetComponent<BondRescueSystem>() : null;
            if (bondRescueSystem != null)
            {
                bondRescueSystem.AddRescueHealth(bondRescueHealthReward);
            }
        }

        private void RecordRouteEntry(RoomType roomType, int roomNumber, RoomModifierType roomModifier)
        {
            currentRouteHistory.Add(roomType);
            currentRouteModifierHistory.Add(roomModifier);
            RunSessionState.RecordRoomEntered(roomType, roomNumber, roomModifier);
        }

        private string BuildCurrentRouteProgressLabel()
        {
            int totalRooms = Mathf.Max(1, roomsToCompleteRun);
            int currentRoom = Mathf.Clamp(CurrentRoomNumber, 0, totalRooms);

            if (currentRoom == 0)
            {
                return $"Room 0/{totalRooms} | Choose a route";
            }

            if (CurrentRoomType == RoomType.BossRoom)
            {
                return $"Room {currentRoom}/{totalRooms} | Boss room";
            }

            if (!useRunCompletion || !useBossFinalRoom)
            {
                return $"Room {currentRoom}/{totalRooms} | Endless route";
            }

            int roomsBeforeBoss = Mathf.Max(0, Mathf.Max(1, totalRooms - 1) - currentRoom);
            string bossLabel = roomsBeforeBoss == 0
                ? "Boss next"
                : $"Boss in {roomsBeforeBoss} room(s)";
            return $"Room {currentRoom}/{totalRooms} | {bossLabel}";
        }

        private string BuildCurrentRoutePathLabel()
        {
            if (currentRouteHistory.Count == 0)
            {
                return "Route: none";
            }

            List<string> labels = new List<string>(currentRouteHistory.Count);
            for (int i = 0; i < currentRouteHistory.Count; i++)
            {
                labels.Add(FormatRoomTypeWithModifier(
                    currentRouteHistory[i],
                    i < currentRouteModifierHistory.Count ? currentRouteModifierHistory[i] : RoomModifierType.None));
            }

            return $"Route: {string.Join(" -> ", labels)}";
        }

        private string BuildCurrentRouteMapLabel()
        {
            string historyLabel = currentRouteHistory.Count > 0
                ? string.Join(" -> ", BuildRoomLabelList(currentRouteHistory, currentRouteModifierHistory))
                : "Start";
            string nextLabel = currentRoomChoices.Count > 0
                ? string.Join(" / ", BuildRoomLabelList(currentRoomChoices, currentRoomChoiceModifiers))
                : "none";
            string goalLabel = useRunCompletion && useBossFinalRoom
                ? $"{GetRoomShortLabel(RoomType.BossRoom)}@{Mathf.Max(1, roomsToCompleteRun)}"
                : "Endless";

            return $"Map: {historyLabel} | Next: {nextLabel} | Goal: {goalLabel}";
        }

        private static List<string> BuildRoomLabelList(
            IReadOnlyList<RoomType> roomTypes,
            IReadOnlyList<RoomModifierType> roomModifiers)
        {
            List<string> labels = new List<string>(roomTypes.Count);
            for (int i = 0; i < roomTypes.Count; i++)
            {
                if (roomTypes[i] == RoomType.BranchEventRoom)
                {
                    continue;
                }

                RoomModifierType modifier = roomModifiers != null && i < roomModifiers.Count
                    ? roomModifiers[i]
                    : RoomModifierType.None;
                labels.Add(FormatRoomTypeWithModifier(roomTypes[i], modifier));
            }

            return labels;
        }

        private void PrepareNextRoomChoices()
        {
            currentRoomChoices.Clear();

            if (IsNextRoomBossRoom())
            {
                currentRoomChoices.Add(RoomType.BossRoom);
            }
            else
            {
                List<RoomType> candidates = BuildPacedCandidateList();
                int targetCount = Mathf.Min(roomChoiceCount, candidates.Count);

                for (int i = 0; i < targetCount; i++)
                {
                    currentRoomChoices.Add(candidates[i]);
                }

                if (currentRoomChoices.Count == 0)
                {
                    currentRoomChoices.Add(RoomType.BattleRoom);
                }
            }

            RefreshCurrentRoomChoiceModifiers();
            RefreshCurrentRoomChoicePreviews();
            RefreshCurrentRouteMapNodes();

            if (logRunMessages)
            {
                Debug.Log($"Next room choices ready: {string.Join(", ", currentRoomChoices)}", this);
            }

            RoomChoicesPrepared?.Invoke(this, currentRoomChoices);
        }

        private void ClearPreparedRoomChoices()
        {
            if (currentRoomChoices.Count == 0
                && currentRoomChoiceModifiers.Count == 0
                && currentRoomChoicePreviews.Count == 0
                && currentRouteMapNodes.Count == 0)
            {
                return;
            }

            currentRoomChoices.Clear();
            currentRoomChoiceModifiers.Clear();
            currentRoomChoicePreviews.Clear();
            currentRouteMapNodes.Clear();
            RoomChoicesCleared?.Invoke(this);
        }

        private void RefreshCurrentRoomChoiceModifiers()
        {
            currentRoomChoiceModifiers.Clear();

            for (int i = 0; i < currentRoomChoices.Count; i++)
            {
                currentRoomChoiceModifiers.Add(RoomModifierRules.GetModifierForChoice(
                    currentRoomChoices[i],
                    i,
                    useRoomModifiers));
            }
        }

        private void RefreshCurrentRoomChoicePreviews()
        {
            currentRoomChoicePreviews.Clear();

            for (int i = 0; i < currentRoomChoices.Count; i++)
            {
                currentRoomChoicePreviews.Add(CreateRoomChoicePreview(
                    currentRoomChoices[i],
                    GetPreparedRoomChoiceModifier(i)));
            }
        }

        private void RefreshCurrentRouteMapNodes()
        {
            currentRouteMapNodes.Clear();

            for (int i = 0; i < currentRouteHistory.Count; i++)
            {
                RoomType roomType = currentRouteHistory[i];
                RoomModifierType modifier = i < currentRouteModifierHistory.Count
                    ? currentRouteModifierHistory[i]
                    : RoomModifierType.None;
                bool isCurrent = i == currentRouteHistory.Count - 1;
                currentRouteMapNodes.Add(new RouteMapNode(
                    roomType,
                    GetRoomShortLabel(roomType),
                    i + 1,
                    !isCurrent,
                    isCurrent,
                    false,
                    roomType == RoomType.BossRoom,
                    -1,
                    modifier,
                    RoomModifierRules.GetShortLabel(modifier)));
            }

            int nextStepNumber = Mathf.Max(1, CurrentRoomNumber + 1);
            for (int i = 0; i < currentRoomChoices.Count; i++)
            {
                RoomType choice = currentRoomChoices[i];
                RoomModifierType modifier = GetPreparedRoomChoiceModifier(i);
                if (choice == RoomType.BranchEventRoom)
                {
                    continue;
                }

                currentRouteMapNodes.Add(new RouteMapNode(
                    choice,
                    GetRoomShortLabel(choice),
                    nextStepNumber,
                    false,
                    false,
                    true,
                    choice == RoomType.BossRoom,
                    i,
                    modifier,
                    RoomModifierRules.GetShortLabel(modifier)));
            }

            if (ShouldAddBossEndpointNode())
            {
                currentRouteMapNodes.Add(new RouteMapNode(
                    RoomType.BossRoom,
                    GetRoomShortLabel(RoomType.BossRoom),
                    Mathf.Max(1, roomsToCompleteRun),
                    false,
                    false,
                    false,
                    true,
                    -1));
            }
        }

        private bool ShouldAddBossEndpointNode()
        {
            return useRunCompletion
                && useBossFinalRoom
                && !ContainsBossEndpointNode();
        }

        private bool ContainsBossEndpointNode()
        {
            for (int i = 0; i < currentRouteMapNodes.Count; i++)
            {
                if (currentRouteMapNodes[i].RoomType == RoomType.BossRoom
                    && currentRouteMapNodes[i].IsBossEndpoint)
                {
                    return true;
                }
            }

            return false;
        }

        private RoomModifierType GetPreparedRoomChoiceModifier(int index)
        {
            return index >= 0 && index < currentRoomChoiceModifiers.Count
                ? currentRoomChoiceModifiers[index]
                : RoomModifierType.None;
        }

        private static bool ShouldAllowModifier(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.BattleRoom:
                case RoomType.EliteRoom:
                case RoomType.SafeRoom:
                case RoomType.ShopRoom:
                    return true;
                default:
                    return false;
            }
        }

        private static string FormatRoomTypeWithModifier(RoomType roomType, RoomModifierType modifier)
        {
            return RoomModifierRules.FormatRoomWithModifier(GetRoomShortLabel(roomType), modifier);
        }

        private RoomChoicePreview CreateRoomChoicePreview(RoomType roomType, RoomModifierType modifier)
        {
            RoomModifierPreview modifierPreview = RoomModifierRules.CreatePreview(roomType, modifier);
            switch (roomType)
            {
                case RoomType.BattleRoom:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        "Battle Room",
                        "Threat: normal enemy group.",
                        BuildRewardPreview(roomType, modifier),
                        "Route: clear enemies, then choose a standard reward.",
                        modifierPreview);
                case RoomType.EliteRoom:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        "Elite Room",
                        "Threat: stronger enemy group.",
                        BuildRewardPreview(roomType, modifier),
                        "Route: higher risk for a wider reward draft.",
                        modifierPreview);
                case RoomType.SafeRoom:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        "Safe Room",
                        "Threat: safe, no enemies.",
                        $"Reward: restore {safeRoomHealAmount * RoomModifierRules.GetSafeHealMultiplier(modifier):0} HP.",
                        "Route: recover, then pick the next path.",
                        modifierPreview);
                case RoomType.ShopRoom:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        "Supply Room",
                        "Threat: safe, no enemies.",
                        BuildRewardPreview(roomType, modifier),
                        "Route: take a smaller reward draft without combat.",
                        modifierPreview);
                case RoomType.BossRoom:
                    return new RoomChoicePreview(
                        roomType,
                        "Boss Room",
                        "Threat: final boss.",
                        "Reward: complete the run and return home.",
                        "Route: final challenge.");
                default:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        roomType.ToString(),
                        "Threat: unknown.",
                        "Reward: unknown.",
                        "Route: scout ahead.",
                        modifierPreview);
            }
        }

        private static RoomChoicePreview CreateModifiedRoomChoicePreview(
            RoomType roomType,
            string title,
            string threatPreview,
            string rewardPreview,
            string routeNote,
            RoomModifierPreview modifierPreview)
        {
            string fullTitle = modifierPreview.HasModifier
                ? $"{title} - {modifierPreview.Title}"
                : title;
            return new RoomChoicePreview(
                roomType,
                fullTitle,
                threatPreview,
                rewardPreview,
                routeNote,
                modifierPreview.ModifierType,
                modifierPreview.Title,
                modifierPreview.RiskPreview,
                modifierPreview.RewardPreview,
                modifierPreview.RouteNote);
        }

        private string BuildRewardPreview(RoomType roomType, RoomModifierType modifier)
        {
            int candidateCount = BuildRewardCandidateList().Count;
            int count = GetRewardChoiceTargetCount(roomType, modifier, candidateCount);
            string buildHint = GetCurrentBuildRewardType().HasValue
                ? " Current AI Build upgrade can appear."
                : string.Empty;

            return $"Reward: choose 1 of {count} options.{buildHint}";
        }

        private List<RoomType> BuildSelectableCandidateList()
        {
            List<RoomType> candidates = new List<RoomType>(4);

            if (selectableRoomTypes != null)
            {
                for (int i = 0; i < selectableRoomTypes.Length; i++)
                {
                    RoomType candidate = selectableRoomTypes[i];
                    if (IsSelectableRoomType(candidate) && !candidates.Contains(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                candidates.Add(RoomType.BattleRoom);
            }

            return candidates;
        }

        private List<RoomType> BuildPacedCandidateList()
        {
            List<RoomType> selectableCandidates = BuildSelectableCandidateList();
            bool suppressSupportRooms = ShouldSuppressSupportRoomCandidates();
            int nextRoomNumber = roomIndex + 2;
            List<RoomType> orderedCandidates = new List<RoomType>(selectableCandidates.Count);

            if (IsLateRouteChoice(nextRoomNumber))
            {
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.EliteRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.BattleRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.ShopRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.SafeRoom, suppressSupportRooms);
            }
            else if (IsEarlyRouteChoice(nextRoomNumber))
            {
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.BattleRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.SafeRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.ShopRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.EliteRoom, suppressSupportRooms);
            }
            else
            {
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.BattleRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.EliteRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.SafeRoom, suppressSupportRooms);
                AddPacedCandidate(orderedCandidates, selectableCandidates, RoomType.ShopRoom, suppressSupportRooms);
            }

            for (int i = 0; i < selectableCandidates.Count; i++)
            {
                AddPacedCandidate(orderedCandidates, selectableCandidates, selectableCandidates[i], suppressSupportRooms);
            }

            if (orderedCandidates.Count == 0)
            {
                orderedCandidates.Add(RoomType.BattleRoom);
            }

            return orderedCandidates;
        }

        private bool IsEarlyRouteChoice(int nextRoomNumber)
        {
            return nextRoomNumber <= 2;
        }

        private bool IsLateRouteChoice(int nextRoomNumber)
        {
            return useRunCompletion
                && useBossFinalRoom
                && nextRoomNumber >= Mathf.Max(2, roomsToCompleteRun - 1);
        }

        private bool ShouldSuppressSupportRoomCandidates()
        {
            if (currentRouteHistory.Count == 0)
            {
                return false;
            }

            return IsSupportRoom(currentRouteHistory[currentRouteHistory.Count - 1]);
        }

        private static void AddPacedCandidate(
            List<RoomType> orderedCandidates,
            List<RoomType> selectableCandidates,
            RoomType roomType,
            bool suppressSupportRooms)
        {
            if (!selectableCandidates.Contains(roomType) || orderedCandidates.Contains(roomType))
            {
                return;
            }

            if (suppressSupportRooms && IsSupportRoom(roomType))
            {
                return;
            }

            orderedCandidates.Add(roomType);
        }

        private void OnGUI()
        {
            if (showRoomFeedbackPanel && !runCompleted && !string.IsNullOrWhiteSpace(lastRoomFeedbackMessage))
            {
                DrawRoomFeedbackPanel();
            }

            if (showCompletionPanel && runCompleted)
            {
                DrawCompletionPanel();
            }
        }

        private void DrawRoomFeedbackPanel()
        {
            Rect rect = GetCenteredRect(roomFeedbackPanelRect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(lastRoomFeedbackMessage);
            if (!string.IsNullOrWhiteSpace(lastRoomModifierFeedbackTitle)
                || !string.IsNullOrWhiteSpace(lastRoomModifierFeedbackLine))
            {
                GUILayout.Space(4f);
                Color previousColor = GUI.color;
                GUI.color = lastRoomModifierFeedbackColor;
                GUILayout.Label(lastRoomModifierFeedbackTitle);
                GUI.color = previousColor;
                GUILayout.Label(lastRoomModifierFeedbackLine);
            }

            GUILayout.EndArea();
        }

        private void DrawCompletionPanel()
        {
            RunSessionSummary summary = RunSessionState.LastSummary;
            Rect rect = GetCenteredRect(completionPanelRect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("本局通关");
            GUILayout.Space(6f);
            GUILayout.Label($"清理房间：{summary.RoomsCleared}/{roomsToCompleteRun}");
            GUILayout.Label($"最后房间：#{summary.LastRoomNumber} {summary.LastRoomType}");
            GUILayout.Label(BuildCompletionRouteLine(summary));
            GUILayout.Label(BuildCompletionRewardLine(summary));
            GUILayout.Label(BuildCompletionRelationshipLine(summary));
            GUILayout.Label(BuildCompletionBossLine(summary));
            GUILayout.Label(BuildCompletionCompanionLine(summary));
            GUILayout.Space(10f);
            GUILayout.Label($"按 {completionReturnHomeKey} 返回家园");
            GUILayout.EndArea();
        }

        private Rect GetCenteredRect(Rect sourceRect)
        {
            float width = Mathf.Min(sourceRect.width, Mathf.Max(180f, Screen.width - 16f));
            float x = sourceRect.x <= 0f ? (Screen.width - width) * 0.5f : sourceRect.x;
            return new Rect(Mathf.Max(8f, x), sourceRect.y, width, sourceRect.height);
        }

        private static string BuildCompletionRewardLine(RunSessionSummary summary)
        {
            if (summary.RewardTitles == null || summary.RewardTitles.Length == 0)
            {
                return "选择奖励：无";
            }

            return $"选择奖励：{string.Join(" / ", summary.RewardTitles)}";
        }

        private static string BuildCompletionRouteLine(RunSessionSummary summary)
        {
            return summary.HasRoutePath ? summary.RoutePathLabel : "Route: none";
        }

        private static string BuildCompletionRelationshipLine(RunSessionSummary summary)
        {
            if (!summary.HasRelationship)
            {
                return "AI 关系：未记录";
            }

            return $"AI 关系：信赖 {summary.FinalTrust}    好感 {summary.FinalAffection}";
        }

        private static string BuildCompletionBossLine(RunSessionSummary summary)
        {
            return $"Boss AI Stats: shield {summary.BossSupportActivations}, warning hit {summary.BossWarningHits}, dodge {summary.BossWarningDodges}";
        }

        private static string BuildCompletionCompanionLine(RunSessionSummary summary)
        {
            if (!summary.HasCompanionFeedback)
            {
                return "AI Feedback: none";
            }

            return $"{summary.CompanionFeedbackLine}  Bond {summary.CompanionTrustDelta:+#;-#;0}/{summary.CompanionAffectionDelta:+#;-#;0}";
        }

        private static bool IsSelectableRoomType(RoomType roomType)
        {
            return roomType != RoomType.BranchEventRoom;
        }

        private static bool IsSupportRoom(RoomType roomType)
        {
            return roomType == RoomType.SafeRoom || roomType == RoomType.ShopRoom;
        }

        private static string GetRoomShortLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.BattleRoom => "Battle",
                RoomType.EliteRoom => "Elite",
                RoomType.SafeRoom => "Safe",
                RoomType.ShopRoom => "Supply",
                RoomType.BranchEventRoom => "Branch",
                RoomType.BossRoom => "Boss",
                _ => roomType.ToString()
            };
        }
    }
}
