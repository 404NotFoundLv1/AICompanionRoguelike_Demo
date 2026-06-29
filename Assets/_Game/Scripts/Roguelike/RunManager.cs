using System;
using System.Collections.Generic;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
using AICompanionRoguelike.Progression;
using AICompanionRoguelike.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Roguelike
{
    [RequireComponent(typeof(RoomManager))]
    public sealed class RunManager : MonoBehaviour
    {
        private static readonly RunRewardType[] CounterplayRewardTypes =
        {
            RunRewardType.DashCooldown,
            RunRewardType.RecoveryWindow,
            RunRewardType.DodgeDamageBoost,
            RunRewardType.GuardOpeningDamage
        };

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
        [SerializeField] private Rect roomFeedbackPanelRect = new Rect(360f, 16f, 640f, 156f);

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
        [SerializeField, Range(0.1f, 1f)] private float counterplayDashCooldownMultiplier = 0.85f;
        [SerializeField, Min(0f)] private float counterplayRecoveryDurationBonus = 0.12f;
        [SerializeField, Min(0f)] private float counterplayDodgeBoostDuration = 2f;
        [SerializeField, Min(1f)] private float counterplayDodgeDamageMultiplier = 1.25f;
        [SerializeField, Min(1f)] private float counterplayGuardOpeningDamageMultiplier = 1.25f;
        [SerializeField, Min(2)] private int growthRouteActivationThreshold = 2;
        [Header("Growth Route Effects")]
        [SerializeField, Min(0f)] private float playerRouteDamageBonusPerLevel = 0.08f;
        [SerializeField, Min(0f)] private float counterplayRouteDodgeBoostDurationPerLevel = 0.4f;
        [SerializeField, Min(0f)] private float counterplayRouteDamageBonusPerLevel = 0.1f;
        [SerializeField, Range(0.1f, 1f)] private float companionRouteCooldownMultiplierPerLevel = 0.9f;
        [SerializeField, Min(0f)] private float survivalRouteRescueHealthBonusPerLevel = 5f;
        [SerializeField, Min(0f)] private float routeSpecializationPlayerDamageBonus = 0.12f;
        [SerializeField, Min(0f)] private float routeSpecializationCounterplayDodgeBoostDurationBonus = 0.6f;
        [SerializeField, Min(0f)] private float routeSpecializationCounterplayDamageBonus = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float routeSpecializationCompanionCooldownMultiplier = 0.85f;
        [SerializeField, Min(0f)] private float routeSpecializationSurvivalRescueHealthBonus = 8f;
        [SerializeField, Min(0)] private int routeSpecializationBuildBonusLevel = 1;
        [SerializeField, Min(0)] private int eliteBonusRewardChoices = 1;
        [SerializeField, Min(1)] private int shopRewardChoiceCount = 2;
        [SerializeField, Min(0f)] private float safeRoomHealAmount = 25f;

        [Header("Supply Rooms")]
        [SerializeField, Min(0)] private int startingSupplies;

        [Header("Relics")]
        [SerializeField] private RunRelicType[] selectableRelics =
        {
            RunRelicType.FirstAidCharm,
            RunRelicType.SyncMark,
            RunRelicType.FieldBackpack
        };
        [SerializeField, Min(0f)] private float firstAidCharmHealAmount = RunRelicRules.DefaultFirstAidHealAmount;
        [SerializeField, Min(1f)] private float syncMarkDamageMultiplier = RunRelicRules.DefaultSyncMarkDamageMultiplier;

        [Header("Meta Progression")]
        [SerializeField, Min(0f)] private float metaPlayerMaxHealthBonusPerLevel = 10f;
        [SerializeField, Min(0f)] private float metaPlayerDamageBonusPerLevel = 0.08f;
        [SerializeField, Range(0.1f, 1f)] private float metaCompanionCooldownMultiplierPerLevel = 0.95f;
        [SerializeField, Min(0.5f)] private float metaProgressionFeedbackDuration = 4f;

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
        private readonly List<RunRewardChoice> currentRewardChoices = new List<RunRewardChoice>(8);
        private readonly List<SafeRestChoice> currentRestChoices = new List<SafeRestChoice>(3);
        private readonly List<RunRelicType> currentRelics = new List<RunRelicType>(4);
        private int currentSupplies;
        private string lastShopFeedbackMessage = string.Empty;
        private string lastSafeRestFeedbackMessage = string.Empty;
        private string lastRelicFeedbackMessage = string.Empty;
        private string lastRelicBannerMessage = string.Empty;
        private RoomType currentRewardSourceRoomType = RoomType.BattleRoom;
        private bool shopRewardDraftOpen;
        private bool shopRewardPurchasedThisRoom;
        private bool waitingForRest;
        private bool safeRestUsedThisRoom;
        private int playerGrowthCount;
        private int companionGrowthCount;
        private int counterplayGrowthCount;
        private int survivalGrowthCount;
        private int buildGrowthCount;
        private bool hasActiveGrowthRoute;
        private RunRewardCategory activeGrowthRouteCategory = RunRewardCategory.Player;
        private int activeGrowthRouteLevel;
        private int playerRouteSpecializationCount;
        private int companionRouteSpecializationCount;
        private int counterplayRouteSpecializationCount;
        private int survivalRouteSpecializationCount;
        private int buildRouteSpecializationCount;
        private int appliedMetaProgressionRunId = -1;
        private string metaProgressionFeedbackLine = string.Empty;
        private float metaProgressionFeedbackTimer;

        public static event Action<RunManager> AnyRunStarted;
        public event Action<RunManager> RunStarted;
        public event Action<RunManager, RoomType, int> RoomAdvanced;
        public event Action<RunManager, IReadOnlyList<RoomType>> RoomChoicesPrepared;
        public event Action<RunManager> RoomChoicesCleared;
        public event Action<RunManager, IReadOnlyList<RunRewardChoice>> RewardChoicesPrepared;
        public event Action<RunManager, RunRewardChoice> RewardSelected;
        public event Action<RunManager> RewardChoicesCleared;
        public event Action<RunManager, IReadOnlyList<SafeRestChoice>> RestChoicesPrepared;
        public event Action<RunManager, SafeRestChoice> RestChoiceSelected;
        public event Action<RunManager> RestChoicesCleared;

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
        public string CurrentRoomObjectiveLabel => BuildCurrentRoomObjectiveLabel();
        public string CurrentRoomProgressLabel => BuildCurrentRoomProgressLabel();
        public string CurrentNextStepLabel => BuildCurrentNextStepLabel();
        public bool HasActionableDemoNextStep => BuildHasActionableDemoNextStep();
        public string CurrentDemoFlowLabel => BuildCurrentDemoFlowLabel();
        public IReadOnlyList<RoomType> CurrentRoomChoices => currentRoomChoices;
        public IReadOnlyList<RoomModifierType> CurrentRoomChoiceModifiers => currentRoomChoiceModifiers;
        public IReadOnlyList<RoomChoicePreview> CurrentRoomChoicePreviews => currentRoomChoicePreviews;
        public IReadOnlyList<RouteMapNode> CurrentRouteMapNodes => currentRouteMapNodes;
        public IReadOnlyList<RunRewardChoice> CurrentRewardChoices => currentRewardChoices;
        public IReadOnlyList<SafeRestChoice> CurrentRestChoices => currentRestChoices;
        public IReadOnlyList<RunRelicType> CurrentRelics => currentRelics;
        public int CurrentSupplies => currentSupplies;
        public int CurrentShopRewardCost => RunSupplyRules.ShopRewardCost();
        public bool IsCurrentRewardShopPurchase => waitingForReward && currentRewardSourceRoomType == RoomType.ShopRoom;
        public bool CanOpenShopRewardDraft => CanOpenCurrentShopRewardDraft();
        public bool HasPurchasedCurrentShopReward => shopRewardPurchasedThisRoom;
        public string CurrentShopAffordabilityLabel => RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies);
        public string LastShopFeedbackMessage => lastShopFeedbackMessage;
        public bool IsWaitingForRest => waitingForRest;
        public bool CanOpenSafeRestDraft => CanOpenCurrentSafeRestDraft();
        public bool HasUsedCurrentSafeRest => safeRestUsedThisRoom;
        public string LastSafeRestFeedbackMessage => lastSafeRestFeedbackMessage;
        public string LastRelicFeedbackMessage => lastRelicFeedbackMessage;
        public string LastRelicBannerMessage => lastRelicBannerMessage;
        public string CurrentRelicSummaryLabel => BuildCurrentRelicSummaryLabel();
        public float SyncMarkDamageMultiplier => syncMarkDamageMultiplier;
        public string CurrentGrowthSummaryLabel => BuildCurrentGrowthSummaryLabel();
        public string CurrentGrowthRouteLabel => BuildCurrentGrowthRouteLabel();
        public bool HasActiveGrowthRoute => hasActiveGrowthRoute;
        public RunRewardCategory ActiveGrowthRouteCategory => activeGrowthRouteCategory;
        public int ActiveGrowthRouteLevel => activeGrowthRouteLevel;
        public float PlayerRouteDamageMultiplier => GetRouteMultiplier(RunRewardCategory.Player, playerRouteDamageBonusPerLevel)
            + (IsActiveGrowthRoute(RunRewardCategory.Player)
                ? routeSpecializationPlayerDamageBonus * playerRouteSpecializationCount
                : 0f);
        public float CounterplayRouteDodgeBoostDurationBonus => IsActiveGrowthRoute(RunRewardCategory.Counterplay)
            ? counterplayRouteDodgeBoostDurationPerLevel * Mathf.Max(0, activeGrowthRouteLevel - 1)
                + routeSpecializationCounterplayDodgeBoostDurationBonus * counterplayRouteSpecializationCount
            : 0f;
        public float CounterplayRouteDamageMultiplier => GetRouteMultiplier(RunRewardCategory.Counterplay, counterplayRouteDamageBonusPerLevel)
            + (IsActiveGrowthRoute(RunRewardCategory.Counterplay)
                ? routeSpecializationCounterplayDamageBonus * counterplayRouteSpecializationCount
                : 0f);
        public float CompanionRouteCooldownMultiplier => IsActiveGrowthRoute(RunRewardCategory.Companion)
            ? Mathf.Pow(companionRouteCooldownMultiplierPerLevel, Mathf.Max(0, activeGrowthRouteLevel - 1))
                * Mathf.Pow(routeSpecializationCompanionCooldownMultiplier, companionRouteSpecializationCount)
            : 1f;
        public float SurvivalRouteRescueHealthBonus => IsActiveGrowthRoute(RunRewardCategory.Survival)
            ? survivalRouteRescueHealthBonusPerLevel * Mathf.Max(0, activeGrowthRouteLevel - 1)
                + routeSpecializationSurvivalRescueHealthBonus * survivalRouteSpecializationCount
            : 0f;
        public int BuildRouteBonusLevel => IsActiveGrowthRoute(RunRewardCategory.Build)
            ? Mathf.Max(0, activeGrowthRouteLevel - 1)
                + routeSpecializationBuildBonusLevel * buildRouteSpecializationCount
            : 0;
        public int CurrentGrowthRouteSpecializationCount => GetRouteSpecializationCount(activeGrowthRouteCategory);
        public string LastRoomFeedbackMessage => lastRoomFeedbackMessage;
        public string LastRoomModifierFeedbackTitle => lastRoomModifierFeedbackTitle;
        public string LastRoomModifierFeedbackLine => lastRoomModifierFeedbackLine;
        public Color LastRoomModifierFeedbackColor => lastRoomModifierFeedbackColor;

        public static RunManager FindActiveRunManager()
        {
            return FindAnyObjectByType<RunManager>();
        }

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
            if (metaProgressionFeedbackTimer > 0f)
            {
                metaProgressionFeedbackTimer -= Time.deltaTime;
            }

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
            waitingForRest = false;
            runCompleted = false;
            SetRoomFeedback(string.Empty);
            ClearRoomModifierFeedback();
            currentRoomModifier = RoomModifierType.None;
            pendingSelectedRoomModifier = RoomModifierType.None;
            currentSupplies = Mathf.Max(0, startingSupplies);
            lastShopFeedbackMessage = string.Empty;
            lastSafeRestFeedbackMessage = string.Empty;
            lastRelicFeedbackMessage = string.Empty;
            lastRelicBannerMessage = string.Empty;
            currentRewardSourceRoomType = RoomType.BattleRoom;
            shopRewardDraftOpen = false;
            shopRewardPurchasedThisRoom = false;
            safeRestUsedThisRoom = false;
            currentRouteHistory.Clear();
            currentRouteModifierHistory.Clear();
            currentRelics.Clear();
            ResetRewardGrowthCounts();
            ClearRewardChoices();
            ClearRestChoices();
            ClearPreparedRoomChoices();
            ApplyMetaProgressionUpgrades();

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
            shopRewardDraftOpen = false;
            shopRewardPurchasedThisRoom = false;
            waitingForRest = false;
            safeRestUsedThisRoom = false;
            if (nextRoomType != RoomType.ShopRoom)
            {
                lastShopFeedbackMessage = string.Empty;
            }
            if (nextRoomType != RoomType.SafeRoom)
            {
                lastSafeRestFeedbackMessage = string.Empty;
            }

            roomIndex++;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearRestChoices();
            ClearPreparedRoomChoices();

            int roomNumber = roomIndex + 1;

            float restoredHealth = ApplyRoomEntryEffect(nextRoomType, currentRoomModifier);
            ApplyRoomModifierEntryEffect(currentRoomModifier);
            ApplyRelicRoomEntryEffects(nextRoomType);
            SetRoomModifierFeedback(currentRoomModifier, restoredHealth);
            SetRoomFeedback(AppendRelicFeedback(
                BuildRoomFeedbackMessage(nextRoomType, restoredHealth, currentRoomModifier),
                lastRelicFeedbackMessage));
            if (currentRoomModifier == RoomModifierType.None)
            {
                ShowCompanionSupportRoomFeedback(nextRoomType, restoredHealth);
            }
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
            waitingForRest = false;
            ClearRewardChoices();
            ClearRestChoices();
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
            GrantRoomClearSupplies(roomType);

            string roomClearedFeedback = BuildRoomClearedFeedbackMessage(roomType, roomNumber);
            if (ShouldShowCombatClearFeedback(roomType))
            {
                SetRoomFeedback(roomClearedFeedback);
            }

            if (logRunMessages)
            {
                Debug.Log(roomClearedFeedback, this);
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

        private void GrantRoomClearSupplies(RoomType roomType)
        {
            int supplyGain = RunSupplyRules.GetSupplyGain(roomType);
            if (supplyGain <= 0)
            {
                return;
            }

            currentSupplies += supplyGain;
            lastShopFeedbackMessage = string.Empty;
        }

        public void SelectReward(int index)
        {
            if (!waitingForReward || index < 0 || index >= currentRewardChoices.Count)
            {
                return;
            }

            RunRewardChoice reward = currentRewardChoices[index];
            RoomType rewardSourceRoomType = currentRewardSourceRoomType;
            bool isShopReward = rewardSourceRoomType == RoomType.ShopRoom;
            if (isShopReward && !TrySpendShopReward())
            {
                return;
            }

            ApplyReward(reward.RewardType);
            RunSessionState.RecordRewardSelected(reward.Title);
            if (isShopReward)
            {
                shopRewardDraftOpen = false;
                shopRewardPurchasedThisRoom = true;
            }

            waitingForReward = false;
            ClearRewardChoices();

            if (logRunMessages)
            {
                Debug.Log($"Reward selected: {reward.Title}. {reward.Description}", this);
            }

            RewardSelected?.Invoke(this, reward);
            BeginNextRoomChoiceFlow();
        }

        public bool OpenShopRewardDraft()
        {
            if (!CanOpenCurrentShopRewardDraft())
            {
                if (CurrentRoomType == RoomType.ShopRoom && shopRewardPurchasedThisRoom)
                {
                    lastShopFeedbackMessage = "Shop already used. Continue to the next route.";
                    SetRoomFeedback(lastShopFeedbackMessage);
                }

                return false;
            }

            shopRewardDraftOpen = true;
            waitingForNextRoom = false;
            ClearPreparedRoomChoices();
            PrepareRewardChoices(RoomType.ShopRoom);
            SetRoomFeedback($"Supply Shop: choose one purchase or press Esc to skip. {RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies)}.");
            ShowCompanionShopFeedback($"AI: Shop is open. {RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies).ToLowerInvariant()}.");
            return true;
        }

        public void CloseShopRewardDraft()
        {
            if (!IsCurrentRewardShopPurchase && !shopRewardDraftOpen)
            {
                return;
            }

            shopRewardDraftOpen = false;
            waitingForReward = false;
            ClearRewardChoices();
            lastShopFeedbackMessage = $"Skipped shop purchase. Supplies {currentSupplies}.";
            SetRoomFeedback($"Supply Shop: {lastShopFeedbackMessage} Choose the next route when ready.");
            ShowCompanionShopFeedback("AI: Skipping the shop is fine. We can use those supplies later.");
            BeginNextRoomChoiceFlow();
        }

        private bool CanOpenCurrentShopRewardDraft()
        {
            return !runCompleted
                && useRoomRewards
                && CurrentRoomType == RoomType.ShopRoom
                && !waitingForReward
                && !shopRewardPurchasedThisRoom;
        }

        private bool TrySpendShopReward()
        {
            if (RunSupplyRules.CanAffordShopReward(currentSupplies))
            {
                currentSupplies = RunSupplyRules.SpendShopReward(currentSupplies);
                lastShopFeedbackMessage = RunSupplyRules.BuildShopSpentLabel(currentSupplies);
                SetRoomFeedback($"Supply Purchase: {lastShopFeedbackMessage}");
                ShowCompanionShopFeedback($"AI: Bought the upgrade. supplies remaining {currentSupplies}.");
                return true;
            }

            lastShopFeedbackMessage = RunSupplyRules.BuildShopBlockedLabel(currentSupplies);
            SetRoomFeedback($"Supply Purchase Blocked: {lastShopFeedbackMessage}");
            ShowCompanionShopFeedback("AI: Not enough supplies. We should take another combat route first.");
            return false;
        }

        public bool AcquireRelic(RunRelicType relicType)
        {
            if (HasRelic(relicType))
            {
                lastRelicFeedbackMessage = $"{RunRelicRules.GetTitle(relicType)} already owned.";
                lastRelicBannerMessage = RunRelicRules.GetEffectBanner(relicType, "already owned");
                return false;
            }

            currentRelics.Add(relicType);
            string title = RunRelicRules.GetTitle(relicType);
            lastRelicFeedbackMessage = $"Relic acquired: {title}. {RunRelicRules.GetDescription(relicType)}";
            lastRelicBannerMessage = RunRelicRules.GetPickupBanner(relicType);
            RunSessionState.RecordRelicCollected(title);
            ShowCompanionShopFeedback($"AI: Relic acquired - {RunRelicRules.GetHudLabel(relicType)}.");
            return true;
        }

        public bool HasRelic(RunRelicType relicType)
        {
            return currentRelics.Contains(relicType);
        }

        public bool OpenSafeRestDraft()
        {
            if (!CanOpenCurrentSafeRestDraft())
            {
                if (CurrentRoomType == RoomType.SafeRoom && safeRestUsedThisRoom)
                {
                    lastSafeRestFeedbackMessage = "Rest point already used. Continue to the next route.";
                    SetRoomFeedback(lastSafeRestFeedbackMessage);
                }

                return false;
            }

            waitingForRest = true;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();
            PrepareSafeRestChoices();
            SetRoomFeedback("Safe Rest Point: choose one rest option or press Esc to close. You can continue the route after choosing.");
            ShowCompanionShopFeedback("AI: Rest point is ready. Choose what we need before moving on.");
            return true;
        }

        public void CloseSafeRestDraft()
        {
            if (!waitingForRest)
            {
                return;
            }

            waitingForRest = false;
            ClearRestChoices();
            lastSafeRestFeedbackMessage = $"Skipped rest point. Supplies {currentSupplies}.";
            SetRoomFeedback($"Safe Rest Point: {lastSafeRestFeedbackMessage} Choose the next route when ready.");
            ShowCompanionShopFeedback("AI: We can skip rest for now. The route is open.");
            BeginNextRoomChoiceFlow();
        }

        public void SelectSafeRestChoice(int index)
        {
            if (!waitingForRest || index < 0 || index >= currentRestChoices.Count)
            {
                return;
            }

            SafeRestChoice choice = currentRestChoices[index];
            ApplySafeRestChoice(choice);
            safeRestUsedThisRoom = true;
            waitingForRest = false;
            ClearRestChoices();

            RestChoiceSelected?.Invoke(this, choice);
            BeginNextRoomChoiceFlow();
        }

        private bool CanOpenCurrentSafeRestDraft()
        {
            return !runCompleted
                && CurrentRoomType == RoomType.SafeRoom
                && !waitingForRest
                && !safeRestUsedThisRoom;
        }

        private void ApplySafeRestChoice(SafeRestChoice choice)
        {
            float restoredHealth = 0f;
            lastRelicBannerMessage = string.Empty;

            switch (choice.ChoiceType)
            {
                case SafeRestChoiceType.Rest:
                    restoredHealth = HealPlayer(GetSafeRestHealAmount());
                    lastSafeRestFeedbackMessage = $"Rested: recovered {restoredHealth:0} HP. Supplies {currentSupplies}.";
                    SetRoomModifierFeedback(currentRoomModifier, restoredHealth);
                    ShowCompanionShopFeedback($"AI: Rest complete. HP recovered {restoredHealth:0}; we can move again.");
                    break;
                case SafeRestChoiceType.Talk:
                    ApplySafeRestTalk();
                    lastSafeRestFeedbackMessage = "Talked with AI: Trust +1, Affection +1, Reliable memory +1.";
                    ShowCompanionShopFeedback("AI: I will remember this. We move together.");
                    break;
                case SafeRestChoiceType.Prepare:
                    int supplyGain = 1;
                    if (HasRelic(RunRelicType.FieldBackpack))
                    {
                        supplyGain += RunRelicRules.FieldBackpackPrepareBonusSupplies;
                        lastRelicFeedbackMessage = $"Field Backpack added +{RunRelicRules.FieldBackpackPrepareBonusSupplies} supply.";
                        lastRelicBannerMessage = RunRelicRules.GetEffectBanner(
                            RunRelicType.FieldBackpack,
                            $"+{RunRelicRules.FieldBackpackPrepareBonusSupplies} supply");
                    }

                    currentSupplies += supplyGain;
                    lastSafeRestFeedbackMessage = HasRelic(RunRelicType.FieldBackpack)
                        ? $"Prepared: Supplies +{supplyGain} with Field Backpack. Supplies {currentSupplies}."
                        : $"Prepared: Supplies +{supplyGain}. Supplies {currentSupplies}.";
                    ShowCompanionShopFeedback($"AI: Supplies prepared. We have {currentSupplies} now.");
                    break;
                default:
                    lastSafeRestFeedbackMessage = "Rest point used.";
                    break;
            }

            SetRoomFeedback($"Safe Rest Point: {lastSafeRestFeedbackMessage} Choose the next route when ready.");
            if (logRunMessages)
            {
                Debug.Log($"Safe rest selected: {choice.Title}. {lastSafeRestFeedbackMessage}", this);
            }
        }

        private float HealPlayer(float healAmount)
        {
            GameObject player = GameObject.Find("Player");
            HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
            if (health == null)
            {
                return 0f;
            }

            float before = health.CurrentHealth;
            health.Heal(healAmount);
            return health.CurrentHealth - before;
        }

        private float GetSafeRestHealAmount()
        {
            return safeRoomHealAmount * RoomModifierRules.GetSafeHealMultiplier(currentRoomModifier);
        }

        private static void ApplySafeRestTalk()
        {
            CompanionRelationship relationship = UnityEngine.Object.FindAnyObjectByType<CompanionRelationship>();
            if (relationship == null)
            {
                return;
            }

            relationship.ApplyMemoryEvent("Safe Room Talk", 1, 1, RelationshipMemoryTag.Reliable);
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

            SetRoomFeedback(AppendRouteOpenedFeedback(lastRoomFeedbackMessage));
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
                && (roomType == RoomType.BattleRoom || roomType == RoomType.EliteRoom);
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
            waitingForRest = false;
            ClearRewardChoices();
            ClearRestChoices();
            ClearPreparedRoomChoices();

            CompanionBossPostFightSettlement postFightSettlement = FindAnyObjectByType<CompanionBossPostFightSettlement>();
            if (postFightSettlement != null)
            {
                postFightSettlement.SettleBossVictory();
            }

            CompanionRelationship relationship = FindAnyObjectByType<CompanionRelationship>();
            int finalTrust = relationship != null ? relationship.Trust : -1;
            int finalAffection = relationship != null ? relationship.Affection : -1;
            RunSessionState.RecordGrowthRouteSummary(
                BuildCurrentGrowthRouteSummaryLabel(),
                hasActiveGrowthRoute ? BuildCurrentGrowthRouteEffectLabel() : string.Empty,
                CurrentGrowthRouteSpecializationCount,
                activeGrowthRouteLevel);
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
            currentRewardSourceRoomType = sourceRoomType;
            currentRewardChoices.Clear();
            if (sourceRoomType == RoomType.ShopRoom)
            {
                lastShopFeedbackMessage = RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies);
            }

            List<RunRewardType> candidates = BuildRewardCandidateList();
            int targetCount = GetRewardChoiceTargetCount(sourceRoomType, candidates.Count);
            if (hasActiveGrowthRoute && currentRewardChoices.Count < targetCount)
            {
                currentRewardChoices.Add(CreateRewardChoice(RunRewardType.GrowthRouteSpecialization));
                candidates.Remove(RunRewardType.GrowthRouteSpecialization);
            }

            RunRewardType? buildRewardType = GetCurrentBuildRewardType();
            if (buildRewardType.HasValue && currentRewardChoices.Count < targetCount)
            {
                currentRewardChoices.Add(CreateRewardChoice(buildRewardType.Value));
                candidates.Remove(buildRewardType.Value);
            }

            TryAddRewardChoiceFromCategory(candidates, RunRewardCategory.Relic, targetCount);

            if (hasActiveGrowthRoute)
            {
                TryAddRewardChoiceFromCategory(candidates, activeGrowthRouteCategory, targetCount);
            }

            if (targetCount >= 2)
            {
                TryAddRewardChoiceFromCategories(
                    candidates,
                    targetCount,
                    RunRewardCategory.Player,
                    RunRewardCategory.Survival);
                TryAddRewardChoiceFromCategory(candidates, RunRewardCategory.Counterplay, targetCount);
            }

            if (targetCount >= 3)
            {
                TryAddRewardChoiceFromCategory(candidates, RunRewardCategory.Companion, targetCount);
            }

            while (currentRewardChoices.Count < targetCount && candidates.Count > 0)
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

        private void PrepareSafeRestChoices()
        {
            currentRestChoices.Clear();
            float healAmount = GetSafeRestHealAmount();
            currentRestChoices.Add(new SafeRestChoice(
                SafeRestChoiceType.Rest,
                "Rest",
                $"Recover up to {healAmount:0} HP.",
                "Stabilize the player before the next fight."));
            currentRestChoices.Add(new SafeRestChoice(
                SafeRestChoiceType.Talk,
                "Talk",
                "Talk with the AI companion. Trust +1, Affection +1, Reliable memory +1.",
                "Strengthen the AI relationship instead of healing."));
            currentRestChoices.Add(new SafeRestChoice(
                SafeRestChoiceType.Prepare,
                "Prepare",
                "Prepare supplies for the next route. Supplies +1.",
                "Trade recovery for more shop flexibility later."));

            RestChoicesPrepared?.Invoke(this, currentRestChoices);
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
            currentRewardSourceRoomType = RoomType.BattleRoom;
            RewardChoicesCleared?.Invoke(this);
        }

        private void ClearRestChoices()
        {
            if (currentRestChoices.Count == 0)
            {
                return;
            }

            currentRestChoices.Clear();
            RestChoicesCleared?.Invoke(this);
        }

        private List<RunRewardType> BuildRewardCandidateList()
        {
            List<RunRewardType> candidates = new List<RunRewardType>(12);

            if (selectableRewards != null)
            {
                for (int i = 0; i < selectableRewards.Length; i++)
                {
                    AddRewardCandidate(candidates, selectableRewards[i]);
                }
            }

            for (int i = 0; i < CounterplayRewardTypes.Length; i++)
            {
                AddRewardCandidate(candidates, CounterplayRewardTypes[i]);
            }

            RunRewardType? buildRewardType = GetCurrentBuildRewardType();
            if (buildRewardType.HasValue)
            {
                AddRewardCandidate(candidates, buildRewardType.Value);
            }

            if (hasActiveGrowthRoute)
            {
                AddRewardCandidate(candidates, RunRewardType.GrowthRouteSpecialization);
            }

            AddRelicRewardCandidates(candidates);

            return candidates;
        }

        private void AddRelicRewardCandidates(List<RunRewardType> candidates)
        {
            RunRelicType[] relics = selectableRelics != null && selectableRelics.Length > 0
                ? selectableRelics
                : RunRelicRules.AllRelics;

            for (int i = 0; i < relics.Length; i++)
            {
                if (HasRelic(relics[i]))
                {
                    continue;
                }

                AddRewardCandidate(candidates, RunRelicRules.GetRewardType(relics[i]));
            }
        }

        private static void AddRewardCandidate(List<RunRewardType> candidates, RunRewardType reward)
        {
            if (!candidates.Contains(reward))
            {
                candidates.Add(reward);
            }
        }

        private RunRewardChoice CreateRewardChoice(RunRewardType rewardType)
        {
            RunRewardChoice legacyChoice = CreateLegacyRewardChoice(rewardType);
            return new RunRewardChoice(
                rewardType,
                legacyChoice.Title,
                legacyChoice.Description,
                GetRewardCategory(rewardType),
                BuildRewardPreviewLine(rewardType),
                GetRewardGrowthTag(rewardType));
        }

        private bool TryAddRewardChoiceFromCategories(
            List<RunRewardType> candidates,
            int targetCount,
            params RunRewardCategory[] categories)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                if (TryAddRewardChoiceFromCategory(candidates, categories[i], targetCount))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryAddRewardChoiceFromCategory(
            List<RunRewardType> candidates,
            RunRewardCategory category,
            int targetCount)
        {
            if (currentRewardChoices.Count >= targetCount || candidates.Count == 0)
            {
                return false;
            }

            List<int> matchingIndexes = new List<int>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (GetRewardCategory(candidates[i]) == category)
                {
                    matchingIndexes.Add(i);
                }
            }

            if (matchingIndexes.Count == 0)
            {
                return false;
            }

            int selectedIndex = matchingIndexes[UnityEngine.Random.Range(0, matchingIndexes.Count)];
            currentRewardChoices.Add(CreateRewardChoice(candidates[selectedIndex]));
            candidates.RemoveAt(selectedIndex);
            return true;
        }

        private RunRewardChoice CreateLegacyRewardChoice(RunRewardType rewardType)
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
                case RunRewardType.DashCooldown:
                    return new RunRewardChoice(
                        rewardType,
                        $"Counterplay: Dash Cooldown -{(1f - counterplayDashCooldownMultiplier) * 100f:0}%",
                        "Dash returns faster, making projectile dodges and repositioning safer.");
                case RunRewardType.RecoveryWindow:
                    return new RunRewardChoice(
                        rewardType,
                        $"Counterplay: Recovery +{counterplayRecoveryDurationBonus:0.00}s",
                        "After taking a hit, follow-up enemy damage is blocked for a little longer.");
                case RunRewardType.DodgeDamageBoost:
                    return new RunRewardChoice(
                        rewardType,
                        $"Counterplay: Dodge Strike x{counterplayDodgeDamageMultiplier:0.##}",
                        $"Successful dodges empower player attacks for {counterplayDodgeBoostDuration:0.0}s.");
                case RunRewardType.GuardOpeningDamage:
                    return new RunRewardChoice(
                        rewardType,
                        $"Counterplay: Guard Opening x{counterplayGuardOpeningDamageMultiplier:0.##}",
                        "Hitting a vulnerable Guard deals extra counterplay damage.");
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
                case RunRewardType.GrowthRouteSpecialization:
                    return CreateGrowthRouteSpecializationChoice();
                case RunRewardType.RelicFirstAidCharm:
                case RunRewardType.RelicSyncMark:
                case RunRewardType.RelicFieldBackpack:
                    RunRelicRules.TryGetRelicType(rewardType, out RunRelicType relicType);
                    return new RunRewardChoice(
                        rewardType,
                        RunRelicRules.GetTitle(relicType),
                        RunRelicRules.GetDescription(relicType));
                default:
                    return new RunRewardChoice(rewardType, rewardType.ToString(), "未知奖励。");
            }
        }

        private RunRewardCategory GetRewardCategory(RunRewardType rewardType)
        {
            switch (rewardType)
            {
                case RunRewardType.GrowthRouteSpecialization:
                    return hasActiveGrowthRoute ? activeGrowthRouteCategory : RunRewardCategory.Player;
                case RunRewardType.MaxHealth:
                case RunRewardType.BondRescueHealth:
                    return RunRewardCategory.Survival;
                case RunRewardType.CompanionCooldown:
                    return RunRewardCategory.Companion;
                case RunRewardType.DashCooldown:
                case RunRewardType.RecoveryWindow:
                case RunRewardType.DodgeDamageBoost:
                case RunRewardType.GuardOpeningDamage:
                    return RunRewardCategory.Counterplay;
                case RunRewardType.GuardianBuildUpgrade:
                case RunRewardType.SuppressorBuildUpgrade:
                case RunRewardType.LinkBuildUpgrade:
                    return RunRewardCategory.Build;
                case RunRewardType.RelicFirstAidCharm:
                case RunRewardType.RelicSyncMark:
                case RunRewardType.RelicFieldBackpack:
                    return RunRewardCategory.Relic;
                default:
                    return RunRewardCategory.Player;
            }
        }

        private string GetRewardGrowthTag(RunRewardType rewardType)
        {
            switch (rewardType)
            {
                case RunRewardType.MaxHealth:
                    return "survival-max-health";
                case RunRewardType.PlayerDamage:
                    return "player-damage";
                case RunRewardType.MoveSpeed:
                    return "player-move-speed";
                case RunRewardType.CompanionCooldown:
                    return "ai-support-cooldown";
                case RunRewardType.BondRescueHealth:
                    return "survival-bond-rescue";
                case RunRewardType.DashCooldown:
                    return "counterplay-dash-cooldown";
                case RunRewardType.RecoveryWindow:
                    return "counterplay-recovery-window";
                case RunRewardType.DodgeDamageBoost:
                    return "counterplay-dodge-strike";
                case RunRewardType.GuardOpeningDamage:
                    return "counterplay-guard-opening";
                case RunRewardType.GuardianBuildUpgrade:
                    return "build-guardian";
                case RunRewardType.SuppressorBuildUpgrade:
                    return "build-suppressor";
                case RunRewardType.LinkBuildUpgrade:
                    return "build-link";
                case RunRewardType.GrowthRouteSpecialization:
                    return $"route-special-{RunRewardChoice.GetCategoryLabel(GetRewardCategory(rewardType)).ToLowerInvariant()}";
                case RunRewardType.RelicFirstAidCharm:
                case RunRewardType.RelicSyncMark:
                case RunRewardType.RelicFieldBackpack:
                    RunRelicRules.TryGetRelicType(rewardType, out RunRelicType relicType);
                    return $"relic-{relicType.ToString().ToLowerInvariant()}";
                default:
                    return "reward";
            }
        }

        private string BuildRewardPreviewLine(RunRewardType rewardType)
        {
            GameObject player = GameObject.Find("Player");
            switch (rewardType)
            {
                case RunRewardType.MaxHealth:
                {
                    HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
                    return health != null
                        ? FormatStatChange("Max HP", health.MaxHealth, health.MaxHealth + maxHealthReward, "0")
                        : $"Max HP: +{maxHealthReward:0}";
                }
                case RunRewardType.PlayerDamage:
                {
                    PlayerCombat2D combat = player != null ? player.GetComponent<PlayerCombat2D>() : null;
                    return combat != null
                        ? FormatStatChange("Damage", combat.Damage, combat.Damage * playerDamageRewardMultiplier, "0.#")
                        : $"Damage: +{(playerDamageRewardMultiplier - 1f) * 100f:0}%";
                }
                case RunRewardType.MoveSpeed:
                {
                    PlayerMovement2D movement = player != null ? player.GetComponent<PlayerMovement2D>() : null;
                    return movement != null
                        ? FormatStatChange("Move Speed", movement.MoveSpeed, movement.MoveSpeed * moveSpeedRewardMultiplier, "0.0")
                        : $"Move Speed: +{(moveSpeedRewardMultiplier - 1f) * 100f:0}%";
                }
                case RunRewardType.CompanionCooldown:
                {
                    CompanionCombat companionCombat = FindAnyObjectByType<CompanionCombat>();
                    return companionCombat != null
                        ? FormatStatChange("AI Cooldown", companionCombat.Cooldown, companionCombat.Cooldown * companionCooldownRewardMultiplier, "0.00", "s")
                        : $"AI Cooldown: -{(1f - companionCooldownRewardMultiplier) * 100f:0}%";
                }
                case RunRewardType.BondRescueHealth:
                {
                    BondRescueSystem rescueSystem = player != null ? player.GetComponent<BondRescueSystem>() : null;
                    return rescueSystem != null
                        ? FormatStatChange("Rescue HP", rescueSystem.RescueHealth, rescueSystem.RescueHealth + bondRescueHealthReward, "0")
                        : $"Rescue HP: +{bondRescueHealthReward:0}";
                }
                case RunRewardType.DashCooldown:
                {
                    PlayerMovement2D movement = player != null ? player.GetComponent<PlayerMovement2D>() : null;
                    return movement != null
                        ? FormatStatChange("Dash CD", movement.DashCooldown, movement.DashCooldown * counterplayDashCooldownMultiplier, "0.00", "s")
                        : $"Dash CD: -{(1f - counterplayDashCooldownMultiplier) * 100f:0}%";
                }
                case RunRewardType.RecoveryWindow:
                {
                    PlayerCounterplayFeedback counterplay = player != null ? player.GetComponent<PlayerCounterplayFeedback>() : null;
                    return counterplay != null
                        ? FormatStatChange(
                            "Recovery",
                            counterplay.PostHitInvulnerabilityDuration,
                            counterplay.PostHitInvulnerabilityDuration + counterplayRecoveryDurationBonus,
                            "0.00",
                            "s")
                        : $"Recovery: +{counterplayRecoveryDurationBonus:0.00}s";
                }
                case RunRewardType.DodgeDamageBoost:
                {
                    PlayerCounterplayFeedback counterplay = player != null ? player.GetComponent<PlayerCounterplayFeedback>() : null;
                    float currentMultiplier = counterplay != null ? counterplay.DodgeDamageMultiplier : 1f;
                    float currentDuration = counterplay != null ? counterplay.DodgeDamageBoostDuration : 0f;
                    float nextDuration = Mathf.Max(currentDuration, counterplayDodgeBoostDuration);
                    return $"Dodge Strike: x{currentMultiplier:0.##} -> x{currentMultiplier * counterplayDodgeDamageMultiplier:0.##} / {nextDuration:0.0}s";
                }
                case RunRewardType.GuardOpeningDamage:
                {
                    PlayerCounterplayFeedback counterplay = player != null ? player.GetComponent<PlayerCounterplayFeedback>() : null;
                    float currentMultiplier = counterplay != null ? counterplay.GuardOpeningDamageMultiplier : 1f;
                    return $"Guard Opening: x{currentMultiplier:0.##} -> x{currentMultiplier * counterplayGuardOpeningDamageMultiplier:0.##}";
                }
                case RunRewardType.GuardianBuildUpgrade:
                    return BuildBuildLevelPreview(CompanionSkillTendency.Guardian);
                case RunRewardType.SuppressorBuildUpgrade:
                    return BuildBuildLevelPreview(CompanionSkillTendency.Suppressor);
                case RunRewardType.LinkBuildUpgrade:
                    return BuildBuildLevelPreview(CompanionSkillTendency.Link);
                case RunRewardType.GrowthRouteSpecialization:
                    return BuildGrowthRouteSpecializationPreviewLine();
                case RunRewardType.RelicFirstAidCharm:
                case RunRewardType.RelicSyncMark:
                case RunRewardType.RelicFieldBackpack:
                    RunRelicRules.TryGetRelicType(rewardType, out RunRelicType relicType);
                    return $"Relic: {RunRelicRules.GetHudLabel(relicType)}.";
                default:
                    return string.Empty;
            }
        }

        private static string FormatStatChange(
            string label,
            float before,
            float after,
            string numberFormat,
            string suffix = "")
        {
            return $"{label}: {before.ToString(numberFormat)}{suffix} -> {after.ToString(numberFormat)}{suffix}";
        }

        private static string BuildBuildLevelPreview(CompanionSkillTendency tendency)
        {
            int currentLevel = CompanionRunBuildState.GetUpgradeLevel(tendency);
            return $"{tendency} Build: Lv{currentLevel} -> Lv{currentLevel + 1}";
        }

        private RunRewardChoice CreateGrowthRouteSpecializationChoice()
        {
            switch (activeGrowthRouteCategory)
            {
                case RunRewardCategory.Player:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "Player Route Core",
                        "Route-exclusive reward: deepen player attack damage for this run.");
                case RunRewardCategory.Companion:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "AI Route Core",
                        "Route-exclusive reward: make companion attacks cycle faster this run.");
                case RunRewardCategory.Counterplay:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "Counterplay Route Core",
                        "Route-exclusive reward: extend dodge punish windows and counter damage.");
                case RunRewardCategory.Survival:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "Survival Route Core",
                        "Route-exclusive reward: improve bond rescue retained HP.");
                case RunRewardCategory.Build:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "Build Route Core",
                        "Route-exclusive reward: add temporary AI Build route power.");
                default:
                    return new RunRewardChoice(
                        RunRewardType.GrowthRouteSpecialization,
                        "Growth Route Core",
                        "Route-exclusive reward: deepen the active route.");
            }
        }

        private string BuildGrowthRouteSpecializationPreviewLine()
        {
            switch (activeGrowthRouteCategory)
            {
                case RunRewardCategory.Player:
                    return $"Damage route: x{PlayerRouteDamageMultiplier:0.##} -> x{PlayerRouteDamageMultiplier + routeSpecializationPlayerDamageBonus:0.##}";
                case RunRewardCategory.Companion:
                    return $"AI Cooldown route: x{CompanionRouteCooldownMultiplier:0.##} -> x{CompanionRouteCooldownMultiplier * routeSpecializationCompanionCooldownMultiplier:0.##}";
                case RunRewardCategory.Counterplay:
                    return $"Counter route: +{CounterplayRouteDodgeBoostDurationBonus:0.0}s -> +{CounterplayRouteDodgeBoostDurationBonus + routeSpecializationCounterplayDodgeBoostDurationBonus:0.0}s / x{CounterplayRouteDamageMultiplier + routeSpecializationCounterplayDamageBonus:0.##}";
                case RunRewardCategory.Survival:
                    return $"Rescue HP route: +{SurvivalRouteRescueHealthBonus:0} -> +{SurvivalRouteRescueHealthBonus + routeSpecializationSurvivalRescueHealthBonus:0}";
                case RunRewardCategory.Build:
                    return $"Build route: +{BuildRouteBonusLevel} -> +{BuildRouteBonusLevel + routeSpecializationBuildBonusLevel}";
                default:
                    return "Route specialization: active route grows stronger";
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
                case RunRewardType.DashCooldown:
                    ApplyCounterplayDashCooldownReward(player);
                    break;
                case RunRewardType.RecoveryWindow:
                    ApplyCounterplayRecoveryReward(player);
                    break;
                case RunRewardType.DodgeDamageBoost:
                    ApplyCounterplayDodgeBoostReward(player);
                    break;
                case RunRewardType.GuardOpeningDamage:
                    ApplyCounterplayGuardOpeningReward(player);
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
                case RunRewardType.GrowthRouteSpecialization:
                    ApplyGrowthRouteSpecializationReward();
                    break;
                case RunRewardType.RelicFirstAidCharm:
                case RunRewardType.RelicSyncMark:
                case RunRewardType.RelicFieldBackpack:
                    if (RunRelicRules.TryGetRelicType(rewardType, out RunRelicType relicType))
                    {
                        AcquireRelic(relicType);
                    }

                    break;
            }

            if (IsCounterplayReward(rewardType))
            {
                ShowCompanionCounterplayRewardFeedback(rewardType);
            }

            RecordRewardGrowth(GetRewardCategory(rewardType));
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

        private void ApplyGrowthRouteSpecializationReward()
        {
            if (!hasActiveGrowthRoute)
            {
                return;
            }

            switch (activeGrowthRouteCategory)
            {
                case RunRewardCategory.Player:
                    playerRouteSpecializationCount++;
                    break;
                case RunRewardCategory.Companion:
                    companionRouteSpecializationCount++;
                    break;
                case RunRewardCategory.Counterplay:
                    counterplayRouteSpecializationCount++;
                    break;
                case RunRewardCategory.Survival:
                    survivalRouteSpecializationCount++;
                    break;
                case RunRewardCategory.Build:
                    buildRouteSpecializationCount++;
                    break;
            }
        }

        private float ApplyRoomEntryEffect(RoomType roomType, RoomModifierType roomModifier)
        {
            return 0f;
        }

        private float ApplyRelicRoomEntryEffects(RoomType roomType)
        {
            lastRelicFeedbackMessage = string.Empty;
            lastRelicBannerMessage = string.Empty;

            if (!HasRelic(RunRelicType.FirstAidCharm) || !IsCombatRoom(roomType))
            {
                return 0f;
            }

            float restoredHealth = HealPlayer(firstAidCharmHealAmount);
            lastRelicFeedbackMessage = restoredHealth > 0f
                ? $"First Aid Charm restored {restoredHealth:0} HP."
                : "First Aid Charm ready, but HP is full.";
            lastRelicBannerMessage = restoredHealth > 0f
                ? RunRelicRules.GetEffectBanner(RunRelicType.FirstAidCharm, $"+{restoredHealth:0} HP")
                : RunRelicRules.GetEffectBanner(RunRelicType.FirstAidCharm, "ready at full HP");
            return restoredHealth;
        }

        private static bool IsCombatRoom(RoomType roomType)
        {
            return roomType == RoomType.BattleRoom
                || roomType == RoomType.EliteRoom
                || roomType == RoomType.BossRoom;
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
                        AppendTacticFeedback(
                            $"Combat Started - Battle Room: Enemy Types Melee/Ranged. Read each enemy warning, evade Melee lunges, and dodge visible projectiles, then clear enemies for {GetRewardChoiceTargetCount(roomType, roomModifier, BuildRewardCandidateList().Count)} reward options."),
                        modifierLine);
                case RoomType.EliteRoom:
                    return AppendModifierFeedback(
                        AppendTacticFeedback(
                            $"Combat Started - Elite Room: Enemy Types Guard/Ranged. Blocked frontal hits deal less damage; punish the Guard opening after it attacks for {GetRewardChoiceTargetCount(roomType, roomModifier, BuildRewardCandidateList().Count)} reward options."),
                        modifierLine);
                case RoomType.SafeRoom:
                    return AppendModifierFeedback(
                        $"Safe Room: no enemies here. Move to the rest point and interact to recover, talk, or prepare. You can also skip through the next-route portal. Supplies {currentSupplies}.",
                        modifierLine);
                case RoomType.ShopRoom:
                    return AppendModifierFeedback(
                        $"Supply Room: no enemies. Move to the shop and interact to buy 1 reward, or skip shopping through the next-route portal. {RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies)}.",
                        modifierLine);
                case RoomType.BossRoom:
                    return AppendTacticFeedback("Boss Room: final challenge. Defeat the boss to complete the run.");
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

        private static string AppendRelicFeedback(string baseMessage, string relicLine)
        {
            return string.IsNullOrWhiteSpace(relicLine)
                ? baseMessage
                : $"{baseMessage} Relic {relicLine}";
        }

        private static string AppendTacticFeedback(string baseMessage)
        {
            string tacticLine = CompanionSkillTendencyRules.GetRunFeedbackLine(CompanionRunBuildState.CurrentTendency);
            return string.IsNullOrWhiteSpace(tacticLine)
                ? baseMessage
                : $"{baseMessage} {tacticLine}";
        }

        private string BuildRoomClearedFeedbackMessage(RoomType roomType, int roomNumber)
        {
            string supplyLine = BuildRoomClearSupplyFeedback(roomType);
            string feedback;
            if (ShouldCompleteRun(roomNumber))
            {
                feedback = AppendSupplyFeedback($"Room Clear - room #{roomNumber} cleared. Run complete.", supplyLine);
                return AppendRelicFeedback(feedback, lastRelicFeedbackMessage);
            }

            if (IsNextRoomBossRoom())
            {
                feedback = AppendSupplyFeedback(
                    $"Room Clear - room #{roomNumber} cleared. Find the final portal to challenge the boss.",
                    supplyLine);
                return AppendRelicFeedback(feedback, lastRelicFeedbackMessage);
            }

            if (ShouldOfferReward(roomType))
            {
                feedback = AppendTacticClearFeedback(AppendSupplyFeedback(
                    $"Room Clear - room #{roomNumber} cleared. Reward Available. Choose a reward, then select the next route.",
                    supplyLine));
                return AppendRelicFeedback(feedback, lastRelicFeedbackMessage);
            }

            feedback = useRoomChoicePortal
                ? AppendTacticClearFeedback(AppendSupplyFeedback(
                    $"Room Clear - room #{roomNumber} cleared. Find the next-room portal to choose a route.",
                    supplyLine))
                : AppendTacticClearFeedback(AppendSupplyFeedback(
                    $"Room Clear - room #{roomNumber} cleared. Press {nextRoomKey} to enter the next room.",
                    supplyLine));
            return AppendRelicFeedback(feedback, lastRelicFeedbackMessage);
        }

        private string BuildRoomClearSupplyFeedback(RoomType roomType)
        {
            string supplyGainLabel = RunSupplyRules.BuildSupplyGainLabel(roomType);
            return string.IsNullOrWhiteSpace(supplyGainLabel)
                ? string.Empty
                : $"{supplyGainLabel}. Supplies {currentSupplies}.";
        }

        private static string AppendSupplyFeedback(string baseMessage, string supplyLine)
        {
            return string.IsNullOrWhiteSpace(supplyLine)
                ? baseMessage
                : $"{baseMessage} {supplyLine}";
        }

        private static string AppendRouteOpenedFeedback(string baseMessage)
        {
            string message = string.IsNullOrWhiteSpace(baseMessage)
                ? "Route Opened."
                : baseMessage;
            return message.Contains("Route Opened")
                ? message
                : $"{message} Route Opened.";
        }

        private static string AppendTacticClearFeedback(string baseMessage)
        {
            string tacticLine = CompanionSkillTendencyRules.GetRunClearRewardLine(CompanionRunBuildState.CurrentTendency);
            return string.IsNullOrWhiteSpace(tacticLine)
                ? baseMessage
                : $"{baseMessage} {tacticLine}";
        }

        private static bool ShouldShowCombatClearFeedback(RoomType roomType)
        {
            return roomType == RoomType.BattleRoom
                || roomType == RoomType.EliteRoom
                || roomType == RoomType.BossRoom;
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

        private void ShowCompanionSupportRoomFeedback(RoomType roomType, float restoredHealth)
        {
            string message = BuildCompanionSupportRoomLine(roomType, restoredHealth);
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ShowCompanionShopFeedback(message);
        }

        private string BuildCompanionSupportRoomLine(RoomType roomType, float restoredHealth)
        {
            switch (roomType)
            {
                case RoomType.SafeRoom:
                    return $"AI: We can recover at the rest point, talk, or prepare. supplies stay at {currentSupplies}.";
                case RoomType.ShopRoom:
                    return $"AI: Check supplies before buying. {RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies).ToLowerInvariant()}.";
                default:
                    return string.Empty;
            }
        }

        private static void ShowCompanionShopFeedback(string message)
        {
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

        private static bool IsCounterplayReward(RunRewardType rewardType)
        {
            for (int i = 0; i < CounterplayRewardTypes.Length; i++)
            {
                if (CounterplayRewardTypes[i] == rewardType)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ShowCompanionCounterplayRewardFeedback(RunRewardType rewardType)
        {
            string message = BuildCompanionCounterplayRewardLine(rewardType);
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

        private static string BuildCompanionCounterplayRewardLine(RunRewardType rewardType)
        {
            switch (rewardType)
            {
                case RunRewardType.DashCooldown:
                    return "AI: Your dash recovers faster. I can call safer openings now.";
                case RunRewardType.RecoveryWindow:
                    return "AI: I will watch the follow-up. Your recovery window is stronger.";
                case RunRewardType.DodgeDamageBoost:
                    return "AI: Dodge cleanly, then strike. I will mark the timing.";
                case RunRewardType.GuardOpeningDamage:
                    return "AI: When the Guard opens up, punish it hard.";
                default:
                    return string.Empty;
            }
        }

        private static void ShowCompanionGrowthRouteFeedback(RunRewardCategory category, int level)
        {
            string message = BuildCompanionGrowthRouteLine(category, level);
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            CompanionSpeechBubbleUI speechBubble = UnityEngine.Object.FindAnyObjectByType<CompanionSpeechBubbleUI>();
            if (speechBubble != null)
            {
                speechBubble.ShowMessage(message, 4f, 4);
            }
        }

        private static string BuildCompanionGrowthRouteLine(RunRewardCategory category, int level)
        {
            switch (category)
            {
                case RunRewardCategory.Player:
                    return $"AI: Player route Lv{level}. I will create space for your attacks.";
                case RunRewardCategory.Companion:
                    return $"AI: AI route Lv{level}. I will take a more active support role.";
                case RunRewardCategory.Counterplay:
                    return $"AI: Counterplay route Lv{level}. Dodge cleanly and punish the opening.";
                case RunRewardCategory.Survival:
                    return $"AI: Survival route Lv{level}. We can take more risks and recover.";
                case RunRewardCategory.Build:
                    return $"AI: Build route Lv{level}. I will lean into this tactic.";
                default:
                    return string.Empty;
            }
        }

        private void ApplyMetaProgressionUpgrades()
        {
            if (!MetaProgressionState.HasState
                || appliedMetaProgressionRunId == RunSessionState.CurrentRunId)
            {
                return;
            }

            appliedMetaProgressionRunId = RunSessionState.CurrentRunId;
            GameObject player = GameObject.Find("Player");

            int maxHealthLevel = MetaProgressionState.PlayerMaxHealthLevel;
            if (maxHealthLevel > 0)
            {
                HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
                if (health != null)
                {
                    health.SetMaxHealth(
                        health.MaxHealth + metaPlayerMaxHealthBonusPerLevel * maxHealthLevel,
                        true);
                }
            }

            int damageLevel = MetaProgressionState.PlayerDamageLevel;
            if (damageLevel > 0)
            {
                PlayerCombat2D combat = player != null ? player.GetComponent<PlayerCombat2D>() : null;
                if (combat != null)
                {
                    combat.MultiplyDamage(1f + metaPlayerDamageBonusPerLevel * damageLevel);
                }
            }

            int cooldownLevel = MetaProgressionState.CompanionCooldownLevel;
            if (cooldownLevel > 0)
            {
                CompanionCombat companionCombat = FindAnyObjectByType<CompanionCombat>();
                if (companionCombat != null)
                {
                    companionCombat.MultiplyCooldown(
                        Mathf.Pow(metaCompanionCooldownMultiplierPerLevel, cooldownLevel));
                }
            }

            if (maxHealthLevel > 0 || damageLevel > 0 || cooldownLevel > 0)
            {
                metaProgressionFeedbackLine = BuildMetaProgressionAppliedLine(
                    maxHealthLevel,
                    damageLevel,
                    cooldownLevel);
                metaProgressionFeedbackTimer = metaProgressionFeedbackDuration;
            }

            if (logRunMessages)
            {
                Debug.Log(
                    $"Meta upgrades applied. HP Lv{maxHealthLevel}, Damage Lv{damageLevel}, AI Cooldown Lv{cooldownLevel}.",
                    this);
            }
        }

        public static string BuildMetaProgressionAppliedLine(
            int playerMaxHealthLevel,
            int playerDamageLevel,
            int companionCooldownLevel)
        {
            return $"Permanent Upgrades Applied: HP Lv{Mathf.Max(0, playerMaxHealthLevel)} / Damage Lv{Mathf.Max(0, playerDamageLevel)} / AI Cooldown Lv{Mathf.Max(0, companionCooldownLevel)}";
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

        private void ApplyCounterplayDashCooldownReward(GameObject player)
        {
            PlayerMovement2D movement = player != null ? player.GetComponent<PlayerMovement2D>() : null;
            if (movement != null)
            {
                movement.MultiplyDashCooldown(counterplayDashCooldownMultiplier);
            }
        }

        private void ApplyCounterplayRecoveryReward(GameObject player)
        {
            PlayerCounterplayFeedback counterplay = GetOrAddPlayerCounterplay(player);
            if (counterplay != null)
            {
                counterplay.AddRecoveryDuration(counterplayRecoveryDurationBonus);
            }
        }

        private void ApplyCounterplayDodgeBoostReward(GameObject player)
        {
            PlayerCounterplayFeedback counterplay = GetOrAddPlayerCounterplay(player);
            if (counterplay != null)
            {
                counterplay.ImproveDodgeDamageBoost(
                    counterplayDodgeBoostDuration,
                    counterplayDodgeDamageMultiplier);
            }
        }

        private void ApplyCounterplayGuardOpeningReward(GameObject player)
        {
            PlayerCounterplayFeedback counterplay = GetOrAddPlayerCounterplay(player);
            if (counterplay != null)
            {
                counterplay.MultiplyGuardOpeningDamage(counterplayGuardOpeningDamageMultiplier);
            }
        }

        private static PlayerCounterplayFeedback GetOrAddPlayerCounterplay(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            if (!player.TryGetComponent(out PlayerCounterplayFeedback counterplay))
            {
                counterplay = player.AddComponent<PlayerCounterplayFeedback>();
            }

            return counterplay;
        }

        private void ResetRewardGrowthCounts()
        {
            playerGrowthCount = 0;
            companionGrowthCount = 0;
            counterplayGrowthCount = 0;
            survivalGrowthCount = 0;
            buildGrowthCount = 0;
            hasActiveGrowthRoute = false;
            activeGrowthRouteCategory = RunRewardCategory.Player;
            activeGrowthRouteLevel = 0;
            playerRouteSpecializationCount = 0;
            companionRouteSpecializationCount = 0;
            counterplayRouteSpecializationCount = 0;
            survivalRouteSpecializationCount = 0;
            buildRouteSpecializationCount = 0;
        }

        private void RecordRewardGrowth(RunRewardCategory category)
        {
            switch (category)
            {
                case RunRewardCategory.Player:
                    playerGrowthCount++;
                    break;
                case RunRewardCategory.Companion:
                    companionGrowthCount++;
                    break;
                case RunRewardCategory.Counterplay:
                    counterplayGrowthCount++;
                    break;
                case RunRewardCategory.Survival:
                    survivalGrowthCount++;
                    break;
                case RunRewardCategory.Build:
                    buildGrowthCount++;
                    break;
                case RunRewardCategory.Relic:
                    break;
            }

            EvaluateGrowthRoute();
        }

        private string BuildCurrentGrowthSummaryLabel()
        {
            return $"Growth: Player {playerGrowthCount} | AI {companionGrowthCount} | Counterplay {counterplayGrowthCount} | Survival {survivalGrowthCount} | Build {buildGrowthCount}";
        }

        private string BuildCurrentRelicSummaryLabel()
        {
            if (currentRelics.Count == 0)
            {
                return "Relics: none";
            }

            string[] labels = new string[currentRelics.Count];
            for (int i = 0; i < currentRelics.Count; i++)
            {
                labels[i] = RunRelicRules.GetHudLabel(currentRelics[i]);
            }

            return $"Relics: {string.Join(" / ", labels)}";
        }

        private void EvaluateGrowthRoute()
        {
            RunRewardCategory previousCategory = activeGrowthRouteCategory;
            bool hadActiveRoute = hasActiveGrowthRoute;

            RunRewardCategory bestCategory = activeGrowthRouteCategory;
            int bestCount = hasActiveGrowthRoute ? GetGrowthCount(activeGrowthRouteCategory) : 0;
            ConsiderGrowthRouteCandidate(RunRewardCategory.Player, ref bestCategory, ref bestCount);
            ConsiderGrowthRouteCandidate(RunRewardCategory.Companion, ref bestCategory, ref bestCount);
            ConsiderGrowthRouteCandidate(RunRewardCategory.Counterplay, ref bestCategory, ref bestCount);
            ConsiderGrowthRouteCandidate(RunRewardCategory.Survival, ref bestCategory, ref bestCount);
            ConsiderGrowthRouteCandidate(RunRewardCategory.Build, ref bestCategory, ref bestCount);

            if (bestCount < Mathf.Max(2, growthRouteActivationThreshold))
            {
                hasActiveGrowthRoute = false;
                activeGrowthRouteLevel = 0;
                return;
            }

            hasActiveGrowthRoute = true;
            activeGrowthRouteCategory = bestCategory;
            activeGrowthRouteLevel = bestCount;

            if (!hadActiveRoute || previousCategory != activeGrowthRouteCategory)
            {
                ShowCompanionGrowthRouteFeedback(activeGrowthRouteCategory, activeGrowthRouteLevel);
            }
        }

        private void ConsiderGrowthRouteCandidate(
            RunRewardCategory candidate,
            ref RunRewardCategory bestCategory,
            ref int bestCount)
        {
            int candidateCount = GetGrowthCount(candidate);
            if (candidateCount > bestCount)
            {
                bestCategory = candidate;
                bestCount = candidateCount;
            }
        }

        private int GetGrowthCount(RunRewardCategory category)
        {
            switch (category)
            {
                case RunRewardCategory.Player:
                    return playerGrowthCount;
                case RunRewardCategory.Companion:
                    return companionGrowthCount;
                case RunRewardCategory.Counterplay:
                    return counterplayGrowthCount;
                case RunRewardCategory.Survival:
                    return survivalGrowthCount;
                case RunRewardCategory.Build:
                    return buildGrowthCount;
                default:
                    return 0;
            }
        }

        private int GetRouteSpecializationCount(RunRewardCategory category)
        {
            switch (category)
            {
                case RunRewardCategory.Player:
                    return playerRouteSpecializationCount;
                case RunRewardCategory.Companion:
                    return companionRouteSpecializationCount;
                case RunRewardCategory.Counterplay:
                    return counterplayRouteSpecializationCount;
                case RunRewardCategory.Survival:
                    return survivalRouteSpecializationCount;
                case RunRewardCategory.Build:
                    return buildRouteSpecializationCount;
                default:
                    return 0;
            }
        }

        private bool IsActiveGrowthRoute(RunRewardCategory category)
        {
            return hasActiveGrowthRoute
                && activeGrowthRouteCategory == category
                && activeGrowthRouteLevel >= Mathf.Max(2, growthRouteActivationThreshold);
        }

        private float GetRouteMultiplier(RunRewardCategory category, float bonusPerLevel)
        {
            return IsActiveGrowthRoute(category)
                ? 1f + Mathf.Max(0f, bonusPerLevel) * Mathf.Max(0, activeGrowthRouteLevel - 1)
                : 1f;
        }

        private string BuildCurrentGrowthRouteLabel()
        {
            return hasActiveGrowthRoute
                ? $"Growth Route: {RunRewardChoice.GetCategoryLabel(activeGrowthRouteCategory)} Lv{activeGrowthRouteLevel} | {BuildCurrentGrowthRouteEffectLabel()}{BuildGrowthRouteSpecializationLabelSuffix()}"
                : "Growth Route: forming";
        }

        private string BuildCurrentGrowthRouteSummaryLabel()
        {
            return hasActiveGrowthRoute
                ? $"{RunRewardChoice.GetCategoryLabel(activeGrowthRouteCategory)} Lv{activeGrowthRouteLevel}"
                : string.Empty;
        }

        private string BuildGrowthRouteSpecializationLabelSuffix()
        {
            int specializationCount = CurrentGrowthRouteSpecializationCount;
            return specializationCount > 0 ? $" | Special x{specializationCount}" : string.Empty;
        }

        private string BuildCurrentGrowthRouteEffectLabel()
        {
            switch (activeGrowthRouteCategory)
            {
                case RunRewardCategory.Player:
                    return $"Damage x{PlayerRouteDamageMultiplier:0.##}";
                case RunRewardCategory.Companion:
                    return $"AI Cooldown x{CompanionRouteCooldownMultiplier:0.##}";
                case RunRewardCategory.Counterplay:
                    return $"Dodge +{CounterplayRouteDodgeBoostDurationBonus:0.0}s / Counter x{CounterplayRouteDamageMultiplier:0.##}";
                case RunRewardCategory.Survival:
                    return $"Rescue HP +{SurvivalRouteRescueHealthBonus:0}";
                case RunRewardCategory.Build:
                    return $"Build Bonus +{BuildRouteBonusLevel}";
                default:
                    return "Effect ready";
            }
        }

        private void RecordRouteEntry(RoomType roomType, int roomNumber, RoomModifierType roomModifier)
        {
            currentRouteHistory.Add(roomType);
            currentRouteModifierHistory.Add(roomModifier);
            RunSessionState.RecordRoomEntered(roomType, roomNumber, roomModifier);
        }

        private string BuildCurrentRoomObjectiveLabel()
        {
            if (runCompleted)
            {
                return "Objective: Run complete";
            }

            if (waitingForReward)
            {
                return CurrentRoomType == RoomType.ShopRoom
                    ? "Objective: Choose a shop reward"
                    : "Objective: Room Cleared - Reward Available";
            }

            if (waitingForRest)
            {
                return "Objective: Choose a rest option";
            }

            if (waitingForNextRoom && ShouldShowSupportRoomObjective())
            {
                return CurrentRoomType == RoomType.SafeRoom
                    ? "Objective: Use rest point or skip"
                    : "Objective: Interact with shop or skip";
            }

            if (waitingForNextRoom)
            {
                return "Objective: Route opened";
            }

            if (IsCombatRoom(CurrentRoomType))
            {
                int total = GetCurrentRoomInitialEnemyCount();
                int defeated = Mathf.Clamp(total - GetCurrentRoomRemainingEnemyCount(), 0, total);
                return total > 0
                    ? $"Objective: Defeat enemies {defeated}/{total}"
                    : "Objective: Clear the room";
            }

            switch (CurrentRoomType)
            {
                case RoomType.SafeRoom:
                    return "Objective: Use rest point or skip";
                case RoomType.ShopRoom:
                    return "Objective: Interact with shop or skip";
                default:
                    return $"Objective: Explore {CurrentRoomType}";
            }
        }

        private string BuildCurrentRoomProgressLabel()
        {
            if (runCompleted)
            {
                return "Progress: Run complete";
            }

            if (waitingForReward)
            {
                return $"Progress: Reward Available - {currentRewardChoices.Count} option(s)";
            }

            if (waitingForRest)
            {
                return $"Progress: Rest options - {currentRestChoices.Count} choice(s)";
            }

            if (waitingForNextRoom)
            {
                if (ShouldShowSupportRoomObjective())
                {
                    return CurrentRoomType == RoomType.SafeRoom
                        ? "Progress: Safe room - rest point available"
                        : $"Progress: Supply room - supplies {currentSupplies}";
                }

                return $"Progress: Route Opened - {currentRoomChoices.Count} path(s)";
            }

            if (IsCombatRoom(CurrentRoomType))
            {
                int total = GetCurrentRoomInitialEnemyCount();
                int remaining = GetCurrentRoomRemainingEnemyCount();
                return total > 0
                    ? $"Progress: Enemies remaining: {remaining}/{total}"
                    : "Progress: No enemies";
            }

            switch (CurrentRoomType)
            {
                case RoomType.SafeRoom:
                    return "Progress: Safe room";
                case RoomType.ShopRoom:
                    return $"Progress: Supply room - supplies {currentSupplies}";
                default:
                    return "Progress: scouting";
            }
        }

        private string BuildCurrentNextStepLabel()
        {
            if (runCompleted)
            {
                return $"Next: Press {completionReturnHomeKey} to return home";
            }

            if (waitingForReward)
            {
                return CurrentRoomType == RoomType.ShopRoom
                    ? "Next: Select a shop reward or close"
                    : "Next: Select a reward";
            }

            if (waitingForRest)
            {
                return "Next: Pick one rest choice or close";
            }

            if (waitingForNextRoom && ShouldShowSupportRoomObjective())
            {
                return CurrentRoomType == RoomType.SafeRoom
                    ? "Next: Interact with rest point or choose a route"
                    : "Next: Interact with shop or choose a route";
            }

            if (waitingForNextRoom)
            {
                return "Next: Choose a route";
            }

            if (IsCombatRoom(CurrentRoomType))
            {
                return "Next: Clear the room";
            }

            return "Next: Explore";
        }

        private bool BuildHasActionableDemoNextStep()
        {
            if (runCompleted)
            {
                return !string.IsNullOrWhiteSpace(homeScenePath);
            }

            if (waitingForReward)
            {
                return currentRewardChoices.Count > 0;
            }

            if (waitingForRest)
            {
                return currentRestChoices.Count > 0;
            }

            if (waitingForNextRoom)
            {
                return currentRoomChoices.Count > 0 || allowDebugNextRoomKey;
            }

            if (IsCombatRoom(CurrentRoomType))
            {
                return roomManager == null || !roomManager.IsRoomCleared;
            }

            return CurrentRoomType == RoomType.SafeRoom || CurrentRoomType == RoomType.ShopRoom;
        }

        private string BuildCurrentDemoFlowLabel()
        {
            string actionState = HasActionableDemoNextStep ? "Ready" : "Blocked";

            if (runCompleted)
            {
                return $"Demo Flow: Complete | {actionState} | Return home with {completionReturnHomeKey}";
            }

            if (waitingForReward)
            {
                if (CurrentRoomType == RoomType.ShopRoom)
                {
                    bool isBlockedPurchase = !RunSupplyRules.CanAffordShopReward(currentSupplies);
                    string shopAction = isBlockedPurchase
                        ? "Close shop or earn more supplies later"
                        : "Buy reward or close shop";
                    return $"Demo Flow: Shop reward | {actionState} | {shopAction}";
                }

                return $"Demo Flow: Reward | {actionState} | Select a reward to open the route";
            }

            if (waitingForRest)
            {
                return $"Demo Flow: Rest choice | {actionState} | Pick one rest choice or close";
            }

            if (waitingForNextRoom)
            {
                if (ShouldShowSupportRoomObjective())
                {
                    return CurrentRoomType == RoomType.SafeRoom
                        ? $"Demo Flow: Safe room | {actionState} | Rest point optional, then Choose route"
                        : $"Demo Flow: Supply room | {actionState} | Shop optional, then Choose route";
                }

                bool bossOnlyChoice = currentRoomChoices.Count == 1 && currentRoomChoices[0] == RoomType.BossRoom;
                return bossOnlyChoice
                    ? $"Demo Flow: Boss route | {actionState} | Choose route to Boss"
                    : $"Demo Flow: Route | {actionState} | Choose route";
            }

            if (IsCombatRoom(CurrentRoomType))
            {
                return CurrentRoomType == RoomType.BossRoom
                    ? $"Demo Flow: Boss fight | {actionState} | Defeat the boss"
                    : $"Demo Flow: Combat | {actionState} | Clear enemies";
            }

            if (CurrentRoomType == RoomType.SafeRoom)
            {
                return $"Demo Flow: Safe room | {actionState} | Rest point optional";
            }

            if (CurrentRoomType == RoomType.ShopRoom)
            {
                return $"Demo Flow: Supply room | {actionState} | Shop optional";
            }

            return $"Demo Flow: Explore | {actionState}";
        }

        private bool ShouldShowSupportRoomObjective()
        {
            if (CurrentRoomType == RoomType.SafeRoom)
            {
                return !safeRestUsedThisRoom
                    && !waitingForRest
                    && string.IsNullOrWhiteSpace(lastSafeRestFeedbackMessage);
            }

            if (CurrentRoomType == RoomType.ShopRoom)
            {
                return !shopRewardPurchasedThisRoom
                    && !waitingForReward
                    && string.IsNullOrWhiteSpace(lastShopFeedbackMessage);
            }

            return false;
        }

        private int GetCurrentRoomInitialEnemyCount()
        {
            return roomManager != null ? Mathf.Max(0, roomManager.InitialEnemyCount) : 0;
        }

        private int GetCurrentRoomRemainingEnemyCount()
        {
            return roomManager != null ? Mathf.Max(0, roomManager.RemainingEnemyCount) : 0;
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
                        $"Rest Point: choose one option; Rest can restore {safeRoomHealAmount * RoomModifierRules.GetSafeHealMultiplier(modifier):0} HP.",
                        $"Route: optional rest point, then pick the next path. Supplies {currentSupplies} are saved for shops.",
                        modifierPreview);
                case RoomType.ShopRoom:
                    return CreateModifiedRoomChoicePreview(
                        roomType,
                        "Supply Room",
                        "Threat: safe, no enemies.",
                        BuildShopRewardPreview(roomType, modifier),
                        "Route: spend supplies for a smaller reward draft without combat.",
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

        private string BuildShopRewardPreview(RoomType roomType, RoomModifierType modifier)
        {
            int candidateCount = BuildRewardCandidateList().Count;
            int count = GetRewardChoiceTargetCount(roomType, modifier, candidateCount);
            return $"Reward: buy 1 of {count} options. {RunSupplyRules.BuildShopAffordabilityLabel(currentSupplies)}.";
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
            if (!string.IsNullOrWhiteSpace(lastRelicBannerMessage))
            {
                GUILayout.Space(4f);
                Color previousColor = GUI.color;
                GUI.color = new Color(0.35f, 0.95f, 1f, 1f);
                GUILayout.Label(lastRelicBannerMessage);
                GUI.color = previousColor;
            }

            if (!string.IsNullOrWhiteSpace(metaProgressionFeedbackLine) && metaProgressionFeedbackTimer > 0f)
            {
                GUILayout.Space(4f);
                Color previousColor = GUI.color;
                GUI.color = new Color(0.55f, 1f, 0.85f, 1f);
                GUILayout.Label(metaProgressionFeedbackLine);
                GUI.color = previousColor;
            }

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
            GUILayout.Label(BuildCompletionGrowthRouteLine(summary));
            GUILayout.Label(BuildCompletionMetaProgressionLine(summary));
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

        private static string BuildCompletionGrowthRouteLine(RunSessionSummary summary)
        {
            return summary.GrowthRouteSummaryLine;
        }

        private static string BuildCompletionMetaProgressionLine(RunSessionSummary summary)
        {
            return summary.MetaProgressionSummaryLine;
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
