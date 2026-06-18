using System;
using System.Collections.Generic;
using AICompanionRoguelike.Character;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Memory;
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
        [SerializeField, Min(1)] private int roomsToCompleteRun = 3;
        [SerializeField] private string homeScenePath = "Assets/_Game/Scenes/HomeScene.unity";
        [SerializeField] private Key completionReturnHomeKey = Key.E;
        [SerializeField] private bool showCompletionPanel = true;
        [SerializeField] private Rect completionPanelRect = new Rect(0f, 92f, 500f, 220f);

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

        [Header("Debug")]
        [SerializeField] private bool logRunMessages = true;

        private int roomIndex = -1;
        private bool waitingForNextRoom;
        private bool waitingForReward;
        private bool runCompleted;
        private readonly List<RoomType> currentRoomChoices = new List<RoomType>(4);
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
        public IReadOnlyList<RoomType> CurrentRoomChoices => currentRoomChoices;
        public IReadOnlyList<RunRewardChoice> CurrentRewardChoices => currentRewardChoices;

        private void Reset()
        {
            roomManager = GetComponent<RoomManager>();
            branchEventRoomController = GetComponent<BranchEventRoomController>();
        }

        private void Awake()
        {
            roomManager = roomManager != null ? roomManager : GetComponent<RoomManager>();
            branchEventRoomController = branchEventRoomController != null ? branchEventRoomController : GetComponent<BranchEventRoomController>();
        }

        private void OnEnable()
        {
            if (roomManager != null)
            {
                roomManager.RoomCleared += HandleRoomCleared;
            }
        }

        private void Start()
        {
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
            if (roomManager != null)
            {
                roomManager.RoomCleared -= HandleRoomCleared;
            }
        }

        public void StartRun()
        {
            RunSessionState.EnsureRunStartedFromBattleScene(SceneManager.GetActiveScene().path);
            roomIndex = -1;
            waitingForNextRoom = false;
            waitingForReward = false;
            runCompleted = false;
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
            AdvanceToRoom(nextRoomType);
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

            AdvanceToRoom(selectedRoomType);
        }

        private void AdvanceToRoom(RoomType nextRoomType)
        {
            if (roomManager == null)
            {
                Debug.LogWarning("RunManager cannot advance because RoomManager is missing.", this);
                return;
            }

            roomIndex++;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();

            int roomNumber = roomIndex + 1;

            roomManager.EnterRoom(nextRoomType, roomNumber);
            RoomAdvanced?.Invoke(this, nextRoomType, roomNumber);

            if (logRunMessages)
            {
                Debug.Log($"Advanced to room #{roomNumber}: {nextRoomType}", this);
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
                PrepareRewardChoices();
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
                && (roomType == RoomType.BattleRoom || roomType == RoomType.EliteRoom);
        }

        private bool ShouldCompleteRun(int roomNumber)
        {
            return useRunCompletion && !runCompleted && roomNumber >= roomsToCompleteRun;
        }

        private void CompleteRun(RoomType roomType, int roomNumber)
        {
            runCompleted = true;
            waitingForNextRoom = false;
            waitingForReward = false;
            ClearRewardChoices();
            ClearPreparedRoomChoices();

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
            waitingForReward = true;
            currentRewardChoices.Clear();

            List<RunRewardType> candidates = BuildRewardCandidateList();
            int targetCount = Mathf.Min(rewardChoiceCount, candidates.Count);

            for (int i = 0; i < targetCount; i++)
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
            List<RunRewardType> candidates = new List<RunRewardType>(5);

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

        private void PrepareNextRoomChoices()
        {
            currentRoomChoices.Clear();

            List<RoomType> candidates = BuildSelectableCandidateList();
            int targetCount = Mathf.Min(roomChoiceCount, candidates.Count);

            for (int i = 0; i < targetCount; i++)
            {
                int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
                currentRoomChoices.Add(candidates[selectedIndex]);
                candidates.RemoveAt(selectedIndex);
            }

            if (currentRoomChoices.Count == 0)
            {
                currentRoomChoices.Add(RoomType.BattleRoom);
            }

            if (logRunMessages)
            {
                Debug.Log($"Next room choices ready: {string.Join(", ", currentRoomChoices)}", this);
            }

            RoomChoicesPrepared?.Invoke(this, currentRoomChoices);
        }

        private void ClearPreparedRoomChoices()
        {
            if (currentRoomChoices.Count == 0)
            {
                return;
            }

            currentRoomChoices.Clear();
            RoomChoicesCleared?.Invoke(this);
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

        private void OnGUI()
        {
            if (!showCompletionPanel || !runCompleted)
            {
                return;
            }

            DrawCompletionPanel();
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
            GUILayout.Label(BuildCompletionRewardLine(summary));
            GUILayout.Label(BuildCompletionRelationshipLine(summary));
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

        private static string BuildCompletionRelationshipLine(RunSessionSummary summary)
        {
            if (!summary.HasRelationship)
            {
                return "AI 关系：未记录";
            }

            return $"AI 关系：信赖 {summary.FinalTrust}    好感 {summary.FinalAffection}";
        }

        private static bool IsSelectableRoomType(RoomType roomType)
        {
            return roomType != RoomType.BranchEventRoom;
        }
    }
}
