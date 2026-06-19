using System;
using System.IO;
using AICompanionRoguelike.Memory;
using UnityEngine;

namespace AICompanionRoguelike.Save
{
    [DefaultExecutionOrder(-10000)]
    public sealed class SaveGameCoordinator : MonoBehaviour
    {
        private const string DefaultSlot = "slot_0";
        private const string DefaultFileName = "save_slot_0.json";

        private static SaveGameCoordinator instance;

        private GameSaveService service;
        private RelationshipAutosaveSession autosaveSession;

        public static event Action<string> StatusChanged;

        public static string DefaultSlotId => DefaultSlot;
        public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, DefaultFileName);
        public static string LastStatusMessage { get; private set; } = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            LastStatusMessage = string.Empty;
            StatusChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeFirstScene()
        {
            if (instance != null)
            {
                return;
            }

            GameObject coordinatorObject = new GameObject("[SaveGameCoordinator]");
            coordinatorObject.AddComponent<SaveGameCoordinator>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            service = new GameSaveService(new JsonGameSaveStore(DefaultSavePath));
            autosaveSession = new RelationshipAutosaveSession(service);
            autosaveSession.Saved += HandleSaved;
            autosaveSession.SaveFailed += HandleSaveFailed;

            GameSaveLoadResult loadResult = autosaveSession.Start();
            PublishLoadStatus(loadResult);
        }

        private void OnApplicationQuit()
        {
            autosaveSession?.SaveNow();
        }

        private void OnDestroy()
        {
            if (autosaveSession != null)
            {
                autosaveSession.Saved -= HandleSaved;
                autosaveSession.SaveFailed -= HandleSaveFailed;
                autosaveSession.Dispose();
                autosaveSession = null;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        public static bool ClearDefaultSave()
        {
            try
            {
                GameSaveService activeService = instance != null && instance.service != null
                    ? instance.service
                    : new GameSaveService(new JsonGameSaveStore(DefaultSavePath));
                bool deleted = activeService.Delete();
                CompanionRelationshipState.Clear();
                PublishStatus(deleted ? "Save data cleared" : "No save data to clear");
                return deleted;
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is NotSupportedException)
            {
                PublishStatus("Could not clear save data");
                Debug.LogWarning($"Could not clear default save: {exception.Message}");
                return false;
            }
        }

        private static void PublishLoadStatus(GameSaveLoadResult result)
        {
            switch (result.Status)
            {
                case GameSaveLoadStatus.Loaded:
                    PublishStatus("Progress loaded");
                    break;
                case GameSaveLoadStatus.Missing:
                    PublishStatus("Autosave ready");
                    break;
                case GameSaveLoadStatus.UnsupportedVersion:
                    PublishStatus("Save version is not supported; using defaults");
                    Debug.LogWarning(result.ErrorMessage);
                    break;
                default:
                    PublishStatus("Save could not be read; using defaults");
                    Debug.LogWarning($"Could not load default save: {result.ErrorMessage}");
                    break;
            }
        }

        private static void PublishStatus(string message)
        {
            LastStatusMessage = message ?? string.Empty;
            StatusChanged?.Invoke(LastStatusMessage);
        }

        private void HandleSaved()
        {
            PublishStatus("Progress autosaved");
        }

        private void HandleSaveFailed(string errorMessage)
        {
            PublishStatus("Autosave failed");
            Debug.LogWarning($"Could not write default save: {errorMessage}", this);
        }
    }
}
