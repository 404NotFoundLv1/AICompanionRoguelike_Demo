using System;
using System.Collections.Generic;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class BranchEventRoomController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private Transform player;
        [SerializeField] private Transform companion;
        [SerializeField] private Camera branchCamera;

        [Header("Choices")]
        [SerializeField] private Key rescueKey = Key.Digit1;
        [SerializeField] private Key leaveKey = Key.Digit2;
        [SerializeField] private Key challengeKey = Key.Digit3;
        [SerializeField] private bool logChoices = true;

        [Header("Branch Room")]
        [SerializeField] private Vector3 branchRoomPlayerPosition = new Vector3(24f, -1.15f, 0f);
        [SerializeField] private bool showChoiceOverlay = true;

        [Header("Camera")]
        [SerializeField] private bool followPlayerInBranchRoom = true;
        [SerializeField] private Vector3 branchRoomCameraOffset = new Vector3(0f, 1.15f, -10f);

        [Header("Previous Room")]
        [SerializeField] private bool freezePreviousRoomDuringChoice = true;

        private readonly List<BehaviourState> frozenBehaviours = new List<BehaviourState>(16);
        private readonly List<RigidbodyState> frozenRigidbodies = new List<RigidbodyState>(8);

        private bool isWaitingForChoice;
        private bool playerIsInBranchRoom;
        private bool cameraIsInBranchRoom;
        private bool previousRoomIsFrozen;
        private Vector3 playerReturnPosition;
        private Vector2 playerReturnVelocity;
        private Vector3 cameraReturnPosition;
        private Quaternion cameraReturnRotation;

        public event Action<BranchEventRoomController, BranchEventChoice> ChoiceSelected;

        public bool IsWaitingForChoice => isWaitingForChoice;
        public bool PlayerIsInBranchRoom => playerIsInBranchRoom;
        public bool PreviousRoomIsFrozen => previousRoomIsFrozen;

        private void Awake()
        {
            roomManager = roomManager != null ? roomManager : GetComponent<RoomManager>();
            branchCamera = branchCamera != null ? branchCamera : Camera.main;
            ResolveParticipants();
        }

        private void Update()
        {
            if (!isWaitingForChoice)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressed(keyboard, rescueKey))
            {
                SelectChoice(BranchEventChoice.Rescue);
            }
            else if (WasPressed(keyboard, leaveKey))
            {
                SelectChoice(BranchEventChoice.Leave);
            }
            else if (WasPressed(keyboard, challengeKey))
            {
                SelectChoice(BranchEventChoice.Challenge);
            }
        }

        private void LateUpdate()
        {
            if (isWaitingForChoice && followPlayerInBranchRoom)
            {
                MoveCameraToPlayer();
            }
        }

        private void OnDisable()
        {
            ReturnFromBranchRoom();
        }

        public void BeginBranchEventRoom(int sourceRoomNumber, RoomType sourceRoomType)
        {
            if (isWaitingForChoice)
            {
                return;
            }

            ResolveReferences();
            if (player == null)
            {
                Debug.LogWarning("BranchEventRoom cannot start because Player is missing.", this);
                return;
            }

            isWaitingForChoice = true;
            FreezePreviousRoom();
            MovePlayerIntoBranchRoom();
            MoveCameraIntoBranchRoom();

            if (logChoices)
            {
                Debug.Log($"BranchEventRoom opened from {sourceRoomType} #{sourceRoomNumber}: [{rescueKey}] Rescue, [{leaveKey}] Leave, [{challengeKey}] Challenge", this);
            }
        }

        public void SelectChoice(BranchEventChoice choice)
        {
            if (!isWaitingForChoice)
            {
                return;
            }

            ChoiceSelected?.Invoke(this, choice);

            if (logChoices)
            {
                Debug.Log($"BranchEventRoom choice selected: {choice}. Returning to the preserved combat room.", this);
            }

            ReturnFromBranchRoom();
        }

        private void ResolveReferences()
        {
            roomManager = roomManager != null ? roomManager : GetComponent<RoomManager>();
            branchCamera = branchCamera != null ? branchCamera : Camera.main;
            ResolveParticipants();
        }

        private void ResolveParticipants()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.Find("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (companion == null)
            {
                GameObject companionObject = GameObject.Find("Companion");
                companion = companionObject != null ? companionObject.transform : null;
            }
        }

        private void MovePlayerIntoBranchRoom()
        {
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerReturnPosition = playerBody != null ? (Vector3)playerBody.position : player.position;
            playerReturnVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
            playerIsInBranchRoom = true;

            MoveTransform(player, branchRoomPlayerPosition, Vector2.zero);
        }

        private void ReturnPlayerFromBranchRoom()
        {
            if (!playerIsInBranchRoom || player == null)
            {
                return;
            }

            MoveTransform(player, playerReturnPosition, playerReturnVelocity);
            playerIsInBranchRoom = false;
        }

        private void MoveCameraIntoBranchRoom()
        {
            if (!followPlayerInBranchRoom)
            {
                return;
            }

            branchCamera = branchCamera != null ? branchCamera : Camera.main;
            if (branchCamera == null)
            {
                return;
            }

            cameraReturnPosition = branchCamera.transform.position;
            cameraReturnRotation = branchCamera.transform.rotation;
            cameraIsInBranchRoom = true;
            MoveCameraToPlayer();
        }

        private void MoveCameraToPlayer()
        {
            if (branchCamera == null || player == null)
            {
                return;
            }

            branchCamera.transform.position = player.position + branchRoomCameraOffset;
        }

        private void ReturnCameraFromBranchRoom()
        {
            if (!cameraIsInBranchRoom || branchCamera == null)
            {
                return;
            }

            branchCamera.transform.SetPositionAndRotation(cameraReturnPosition, cameraReturnRotation);
            cameraIsInBranchRoom = false;
        }

        private void FreezePreviousRoom()
        {
            if (!freezePreviousRoomDuringChoice || previousRoomIsFrozen)
            {
                return;
            }

            FreezeCompanion();
            FreezeEnemies();
            previousRoomIsFrozen = true;
        }

        private void FreezeCompanion()
        {
            if (companion == null)
            {
                return;
            }

            FreezeBehaviour(companion.GetComponent<CompanionMovement>());
            FreezeBehaviour(companion.GetComponent<CompanionSensor>());
            FreezeBehaviour(companion.GetComponent<CompanionCombat>());
            FreezeBehaviour(companion.GetComponent<CompanionQTERequester>());
            FreezeRigidbody(companion.GetComponent<Rigidbody2D>());
        }

        private void FreezeEnemies()
        {
            if (roomManager == null)
            {
                return;
            }

            IReadOnlyList<EnemyController2D> enemies = roomManager.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController2D enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                FreezeBehaviour(enemy);
                FreezeBehaviour(enemy.GetComponent<EnemyAttack2D>());
                FreezeRigidbody(enemy.GetComponent<Rigidbody2D>());
            }
        }

        private void RestorePreviousRoom()
        {
            if (!previousRoomIsFrozen)
            {
                return;
            }

            for (int i = 0; i < frozenRigidbodies.Count; i++)
            {
                RigidbodyState state = frozenRigidbodies[i];
                if (state.Body == null)
                {
                    continue;
                }

                state.Body.simulated = state.WasSimulated;
                state.Body.linearVelocity = state.LinearVelocity;
                state.Body.angularVelocity = state.AngularVelocity;
            }

            for (int i = 0; i < frozenBehaviours.Count; i++)
            {
                BehaviourState state = frozenBehaviours[i];
                if (state.Behaviour != null)
                {
                    state.Behaviour.enabled = state.WasEnabled;
                }
            }

            frozenRigidbodies.Clear();
            frozenBehaviours.Clear();
            previousRoomIsFrozen = false;
        }

        private void FreezeBehaviour(Behaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            frozenBehaviours.Add(new BehaviourState(behaviour, behaviour.enabled));
            behaviour.enabled = false;
        }

        private void FreezeRigidbody(Rigidbody2D body)
        {
            if (body == null)
            {
                return;
            }

            frozenRigidbodies.Add(new RigidbodyState(body, body.simulated, body.linearVelocity, body.angularVelocity));
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        private void ReturnFromBranchRoom()
        {
            if (!isWaitingForChoice && !playerIsInBranchRoom && !previousRoomIsFrozen && !cameraIsInBranchRoom)
            {
                return;
            }

            isWaitingForChoice = false;
            ReturnPlayerFromBranchRoom();
            ReturnCameraFromBranchRoom();
            RestorePreviousRoom();
        }

        private static void MoveTransform(Transform target, Vector3 position, Vector2 velocity)
        {
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = velocity;
                target.position = position;
                return;
            }

            target.position = position;
        }

        private void OnGUI()
        {
            if (!isWaitingForChoice || !showChoiceOverlay)
            {
                return;
            }

            const float panelWidth = 420f;
            const float panelHeight = 210f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Branch Event Room");
            GUILayout.Space(8f);
            GUILayout.Label("The previous combat room is frozen. Choose how to return.");
            GUILayout.Space(12f);

            if (GUILayout.Button($"[{rescueKey}] Rescue"))
            {
                SelectChoice(BranchEventChoice.Rescue);
            }

            if (GUILayout.Button($"[{leaveKey}] Leave"))
            {
                SelectChoice(BranchEventChoice.Leave);
            }

            if (GUILayout.Button($"[{challengeKey}] Challenge"))
            {
                SelectChoice(BranchEventChoice.Challenge);
            }

            GUILayout.EndArea();
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        private readonly struct BehaviourState
        {
            public BehaviourState(Behaviour behaviour, bool wasEnabled)
            {
                Behaviour = behaviour;
                WasEnabled = wasEnabled;
            }

            public Behaviour Behaviour { get; }
            public bool WasEnabled { get; }
        }

        private readonly struct RigidbodyState
        {
            public RigidbodyState(Rigidbody2D body, bool wasSimulated, Vector2 linearVelocity, float angularVelocity)
            {
                Body = body;
                WasSimulated = wasSimulated;
                LinearVelocity = linearVelocity;
                AngularVelocity = angularVelocity;
            }

            public Rigidbody2D Body { get; }
            public bool WasSimulated { get; }
            public Vector2 LinearVelocity { get; }
            public float AngularVelocity { get; }
        }
    }
}
