using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AICompanionRoguelike.Companion;
using AICompanionRoguelike.Combat;
using AICompanionRoguelike.Enemy;
using AICompanionRoguelike.Memory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AICompanionRoguelike.Roguelike
{
    public sealed class BranchEventRoomController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private Transform player;
        [SerializeField] private Transform companion;
        [SerializeField] private Camera branchCamera;
        [SerializeField] private CompanionRelationship companionRelationship;

        [Header("Choices")]
        [SerializeField] private Key rescueKey = Key.Digit1;
        [SerializeField] private Key leaveKey = Key.Digit2;
        [SerializeField] private Key challengeKey = Key.Digit3;
        [SerializeField] private bool logChoices = true;

        [Header("Branch Scene")]
        [SerializeField] private bool useAdditiveBranchScene = true;
        [SerializeField] private string branchScenePath = "Assets/_Game/Scenes/BranchEventRoom.unity";
        [SerializeField] private string branchSpawnPointName = "BranchPlayerSpawn";
        [SerializeField] private Vector3 branchSceneWorldOffset = new Vector3(24f, 0f, 0f);
        [SerializeField] private bool unloadBranchSceneOnReturn = true;

        [Header("Branch Room Fallback")]
        [SerializeField] private Vector3 branchRoomPlayerPosition = new Vector3(24f, -1.15f, 0f);
        [SerializeField] private bool showChoiceOverlay = true;

        [Header("Camera")]
        [SerializeField] private bool followPlayerInBranchRoom = true;
        [SerializeField] private Vector3 branchRoomCameraOffset = new Vector3(0f, 1.15f, -10f);

        [Header("Previous Room")]
        [SerializeField] private bool freezePreviousRoomDuringChoice = true;

        [Header("Choice Outcomes")]
        [SerializeField, Range(0f, 1f)] private float rescueHealthPercent = 0.35f;
        [SerializeField] private int rescueTrustDelta = 8;
        [SerializeField] private int rescueAffectionDelta = 6;
        [SerializeField] private int leaveTrustDelta = -12;
        [SerializeField] private int leaveAffectionDelta = -10;
        [SerializeField, Min(0f)] private float challengeBuffDuration = 8f;
        [SerializeField, Min(0f)] private float challengeOutgoingDamageMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float challengeIncomingDamageMultiplier = 0.7f;
        [SerializeField] private int challengeTrustDelta = 3;
        [SerializeField] private int challengeAffectionDelta = 1;
        [SerializeField] private bool quitGameOnLeave = true;

        [Header("Home Return")]
        [SerializeField] private bool returnHomeOnLeave = true;
        [SerializeField] private string homeScenePath = "Assets/_Game/Scenes/HomeScene.unity";

        private readonly List<BehaviourState> frozenBehaviours = new List<BehaviourState>(16);
        private readonly List<RigidbodyState> frozenRigidbodies = new List<RigidbodyState>(8);

        private bool isWaitingForChoice;
        private bool isLoadingBranchScene;
        private bool branchSceneIsLoaded;
        private bool playerIsInBranchRoom;
        private bool cameraIsInBranchRoom;
        private bool previousRoomIsFrozen;
        private Scene loadedBranchScene;
        private Vector3 playerReturnPosition;
        private Vector2 playerReturnVelocity;
        private Vector3 cameraReturnPosition;
        private Quaternion cameraReturnRotation;
        private HealthComponent playerHealth;
        private PlayerBranchChoiceBuff playerBranchChoiceBuff;
        private BranchEventChoice lastSelectedChoice;
        private string lastOutcomeDescription;

        public event Action<BranchEventRoomController, BranchEventChoice> ChoiceSelected;

        public bool IsWaitingForChoice => isWaitingForChoice;
        public bool IsLoadingBranchScene => isLoadingBranchScene;
        public bool BranchSceneIsLoaded => branchSceneIsLoaded;
        public bool PlayerIsInBranchRoom => playerIsInBranchRoom;
        public bool PreviousRoomIsFrozen => previousRoomIsFrozen;
        public BranchEventChoice LastSelectedChoice => lastSelectedChoice;
        public string LastOutcomeDescription => lastOutcomeDescription;

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
            if (isWaitingForChoice || isLoadingBranchScene)
            {
                return;
            }

            ResolveReferences();
            if (player == null)
            {
                Debug.LogWarning("BranchEventRoom cannot start because Player is missing.", this);
                return;
            }

            StartCoroutine(BeginBranchEventRoomRoutine(sourceRoomNumber, sourceRoomType));
        }

        public void SelectChoice(BranchEventChoice choice)
        {
            if (!isWaitingForChoice)
            {
                return;
            }

            lastSelectedChoice = choice;
            ApplyChoiceOutcome(choice);
            ChoiceSelected?.Invoke(this, choice);

            if (logChoices)
            {
                Debug.Log($"BranchEventRoom choice selected: {choice}. {lastOutcomeDescription}", this);
            }

            if (choice == BranchEventChoice.Leave && returnHomeOnLeave)
            {
                ReturnFromBranchRoom();
                ReturnHome();
                return;
            }

            if (choice == BranchEventChoice.Leave && quitGameOnLeave)
            {
                QuitGameForNow();
                return;
            }

            ReturnFromBranchRoom();
        }

        private IEnumerator BeginBranchEventRoomRoutine(int sourceRoomNumber, RoomType sourceRoomType)
        {
            isLoadingBranchScene = true;
            FreezePreviousRoom();

            Vector3 branchPlayerPosition = branchRoomPlayerPosition;
            if (useAdditiveBranchScene)
            {
                yield return LoadBranchScene();
                branchPlayerPosition = FindBranchSpawnPosition(branchPlayerPosition);
            }

            MovePlayerIntoBranchRoom(branchPlayerPosition);
            MoveCameraIntoBranchRoom();
            isWaitingForChoice = true;
            isLoadingBranchScene = false;

            if (logChoices)
            {
                string sceneInfo = branchSceneIsLoaded ? $" using additive scene {loadedBranchScene.path}" : " using fallback position";
                Debug.Log($"BranchEventRoom opened from {sourceRoomType} #{sourceRoomNumber}{sceneInfo}: [{rescueKey}] Rescue, [{leaveKey}] Leave, [{challengeKey}] Challenge", this);
            }
        }

        private IEnumerator LoadBranchScene()
        {
            branchSceneIsLoaded = false;
            loadedBranchScene = default(Scene);

            if (string.IsNullOrWhiteSpace(branchScenePath))
            {
                Debug.LogWarning("BranchEventRoom has no branchScenePath. Falling back to branchRoomPlayerPosition.", this);
                yield break;
            }

            Scene existingScene = FindLoadedBranchScene();
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                loadedBranchScene = existingScene;
                branchSceneIsLoaded = true;
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(branchScenePath))
            {
                Debug.LogWarning($"BranchEventRoom scene '{branchScenePath}' is not in Build Settings or cannot be loaded. Falling back to branchRoomPlayerPosition.", this);
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(branchScenePath, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogWarning($"BranchEventRoom failed to start loading '{branchScenePath}'. Falling back to branchRoomPlayerPosition.", this);
                yield break;
            }

            yield return loadOperation;

            loadedBranchScene = FindLoadedBranchScene();
            branchSceneIsLoaded = loadedBranchScene.IsValid() && loadedBranchScene.isLoaded;

            if (!branchSceneIsLoaded)
            {
                Debug.LogWarning($"BranchEventRoom loaded '{branchScenePath}' but could not resolve the loaded scene. Falling back to branchRoomPlayerPosition.", this);
                yield break;
            }

            OffsetLoadedBranchScene();
        }

        private Scene FindLoadedBranchScene()
        {
            Scene sceneByPath = SceneManager.GetSceneByPath(branchScenePath);
            if (sceneByPath.IsValid())
            {
                return sceneByPath;
            }

            string sceneName = Path.GetFileNameWithoutExtension(branchScenePath);
            return string.IsNullOrEmpty(sceneName) ? default(Scene) : SceneManager.GetSceneByName(sceneName);
        }

        private void OffsetLoadedBranchScene()
        {
            if (!loadedBranchScene.IsValid() || !loadedBranchScene.isLoaded || branchSceneWorldOffset == Vector3.zero)
            {
                return;
            }

            GameObject[] roots = loadedBranchScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                roots[i].transform.position += branchSceneWorldOffset;
            }
        }

        private Vector3 FindBranchSpawnPosition(Vector3 fallbackPosition)
        {
            if (!branchSceneIsLoaded || string.IsNullOrEmpty(branchSpawnPointName))
            {
                return fallbackPosition;
            }

            GameObject[] roots = loadedBranchScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform spawnPoint = FindChildRecursive(roots[i].transform, branchSpawnPointName);
                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }

            Debug.LogWarning($"BranchEventRoom scene '{branchScenePath}' has no spawn point named '{branchSpawnPointName}'. Falling back to branchRoomPlayerPosition.", this);
            return fallbackPosition;
        }

        private void ResolveReferences()
        {
            roomManager = roomManager != null ? roomManager : GetComponent<RoomManager>();
            branchCamera = branchCamera != null ? branchCamera : Camera.main;
            ResolveParticipants();
            ResolveOutcomeReferences();
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

        private void ResolveOutcomeReferences()
        {
            playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
            playerBranchChoiceBuff = player != null ? player.GetComponent<PlayerBranchChoiceBuff>() : null;

            if (player != null && playerBranchChoiceBuff == null)
            {
                playerBranchChoiceBuff = player.gameObject.AddComponent<PlayerBranchChoiceBuff>();
            }

            if (companionRelationship == null)
            {
                companionRelationship = FindAnyObjectByType<CompanionRelationship>();
            }
        }

        private void MovePlayerIntoBranchRoom(Vector3 branchPlayerPosition)
        {
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            playerReturnPosition = playerBody != null ? (Vector3)playerBody.position : player.position;
            playerReturnVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
            playerIsInBranchRoom = true;

            MoveTransform(player, branchPlayerPosition, Vector2.zero);
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
            if (!isWaitingForChoice && !isLoadingBranchScene && !playerIsInBranchRoom && !previousRoomIsFrozen && !cameraIsInBranchRoom && !branchSceneIsLoaded)
            {
                return;
            }

            isWaitingForChoice = false;
            isLoadingBranchScene = false;
            ReturnPlayerFromBranchRoom();
            ReturnCameraFromBranchRoom();
            RestorePreviousRoom();
            UnloadBranchSceneIfNeeded();
        }

        private void UnloadBranchSceneIfNeeded()
        {
            if (!unloadBranchSceneOnReturn || !branchSceneIsLoaded || !loadedBranchScene.IsValid() || !loadedBranchScene.isLoaded)
            {
                branchSceneIsLoaded = false;
                loadedBranchScene = default(Scene);
                return;
            }

            SceneManager.UnloadSceneAsync(loadedBranchScene);
            branchSceneIsLoaded = false;
            loadedBranchScene = default(Scene);
        }

        private void ApplyChoiceOutcome(BranchEventChoice choice)
        {
            ResolveOutcomeReferences();

            switch (choice)
            {
                case BranchEventChoice.Rescue:
                    ApplyRescueOutcome();
                    break;
                case BranchEventChoice.Leave:
                    ApplyLeaveOutcome();
                    break;
                case BranchEventChoice.Challenge:
                    ApplyChallengeOutcome();
                    break;
            }
        }

        private void ApplyRescueOutcome()
        {
            float targetHealth = playerHealth != null ? playerHealth.MaxHealth * rescueHealthPercent : 0f;
            float healedAmount = 0f;

            if (playerHealth != null && !playerHealth.IsDead && playerHealth.CurrentHealth < targetHealth)
            {
                healedAmount = targetHealth - playerHealth.CurrentHealth;
                playerHealth.Heal(healedAmount);
            }

            ApplyRelationshipMemory(
                "BranchRescue",
                rescueTrustDelta,
                rescueAffectionDelta,
                RelationshipMemoryTag.Protected);

            lastOutcomeDescription =
                $"Rescue restored {healedAmount:0.#} HP and strengthened the companion bond. Returning to the preserved combat room.";
        }

        private void ApplyLeaveOutcome()
        {
            ApplyRelationshipMemory(
                "BranchLeave",
                leaveTrustDelta,
                leaveAffectionDelta,
                RelationshipMemoryTag.Abandoned);

            lastOutcomeDescription = returnHomeOnLeave
                ? "Leave recorded an abandoned memory. Returning to home."
                : "Leave recorded an abandoned memory. Home return is disabled, so the game will quit.";
        }

        private void ApplyChallengeOutcome()
        {
            if (playerBranchChoiceBuff != null)
            {
                playerBranchChoiceBuff.Activate(
                    challengeBuffDuration,
                    challengeOutgoingDamageMultiplier,
                    challengeIncomingDamageMultiplier);
            }

            ApplyRelationshipMemory(
                "BranchChallenge",
                challengeTrustDelta,
                challengeAffectionDelta,
                RelationshipMemoryTag.Brave);

            lastOutcomeDescription =
                $"Challenge keeps the player near death but grants {challengeBuffDuration:0.#}s combat boost. Returning to the preserved combat room.";
        }

        private void ApplyRelationshipMemory(
            string sourceLabel,
            int trustDelta,
            int affectionDelta,
            RelationshipMemoryTag memoryTag)
        {
            if (companionRelationship == null)
            {
                return;
            }

            companionRelationship.ApplyMemoryEvent(sourceLabel, trustDelta, affectionDelta, memoryTag);
        }

        private void ReturnHome()
        {
            if (string.IsNullOrWhiteSpace(homeScenePath))
            {
                Debug.LogWarning("Leave selected but homeScenePath is empty. Falling back to quit behavior.", this);
                QuitGameForNow();
                return;
            }

            Debug.Log($"Leave selected: returning to home scene {homeScenePath}.", this);
            SceneManager.LoadScene(homeScenePath, LoadSceneMode.Single);
        }

        private void QuitGameForNow()
        {
            Debug.Log("Leave selected: quitting the game because home return is disabled.", this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildRecursive(root.GetChild(i), childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
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
            if (!showChoiceOverlay)
            {
                return;
            }

            if (isLoadingBranchScene)
            {
                DrawBranchOverlay("Branch Event Room", "Loading branch scene...");
                return;
            }

            if (!isWaitingForChoice)
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
            GUILayout.Label(branchSceneIsLoaded ? "Additive branch scene loaded. Choose the consequence." : "Fallback branch position active. Choose the consequence.");
            GUILayout.Space(12f);

            if (GUILayout.Button($"[{rescueKey}] Rescue - heal and strengthen bond"))
            {
                SelectChoice(BranchEventChoice.Rescue);
            }

            if (GUILayout.Button($"[{leaveKey}] Leave - return home"))
            {
                SelectChoice(BranchEventChoice.Leave);
            }

            if (GUILayout.Button($"[{challengeKey}] Challenge - return with combat boost"))
            {
                SelectChoice(BranchEventChoice.Challenge);
            }

            GUILayout.EndArea();
        }

        private static void DrawBranchOverlay(string title, string message)
        {
            const float panelWidth = 420f;
            const float panelHeight = 120f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label(title);
            GUILayout.Space(8f);
            GUILayout.Label(message);
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
