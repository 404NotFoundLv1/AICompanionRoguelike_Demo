using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.QTE
{
    public sealed class QTEManager : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField, Min(0.1f)] private float defaultDuration = 2f;
        [SerializeField] private Key defaultExpectedKey = Key.E;
        [SerializeField] private bool logDebugMessages = true;

        private float activeDuration;
        private string activePrompt;
        private Key expectedKey;
        private GameObject requester;
        private GameObject target;

        public static QTEManager Instance { get; private set; }

        public event Action<QTEManager> QTEStarted;
        public event Action<QTEManager, QTEResultType> QTECompleted;

        public bool IsActive { get; private set; }
        public float TimeRemaining { get; private set; }
        public float ActiveDuration => activeDuration;
        public string ActivePrompt => activePrompt;
        public Key ExpectedKey => expectedKey;
        public GameObject Requester => requester;
        public GameObject Target => target;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate QTEManager disabled. Only one QTEManager should exist in the active scene.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (WasExpectedKeyPressed())
            {
                ResolveActiveQTE(QTEResultType.Success);
                return;
            }

            if (WasWrongKeyPressed())
            {
                ResolveActiveQTE(QTEResultType.WrongInput);
                return;
            }

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                ResolveActiveQTE(QTEResultType.Ignored);
            }
        }

        public bool TryStartQTE(string prompt, float duration, Key requiredKey, GameObject qteRequester, GameObject qteTarget)
        {
            if (IsActive)
            {
                return false;
            }

            activeDuration = Mathf.Max(0.1f, duration > 0f ? duration : defaultDuration);
            expectedKey = requiredKey != Key.None ? requiredKey : defaultExpectedKey;
            activePrompt = string.IsNullOrWhiteSpace(prompt) ? $"Press {expectedKey}" : prompt;
            requester = qteRequester;
            target = qteTarget;
            TimeRemaining = activeDuration;
            IsActive = true;

            if (logDebugMessages)
            {
                Debug.Log($"QTE started: {activePrompt} | Key: {expectedKey} | Time: {activeDuration:0.00}s", this);
            }

            QTEStarted?.Invoke(this);
            return true;
        }

        public bool ResolveActiveQTE(QTEResultType resultType)
        {
            if (!IsActive)
            {
                return false;
            }

            IsActive = false;
            TimeRemaining = 0f;

            if (logDebugMessages)
            {
                Debug.Log($"QTE completed: {resultType} | Prompt: {activePrompt}", this);
            }

            QTECompleted?.Invoke(this, resultType);
            return true;
        }

        private bool WasExpectedKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && expectedKey != Key.None && keyboard[expectedKey].wasPressedThisFrame;
        }

        private bool WasWrongKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame && !WasExpectedKeyPressed();
        }
    }
}
