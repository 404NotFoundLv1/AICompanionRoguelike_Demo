using System;
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
        [SerializeField] private RoomType[] roomSequence =
        {
            RoomType.BattleRoom,
            RoomType.SafeRoom,
            RoomType.BattleRoom,
            RoomType.ShopRoom,
            RoomType.EliteRoom
        };

        [Header("Debug")]
        [SerializeField] private bool logRunMessages = true;

        private int roomIndex = -1;
        private bool waitingForNextRoom;

        public static event Action<RunManager> AnyRunStarted;
        public event Action<RunManager> RunStarted;
        public event Action<RunManager, RoomType, int> RoomAdvanced;

        public int CurrentRoomNumber => Mathf.Max(0, roomIndex + 1);
        public RoomType CurrentRoomType => roomManager != null ? roomManager.CurrentRoomType : RoomType.BattleRoom;
        public bool IsWaitingForNextRoom => waitingForNextRoom;

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
            if (!waitingForNextRoom || !WasNextRoomPressed())
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
            if (roomManager == null)
            {
                Debug.LogWarning("RunManager cannot advance because RoomManager is missing.", this);
                return;
            }

            roomIndex++;
            waitingForNextRoom = false;

            RoomType nextRoomType = GetRoomTypeForIndex(roomIndex);
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
            waitingForNextRoom = true;

            if (logRunMessages)
            {
                Debug.Log($"Room #{roomNumber} cleared. Press {nextRoomKey} to enter the next room.", this);
            }
        }

        private bool WasNextRoomPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && nextRoomKey != Key.None && keyboard[nextRoomKey].wasPressedThisFrame;
        }
    }
}
